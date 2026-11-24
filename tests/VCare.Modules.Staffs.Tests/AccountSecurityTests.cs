using System.Data.Common;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Staffs.Application.Abstraction;
using Staffs.Application.Dto;
using Staffs.Application.Services;
using Staffs.Application.Services.Interface;
using Staffs.Domain.Entity;
using Staffs.Infrastructure;
using Staffs.Infrastructure.Persistence;
using VCare.SharedKernel.Abstractions;
using VCare.SharedKernel.Domain;
using VCare.SharedKernel.Results;
using Xunit;

namespace VCare.Modules.Staffs.Tests;

/// <summary>
/// The real Identity user manager over an in-memory store, configured the way
/// StaffModule configures it, so lockout and security stamps behave as in production.
/// </summary>
internal sealed class SignInWorld : IAsyncDisposable
{
    public const string Password = "Str0ng!Passw0rd";
    public static readonly Guid CareHome = Guid.NewGuid();

    private sealed class NoCaller : ICurrentUser
    {
        public Guid UserId => Guid.Empty;
        public Guid CareHomeId => Guid.Empty;
        public bool IsAuthenticated => false;
        public bool IsInRole(string role) => false;
    }

    private sealed class NoEvents : IDomainEventDispatcher
    {
        public Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken ct = default) => Task.CompletedTask;
    }

    /// <summary>Issues dummy tokens and records which sessions were ended and with what stamp tokens were minted.</summary>
    internal sealed class Sessions : IRefreshToken
    {
        public List<Guid> EndedFor { get; } = [];
        public string? LastStamp { get; private set; }

        public Task<Result<(string AccessToken, string RefreshToken)>> CreateTokensAsync(string name, Guid tokenFamily, StaffId staffId,
            Guid careHomeId, IEnumerable<string> roles, string securityStamp, CancellationToken cancellationToken, DateTime? sessionStartedAt = null)
        {
            LastStamp = securityStamp;
            return Task.FromResult(Result.Success(("access", "refresh")));
        }

        public Task<Result<(string AccessToken, string RefreshToken)>> RefreshTokenAsync(string currentRefreshToken, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<Result> RevokeFamilyAsync(string currentRefreshToken, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task RevokeAllForStaffAsync(StaffId staffId, CancellationToken cancellationToken)
        {
            EndedFor.Add(staffId.Value);
            return Task.CompletedTask;
        }
    }

    private readonly ServiceProvider _root;
    private readonly AsyncServiceScope _scope;

    public Sessions Tokens { get; } = new();
    public UserManager<ApplicationUser> Users => _scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    public AuthService Auth => new(Users, _scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>(), Tokens,
        Options.Create(new Hosting { BaseUrl = "https://vcare.example", ResetPasswordPageUrl = "https://app.vcare.example/reset" }));
    public StaffSessionValidator Validator => new(Users);

    private SignInWorld(ServiceProvider root)
    {
        _root = root;
        _scope = root.CreateAsyncScope();
    }

    public static async Task<SignInWorld> CreateAsync()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection();
        services.AddSingleton<ICurrentUser, NoCaller>();
        services.AddSingleton<IDomainEventDispatcher, NoEvents>();
        var database = Guid.NewGuid().ToString();
        services.AddDbContext<StaffDbContext>(o => o.UseInMemoryDatabase(database));
        services.AddIdentityCore<ApplicationUser>(StaffModule.ConfigureIdentity)
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<StaffDbContext>()
            .AddDefaultTokenProviders();
        var world = new SignInWorld(services.BuildServiceProvider());
        // Seeds the Admin and Carer roles from the model.
        await world._scope.ServiceProvider.GetRequiredService<StaffDbContext>().Database.EnsureCreatedAsync();
        return world;
    }

    /// <summary>A confirmed staff member who can sign in.</summary>
    public async Task<Guid> StaffAsync(string userName, string role)
    {
        var id = Guid.NewGuid();
        var created = await Auth.CreateApplicationUser(id, userName, $"{userName}@example.invalid", Password, "07000000000", role, CareHome);
        Assert.True(created.IsSuccess, created.IsFailure ? created.Error.ToString() : null);
        var user = (await Users.FindByIdAsync(id.ToString()))!;
        await Users.ConfirmEmailAsync(user, await Users.GenerateEmailConfirmationTokenAsync(user));
        return id;
    }

    /// <summary>What an access token issued now would carry for this staff member.</summary>
    public async Task<ClaimsPrincipal> SignedInAsync(Guid staffId)
    {
        var user = (await Users.FindByIdAsync(staffId.ToString()))!;
        return new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, staffId.ToString()), new Claim(StaffSessionValidator.StampClaim, user.SecurityStamp!)], "test"));
    }

    public async ValueTask DisposeAsync()
    {
        await _scope.DisposeAsync();
        await _root.DisposeAsync();
    }
}

