using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Staffs.Application.Abstraction;
using Staffs.Application.Dto;
using Staffs.Application.Services;
using Staffs.Application.Services.Interface;
using Staffs.Domain.Entity;
using VCare.SharedKernel.Abstractions;
using VCare.SharedKernel.Domain;
using VCare.SharedKernel.Results;
using Xunit;

namespace VCare.Modules.Staffs.Tests;

public class StaffDocumentTests
{
    private static readonly Guid CareHome = Guid.NewGuid();
    private const string OwnFile = "home-a/documents/2a_dbs-certificate.pdf";
    private const string ResidentsFile = "home-a/documents/7d_hospital-letter.pdf";

    private sealed class FakeStaff(Staff staff) : IStaffRepository
    {
        public Task<Staff?> GetByIdAsync(StaffId id, CancellationToken cancellationToken = default) =>
            Task.FromResult(id == staff.Id ? staff : null);
        public Task<Staff?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) => Task.FromResult<Staff?>(null);
        public Task AddAsync(Staff s, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Update(Staff s) { }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task<int> CommitandSaveAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task<int> CountActiveAdminsAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
        public Task<bool> OwnsStorageKeyAsync(StaffId staffId, string storageKey, CancellationToken cancellationToken = default) =>
            Task.FromResult(staffId == staff.Id
                && (staff.ProfilePictureUrl == storageKey || staff.GetDocuments().Any(d => d.StorageKeyName == storageKey)));
    }

    // The document path touches neither sign-in nor email.
    private sealed class NoAuth : IAuthService
    {
        public Task<Result> CreateApplicationUser(Guid staffId, string username, string email, string password, string phonenumber, string roleName, Guid careHomeId) => throw new NotSupportedException();
        public Task<Result<ApplicationUserResponse>> GetApplicationUserByEmailAsync(string email, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Result<(string AccessToken, string RefreshToken)>> LoginAsync(string userName, string password, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Result<(string AccessToken, string RefreshToken)>> RefreshAsync(string refreshToken, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Result> ValidateNewUserAsync(string username, string email, string password, string roleName) => throw new NotSupportedException();
        public Task<Result> LogoutAsync(string refreshToken, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Result<string>> GenerateEmailVerificationLinkAsync(string email) => throw new NotSupportedException();
        public Task<bool> NeedsEmailVerificationAsync(string email) => throw new NotSupportedException();
        public Task<Result> VerifyEmailAsync(string email, string token) => throw new NotSupportedException();
        public Task<Result<string?>> GetPasswordResetcode(string email) => throw new NotSupportedException();
        public Task<Result> ChangeRoleAsync(Guid staffId, string role, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Result> ChangeEmailAsync(Guid staffId, string email, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Result> DeactivateAsync(Guid staffId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Result> ReactivateAsync(Guid staffId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Result<bool>> ResetPasswordAsync(string password, string email, string token) => throw new NotSupportedException();
    }

    private sealed class NoEmail : INotification
    {
        public Task<Result> SendEmailVerificationLinkAsync(string email, string userName, string url, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Result> SendPasswordResetLinkAsync(string email, string url, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class FakeCurrentUser : ICurrentUser
    {
        public Guid UserId { get; } = Guid.NewGuid();
        public Guid CareHomeId => CareHome;
        public bool IsAuthenticated => true;
        public bool IsInRole(string role) => true;
    }

    /// <summary>Hands back any object it is asked for, as a bucket would; records what was asked.</summary>
    private sealed class Bucket : IDocumentService
    {
        public List<string> Fetched { get; } = [];
        public Task<(Stream Content, string ContentType)> GetObjectAsync(string objectKey, string sourceBucket, CancellationToken cancellationToken = default)
        {
            Fetched.Add(objectKey);
            return Task.FromResult<(Stream, string)>((new MemoryStream([1, 2, 3]), "application/pdf"));
        }
        public Task<(Stream Content, string ContentType)> GetProfileImageAsync(string objectKey, string sourceBucket, CancellationToken cancellationToken = default) =>
            GetObjectAsync(objectKey, sourceBucket, cancellationToken);
        public Task<Result<string>> WritingAnObjectAsync(string bucketName, Stream content, string fileName, string contentType, string careHomeId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<Result<string>> UploadProfileImageAsync(string bucketName, Stream content, string fileName, string contentType, string careHomeId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private static (StaffService Service, Bucket Bucket, Staff Staff) CarerWithACertificate()
    {
        var staff = Staff.Create("Grace", "Hopper", "grace@example.invalid", "07000000000", "Carer", "2 Harbour Row", "ghopper", CareHome);
        staff.AddDocument("DBS certificate", OwnFile);
        var bucket = new Bucket();
        var service = new StaffService(new FakeStaff(staff), new NoAuth(), new FakeCurrentUser(), bucket,
            Options.Create(new StorageEntity { BaseUrl = "https://example.invalid", BucketName = "vcare" }),
            new NoEmail(), NullLogger<StaffService>.Instance);
        return (service, bucket, staff);
    }

    [Fact]
    public async Task A_key_that_is_not_one_of_the_staff_members_documents_is_not_fetched()
    {
        var (service, bucket, staff) = CarerWithACertificate();

        var result = await service.GetStaffDocumentsAsync(staff.Id, ResidentsFile, CancellationToken.None);

        Assert.Equal(ErrorKind.NotFound, result.Error.Kind);
        Assert.Empty(bucket.Fetched);
    }

    [Fact]
    public async Task The_staff_members_own_document_is_fetched()
    {
        var (service, bucket, staff) = CarerWithACertificate();

        var result = await service.GetStaffDocumentsAsync(staff.Id, OwnFile, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal([OwnFile], bucket.Fetched);
    }
}
