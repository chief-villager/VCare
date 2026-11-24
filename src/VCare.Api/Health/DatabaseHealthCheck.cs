using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace VCare.Api.Health;

/// <summary>
/// Ready only when the database answers. Every module keeps its data in the one
/// database, so without it no request can do anything useful; a load balancer
/// should stop sending traffic rather than let callers find out one 500 at a time.
/// Uses its own connection, not the request-scoped one the modules share.
/// </summary>
internal sealed class DatabaseHealthCheck(IConfiguration configuration) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new SqlConnection(configuration.GetConnectionString("Default"));
            await connection.OpenAsync(cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            await command.ExecuteScalarAsync(cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // The exception stays in the logs; the response says only that the
            // database is unreachable, not why.
            return HealthCheckResult.Unhealthy("The database is unreachable.", ex);
        }
    }
}