public class SignInSecurityTests
{
    [Fact]
    public async Task Five_wrong_passwords_lock_the_account_even_against_the_right_one()
    {
        await using var world = await SignInWorld.CreateAsync();
        await world.StaffAsync("amaka", "Carer");

        for (var i = 0; i < 5; i++)
            await world.Auth.LoginAsync("amaka", "guess" + i, CancellationToken.None);
        var right = await world.Auth.LoginAsync("amaka", SignInWorld.Password, CancellationToken.None);

        Assert.Equal("Auth.LockedOut", right.Error.Code);
    }

    [Fact]
    public async Task A_successful_sign_in_starts_the_failure_count_again()
    {
        await using var world = await SignInWorld.CreateAsync();
        await world.StaffAsync("amaka", "Carer");
        for (var i = 0; i < 4; i++)
            await world.Auth.LoginAsync("amaka", "guess" + i, CancellationToken.None);
        Assert.True((await world.Auth.LoginAsync("amaka", SignInWorld.Password, CancellationToken.None)).IsSuccess);

        await world.Auth.LoginAsync("amaka", "one more typo", CancellationToken.None);
        var after = await world.Auth.LoginAsync("amaka", SignInWorld.Password, CancellationToken.None);

        Assert.True(after.IsSuccess);
    }

    [Fact]
    public async Task A_deactivated_staff_member_cannot_sign_in_and_their_sessions_end()
    {
        await using var world = await SignInWorld.CreateAsync();
        var leaver = await world.StaffAsync("leaver", "Carer");
        var session = await world.SignedInAsync(leaver);

        var deactivated = await world.Auth.DeactivateAsync(leaver, CancellationToken.None);
        var login = await world.Auth.LoginAsync("leaver", SignInWorld.Password, CancellationToken.None);

        Assert.True(deactivated.IsSuccess);
        Assert.Equal("Auth.Deactivated", login.Error.Code);
        Assert.Contains(leaver, world.Tokens.EndedFor);
        Assert.False(await world.Validator.IsCurrentAsync(session, CancellationToken.None));
    }

    [Fact]
    public async Task A_reactivated_staff_member_can_sign_in_again()
    {
        await using var world = await SignInWorld.CreateAsync();
        var returner = await world.StaffAsync("returner", "Carer");
        await world.Auth.DeactivateAsync(returner, CancellationToken.None);

        await world.Auth.ReactivateAsync(returner, CancellationToken.None);

        Assert.True((await world.Auth.LoginAsync("returner", SignInWorld.Password, CancellationToken.None)).IsSuccess);
    }

    [Fact]
    public async Task A_role_change_moves_the_sign_in_role_and_ends_existing_sessions()
    {
        await using var world = await SignInWorld.CreateAsync();
        var demoted = await world.StaffAsync("former.admin", "Admin");
        var session = await world.SignedInAsync(demoted);
        Assert.True(await world.Validator.IsCurrentAsync(session, CancellationToken.None));

        var changed = await world.Auth.ChangeRoleAsync(demoted, "Carer", CancellationToken.None);

        Assert.True(changed.IsSuccess);
        var user = (await world.Users.FindByIdAsync(demoted.ToString()))!;
        Assert.Equal(["Carer"], await world.Users.GetRolesAsync(user));
        Assert.False(await world.Validator.IsCurrentAsync(session, CancellationToken.None));
        Assert.Contains(demoted, world.Tokens.EndedFor);
    }

