using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace VCare.Api.Tests;

// Minimal APIs mark every endpoint that binds a form or a file as needing
// antiforgery validation. The app has no antiforgery middleware, because it
// signs callers in with bearer tokens only and so has no cookie for a forged
// cross-site request to ride on. An endpoint still carrying that mark throws
// on every request: every document and profile-image upload returned 500.
public class EndpointAntiforgeryTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private IReadOnlyList<Endpoint> Endpoints =>
        factory.Services.GetRequiredService<EndpointDataSource>().Endpoints;

    [Fact]
    public void No_endpoint_asks_for_antiforgery_validation()
    {
        var demanding = Endpoints
            .Where(e => e.Metadata.GetMetadata<IAntiforgeryMetadata>()?.RequiresValidation == true)
            .Select(e => e.DisplayName)
            .ToList();

        Assert.Empty(demanding);
    }

    // Guards the test above: it proves nothing if the upload routes aren't mapped.
    [Theory]
    [InlineData("POST /api/patient/{patientId:Guid}/document")]
    [InlineData("POST /api/patient/{patientId:Guid}/profile-image")]
    [InlineData("POST /api/staff/{staffId:Guid}/document")]
    [InlineData("POST /api/staff/{staffId:Guid}/profile-image")]
    public void The_upload_endpoint_is_mapped(string route)
    {
        Assert.Contains(Endpoints, e => e.DisplayName?.Contains(route) == true);
    }
}

// The API with just enough configuration to start. Nothing here connects to
// the database, S3 or a mail server.
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", "Server=localhost,1;Database=unused;User Id=unused;Password=unused;TrustServerCertificate=true");
        builder.UseSetting("Jwt:Key", new string('k', 64));
        builder.UseSetting("Jwt:Issuer", "vcare-tests");
        builder.UseSetting("Jwt:Audience", "vcare-tests");
        builder.UseSetting("Jwt:DurationInMinutes", "15");
        builder.UseSetting("Hosting:BaseUrl", "http://localhost");
        builder.UseSetting("Hosting:ResetPasswordPageUrl", "http://localhost/reset");
        builder.UseSetting("Smtp:Host", "localhost");
        builder.UseSetting("Smtp:FromAddress", "noreply@vcare.test");
        builder.UseSetting("Storage:BaseUrl", "http://localhost");
        builder.UseSetting("Storage:BucketName", "unused");
        builder.UseSetting("BackgroundJobs:Enabled", "false");
    }
}
