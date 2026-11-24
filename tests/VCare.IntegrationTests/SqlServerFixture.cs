using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;
using Xunit;

namespace VCare.IntegrationTests;

/// <summary>
/// One SQL Server in Docker for the whole run, migrated by the real app, and the
/// API wired to it. What EF InMemory can't show (locks, row-level security, the
/// SQL the audit trail writes) is tested here.
/// </summary>
public sealed class SqlServerFixture : IAsyncLifetime
{
    // Every module's DbContext, in the order scripts/migrations.sh applies them.
    internal static readonly string[] MigrationOrder =
    [
        "AuditDbContext", "CareHomeDbContext", "StaffDbContext", "PatientsDbContext",
        "CarePlanDbContext", "MedicationDbContext", "VisitationDbContext",
    ];

    private readonly MsSqlContainer _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    public string ConnectionString { get; private set; } = string.Empty;
    public VCareApi Api { get; private set; } = null!;
    public TestHomes Homes { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        ConnectionString = new SqlConnectionStringBuilder(_container.GetConnectionString())
        {
            InitialCatalog = "VCare",
            TrustServerCertificate = true,
        }.ConnectionString;
        Api = new VCareApi(ConnectionString);
        await Api.MigrateAsync();
        Homes = new TestHomes(this);
    }

    public async Task DisposeAsync()
    {
        await Api.DisposeAsync();
        await _container.DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class SqlServerCollection : ICollectionFixture<SqlServerFixture>
{
    public const string Name = "SQL Server";
}

/// <summary>The API with just enough configuration to start, on the test database.</summary>
public sealed class VCareApi(string connectionString) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", connectionString);
        builder.UseSetting("Jwt:Key", new string('k', 64));
        builder.UseSetting("Jwt:Issuer", "vcare-tests");
        builder.UseSetting("Jwt:Audience", "vcare-tests");
        builder.UseSetting("Jwt:DurationInMinutes", "15");
        builder.UseSetting("Hosting:BaseUrl", "http://localhost");
        builder.UseSetting("Hosting:ResetPasswordPageUrl", "http://localhost/reset");
        // Nothing listens here: a send fails, is logged, and the request carries on.
        builder.UseSetting("Smtp:Host", "127.0.0.1");
        builder.UseSetting("Smtp:Port", "1");
        builder.UseSetting("Smtp:FromAddress", "noreply@vcare.test");
        builder.UseSetting("Storage:BaseUrl", "http://localhost");
        builder.UseSetting("Storage:BucketName", "unused");
        builder.UseSetting("BackgroundJobs:Enabled", "false");
    }

    /// <summary>Every module's DbContext types, found in the loaded module assemblies.</summary>
    internal Dictionary<string, Type> ContextTypes()
    {
        _ = Services; // builds the host, which loads every module
        return AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => a.GetName().Name?.StartsWith("VCare.Modules.", StringComparison.Ordinal) == true)
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(DbContext).IsAssignableFrom(t) && !t.IsAbstract)
            .ToDictionary(t => t.Name);
    }

    public async Task MigrateAsync()
    {
        var types = ContextTypes();
        using var scope = Services.CreateScope();
        foreach (var name in SqlServerFixture.MigrationOrder)
        {
            var context = (DbContext)scope.ServiceProvider.GetRequiredService(types[name]);
            await context.Database.MigrateAsync();
        }
    }
}