    [Fact]
    public async Task An_email_change_moves_the_sign_in_email_and_needs_confirming_again()
    {
        await using var world = await SignInWorld.CreateAsync();
        var staff = await world.StaffAsync("amaka", "Carer");

        var changed = await world.Auth.ChangeEmailAsync(staff, "amaka.new@example.invalid", CancellationToken.None);

        Assert.True(changed.IsSuccess);
        var user = (await world.Users.FindByIdAsync(staff.ToString()))!;
        Assert.Equal("amaka.new@example.invalid", user.Email);
        Assert.False(user.EmailConfirmed);
    }

    [Fact]
    public async Task A_password_reset_ends_existing_sessions()
    {
        await using var world = await SignInWorld.CreateAsync();
        var staff = await world.StaffAsync("amaka", "Carer");
        var session = await world.SignedInAsync(staff);
        var user = (await world.Users.FindByIdAsync(staff.ToString()))!;
        var token = await world.Users.GeneratePasswordResetTokenAsync(user);

        var reset = await world.Auth.ResetPasswordAsync("N3w!Passw0rd", user.Email!, token);

        Assert.True(reset.IsSuccess);
        Assert.False(await world.Validator.IsCurrentAsync(session, CancellationToken.None));
        Assert.Contains(staff, world.Tokens.EndedFor);
    }

    [Fact]
    public async Task A_reset_request_for_an_unknown_email_produces_no_link_and_no_error()
    {
        await using var world = await SignInWorld.CreateAsync();

        var result = await world.Auth.GetPasswordResetcode("nobody@example.invalid");

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task Tokens_carry_the_security_stamp_they_are_checked_against()
    {
        await using var world = await SignInWorld.CreateAsync();
        var staff = await world.StaffAsync("amaka", "Carer");

        await world.Auth.LoginAsync("amaka", SignInWorld.Password, CancellationToken.None);

        var user = (await world.Users.FindByIdAsync(staff.ToString()))!;
        Assert.Equal(user.SecurityStamp, world.Tokens.LastStamp);
    }
}

public class StaffAccountTests
{
    private static readonly Guid CareHome = Guid.NewGuid();

