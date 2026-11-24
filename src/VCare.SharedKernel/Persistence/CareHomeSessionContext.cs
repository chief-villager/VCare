using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using VCare.SharedKernel.Abstractions;

namespace VCare.SharedKernel.Persistence;

/// <summary>
/// Tells SQL Server whose data a connection may touch, each time EF opens one.
/// Row-level security (see <see cref="RowLevelSecurity"/>) then filters every
/// read and refuses every write outside that care home, under the query filters.
/// <list type="bullet">
/// <item>A signed-in caller: their care home only. One with no care home claim sees nothing.</item>
/// <item>No signed-in caller (sign-in, registration, password reset, background
/// jobs, migrations): every care home, as those flows already work across homes.</item>
/// </list>
/// The value is read-only for the session; a pooled connection is reset before
/// reuse, which clears it.
/// </summary>
public sealed class CareHomeSessionContext(ICurrentUser currentUser) : DbConnectionInterceptor
{
    public const string CareHomeKey = "CareHomeId";
    public const string AllCareHomesKey = "AllCareHomes";

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        using var command = Command(connection);
        command.ExecuteNonQuery();
    }

    public override async Task ConnectionOpenedAsync(DbConnection connection, ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        await using var command = Command(connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private DbCommand Command(DbConnection connection)
    {
        var command = connection.CreateCommand();
        var value = command.CreateParameter();
        value.ParameterName = "@value";
        if (currentUser.IsAuthenticated)
        {
            command.CommandText = $"EXEC sp_set_session_context @key = N'{CareHomeKey}', @value = @value, @read_only = 1;";
            value.Value = currentUser.CareHomeId;
        }
        else
        {
            command.CommandText = $"EXEC sp_set_session_context @key = N'{AllCareHomesKey}', @value = @value, @read_only = 1;";
            value.Value = true;
        }
        command.Parameters.Add(value);
        return command;
    }
}
