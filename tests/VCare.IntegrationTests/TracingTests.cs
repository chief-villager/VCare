using System.Diagnostics;
using System.Net.Http.Json;
using Microsoft.AspNetCore.TestHost;
using OpenTelemetry.Trace;
using Xunit;

namespace VCare.IntegrationTests;

[Collection(SqlServerCollection.Name)]
public class TracingTests(SqlServerFixture sql)
{
    [Fact]
    public async Task The_sql_a_request_runs_is_traced_under_that_request()
    {
        var exported = new List<Activity>();
        await using var api = sql.Api.WithWebHostBuilder(b => b.ConfigureTestServices(services =>
            services.ConfigureOpenTelemetryTracerProvider(tracing => tracing.AddInMemoryExporter(exported))));
        using var client = api.CreateClient();

        // Looks the user up in the database, then refuses.
        await client.PostAsJsonAsync("/api/auth/login", new { userName = "nobody", password = "wrong-password" });

        Activity? request = null;
        for (var i = 0; i < 40 && request is null; i++)
        {
            await Task.Delay(50);
            lock (exported)
            {
                request = exported.FirstOrDefault(a => a.Kind == ActivityKind.Server && (string?)a.GetTagItem("url.path") == "/api/auth/login");
            }
        }
        Assert.NotNull(request);
        lock (exported)
        {
            Assert.Contains(exported, a => a.Kind == ActivityKind.Client
                && a.Source.Name.Contains("SqlClient", StringComparison.Ordinal)
                && a.TraceId == request.TraceId);
        }
    }
}