    private sealed class Repository(params Staff[] staff) : IStaffRepository
    {
        public Task<Staff?> GetByIdAsync(StaffId id, CancellationToken cancellationToken = default) =>
            Task.FromResult(staff.FirstOrDefault(s => s.Id == id));
        public Task<Staff?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) => Task.FromResult<Staff?>(null);
        public Task AddAsync(Staff s, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Update(Staff s) { }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task<int> CommitandSaveAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task<bool> OwnsStorageKeyAsync(StaffId staffId, string storageKey, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<int> CountActiveAdminsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(staff.Count(s => s.IsActive && s.Role == "Admin"));
    }

    /// <summary>Records what reached the sign-in side.</summary>
    private sealed class SignIn : IAuthService
    {
        public List<(Guid StaffId, string Role)> RoleChanges { get; } = [];
        public List<Guid> Deactivated { get; } = [];
        public Task<Result> ChangeRoleAsync(Guid staffId, string role, CancellationToken cancellationToken)
        {
            RoleChanges.Add((staffId, role));
            return Task.FromResult(Result.Success());
        }
        public Task<Result> ChangeEmailAsync(Guid staffId, string email, CancellationToken cancellationToken) => Task.FromResult(Result.Success());
        public Task<Result> DeactivateAsync(Guid staffId, CancellationToken cancellationToken)
        {
            Deactivated.Add(staffId);
            return Task.FromResult(Result.Success());
        }
        public Task<Result> ReactivateAsync(Guid staffId, CancellationToken cancellationToken) => Task.FromResult(Result.Success());
        public Task<Result<string?>> GetPasswordResetcode(string email) => Task.FromResult(Result.Success<string?>(null));

        public Task<Result> CreateApplicationUser(Guid staffId, string username, string email, string password, string phonenumber, string roleName, Guid careHomeId) => throw new NotSupportedException();
        public Task<Result<ApplicationUserResponse>> GetApplicationUserByEmailAsync(string email, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Result<(string AccessToken, string RefreshToken)>> LoginAsync(string userName, string password, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Result<(string AccessToken, string RefreshToken)>> RefreshAsync(string refreshToken, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Result> ValidateNewUserAsync(string username, string email, string password, string roleName) => throw new NotSupportedException();
        public Task<Result> LogoutAsync(string refreshToken, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Result<string>> GenerateEmailVerificationLinkAsync(string email) => Task.FromResult(Result.Success("https://vcare.example/verify"));
        public Task<bool> NeedsEmailVerificationAsync(string email) => throw new NotSupportedException();
        public Task<Result> VerifyEmailAsync(string email, string token) => throw new NotSupportedException();
        public Task<Result<bool>> ResetPasswordAsync(string password, string email, string token) => throw new NotSupportedException();
    }

    /// <summary>Fails the test if anything is sent.</summary>
    private sealed class NoEmail : INotification
    {
        public Task<Result> SendEmailVerificationLinkAsync(string email, string userName, string url, CancellationToken cancellationToken) => Task.FromResult(Result.Success());
        public Task<Result> SendPasswordResetLinkAsync(string email, string url, CancellationToken cancellationToken) => throw new InvalidOperationException("Nothing should be sent.");
    }

    private sealed class Caller(Guid userId) : ICurrentUser
    {
        public Guid UserId => userId;
        public Guid CareHomeId => CareHome;
        public bool IsAuthenticated => true;
        public bool IsInRole(string role) => true;
    }

    private static Staff Person(string role, string userName = "someone") =>
        Staff.Create("Grace", "Hopper", $"{userName}@example.invalid", "07000000000", role, "2 Harbour Row", userName, CareHome);

    private static (StaffService Service, SignIn SignIn) Service(Guid caller, params Staff[] staff)
    {
        var signIn = new SignIn();
        return (new StaffService(new Repository(staff), signIn, new Caller(caller), null!,
            Options.Create(new StorageEntity { BaseUrl = "https://example.invalid", BucketName = "vcare" }),
            new NoEmail(), NullLogger<StaffService>.Instance), signIn);
    }

    [Fact]
    public async Task A_role_that_does_not_exist_is_refused()
    {
        var carer = Person("Carer");
        var (service, signIn) = Service(Guid.NewGuid(), carer);

        var result = await service.UpdateStaffAsync(carer.Id, new UpdateStaffRequest(Role: "Superuser"), CancellationToken.None);

        Assert.Equal("Staffs.InvalidRole", result.Error.Code);
        Assert.Equal("Carer", carer.Role);
        Assert.Empty(signIn.RoleChanges);
    }

    [Fact]
    public async Task A_role_change_reaches_the_sign_in_record()
    {
        var carer = Person("Carer");
        var (service, signIn) = Service(Guid.NewGuid(), carer, Person("Admin", "boss"));

        var result = await service.UpdateStaffAsync(carer.Id, new UpdateStaffRequest(Role: "admin"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Admin", carer.Role);
        Assert.Equal([(carer.Id.Value, "Admin")], signIn.RoleChanges);
    }

    [Fact]
    public async Task The_last_admin_cannot_be_made_a_carer()
    {
        var admin = Person("Admin");
        var (service, signIn) = Service(Guid.NewGuid(), admin, Person("Carer", "carer"));

        var result = await service.UpdateStaffAsync(admin.Id, new UpdateStaffRequest(Role: "Carer"), CancellationToken.None);

        Assert.Equal("Staffs.LastAdmin", result.Error.Code);
        Assert.Empty(signIn.RoleChanges);
    }

    [Fact]
    public async Task Staff_cannot_deactivate_themselves()
    {
        var admin = Person("Admin");
        var (service, signIn) = Service(admin.Id.Value, admin, Person("Admin", "other"));

        var result = await service.DeactivateStaffAsync(admin.Id, new DeactivateStaffRequest("Leaving"), CancellationToken.None);

        Assert.Equal("Staffs.CannotDeactivateSelf", result.Error.Code);
        Assert.Empty(signIn.Deactivated);
    }

    [Fact]
    public async Task Deactivating_staff_ends_their_access_and_records_who_and_why()
    {
        var admin = Person("Admin", "admin");
        var leaver = Person("Carer", "leaver");
        var (service, signIn) = Service(admin.Id.Value, admin, leaver);

        var result = await service.DeactivateStaffAsync(leaver.Id, new DeactivateStaffRequest("Left on 20 November"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(leaver.IsActive);
        Assert.Equal("Left on 20 November", leaver.DeactivationReason);
        Assert.Equal(admin.Id.Value, leaver.DeactivatedByStaffId);
        Assert.Equal([leaver.Id.Value], signIn.Deactivated);
    }

    [Fact]
    public async Task A_reset_request_for_an_unknown_email_succeeds_and_sends_nothing()
    {
        var (service, _) = Service(Guid.NewGuid());

        var result = await service.RequestPasswordResetAsync("nobody@example.invalid", CancellationToken.None);

        Assert.True(result.IsSuccess);
    }
}

public class SessionLengthTests
{
    private sealed class Clock(DateTime start) : TimeProvider
    {
        public DateTime Now { get; set; } = start;
        public override DateTimeOffset GetUtcNow() => new(DateTime.SpecifyKind(Now, DateTimeKind.Utc));
    }

    private sealed class Jwt : IJwtTokenService
    {
        public Result<string> CreateToken(string name, StaffId staffId, Guid careHomeId, IEnumerable<string> roles, string securityStamp) =>
            Result.Success("access");
    }

    private sealed class Store : IRefreshTokenRepository
    {
        private readonly List<RefreshToken> _tokens = [];
        public Task<RefreshToken?> GetByHashAsync(byte[] tokenHash, CancellationToken cancellationToken) =>
            Task.FromResult(_tokens.FirstOrDefault(t => t.TokenHash.SequenceEqual(tokenHash)));
        public Task<IEnumerable<RefreshToken>> GetAllActiveTokenAsync(Guid TokenFamilyId, CancellationToken cancellationToken) =>
            Task.FromResult<IEnumerable<RefreshToken>>(_tokens.Where(t => t.TokenFamily == TokenFamilyId && !t.IsRevoked).ToList());
        public Task<IEnumerable<RefreshToken>> GetAllActiveForStaffAsync(Guid staffId, CancellationToken cancellationToken) =>
            Task.FromResult<IEnumerable<RefreshToken>>(_tokens.Where(t => t.StaffId == staffId && !t.IsRevoked).ToList());
        public Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken)
        {
            _tokens.Add(refreshToken);
            return Task.CompletedTask;
        }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(0);
    }

    private sealed class Principal : IStaffPrincipalReader
    {
        public Task<Result<StaffPrincipal>> GetByStaffIdAsync(StaffId staffId, CancellationToken cancellationToken) =>
            Task.FromResult(Result.Success(new StaffPrincipal("amaka", Guid.NewGuid(), ["Carer"], "stamp")));
    }

    [Fact]
    public async Task A_session_cannot_be_kept_alive_past_its_maximum_length_by_refreshing()
    {
        var start = new DateTime(2026, 11, 20, 7, 0, 0);
        var clock = new Clock(start);
        var tokens = new RefreshTokenGenerator(new Jwt(), new Store(), new Principal(),
            Options.Create(new JwtSettings { Key = "k", Issuer = "i", Audience = "a", DurationInMinutes = 30, MaxSessionHours = 12 }), clock);
        var issued = await tokens.CreateTokensAsync("amaka", Guid.NewGuid(), new StaffId(Guid.NewGuid()), Guid.NewGuid(), ["Carer"], "stamp",
            CancellationToken.None);
        var refresh = issued.Value.RefreshToken;

        // A refresh every 55 minutes, through a 12-hour shift.
        Result<(string AccessToken, string RefreshToken)> last = issued;
        while (clock.Now + TimeSpan.FromMinutes(55) < start.AddHours(12))
        {
            clock.Now += TimeSpan.FromMinutes(55);
            last = await tokens.RefreshTokenAsync(refresh, CancellationToken.None);
            Assert.True(last.IsSuccess, $"refresh at {clock.Now:HH:mm} failed");
            refresh = last.Value.RefreshToken;
        }
        clock.Now += TimeSpan.FromMinutes(55);
        var beyond = await tokens.RefreshTokenAsync(refresh, CancellationToken.None);

        Assert.Equal("Auth.InvalidToken", beyond.Error.Code);
    }
}
