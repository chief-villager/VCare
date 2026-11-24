using System.Diagnostics;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Trace;
using Xunit;

namespace VCare.Api.Tests;

// Spans are what lets someone follow one slow or failing request across the
// API, the database and S3. They are produced whether or not an exporter is
// configured; OTEL_EXPORTER_OTLP_ENDPOINT decides whether they leave the process.
public class TracingTests
{
    [Fact]
    public async Task An_incoming_request_is_traced_but_health_checks_are_not()
    {
        var exported = new List<Activity>();
        await using var api = new ApiFactory().WithWebHostBuilder(b => b.ConfigureTestServices(services =>
            services.ConfigureOpenTelemetryTracerProvider(tracing => tracing.AddInMemoryExporter(exported))));
        using var client = api.CreateClient();

        // A load balancer calls the health checks every few seconds: tracing each
        // call would bury the requests worth looking at.
        (await client.GetAsync("/health/live")).EnsureSuccessStatusCode();
        await client.GetAsync("/api/patient");

        // The server span ends just after the response is written.
        for (var i = 0; i < 40 && !Traced(exported, "/api/patient"); i++)
        {
            await Task.Delay(50);
        }
        Assert.True(Traced(exported, "/api/patient"), string.Join(", ", exported.Select(a => a.DisplayName)));
        Assert.False(Traced(exported, "/health/live"));
    }

    private static bool Traced(List<Activity> exported, string path)
    {
        lock (exported)
        {
            return exported.Any(a => a.Kind == ActivityKind.Server && (string?)a.GetTagItem("url.path") == path);
        }
    }
}
