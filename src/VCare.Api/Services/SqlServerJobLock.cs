using System.Data;
using Microsoft.Data.SqlClient;
using VCare.SharedKernel.Abstractions;

namespace VCare.Api.Services
{
    /// <summary>
    /// <see cref="IJobLock"/> over SQL Server application locks (sp_getapplock).
    /// The lock belongs to a connection held open for as long as the handle lives.
    /// Pooling is off for that connection, so closing it really ends the session:
    /// if an instance dies mid-pass, the database drops its lock with the session
    /// instead of leaving it on a pooled connection.
    /// </summary>
    public sealed class SqlServerJobLock : IJobLock
    {
        private readonly string _connectionString;

        public SqlServerJobLock(string connectionString)
        {
            _connectionString = new SqlConnectionStringBuilder(connectionString) { Pooling = false }.ConnectionString;
        }

        public async Task<IAsyncDisposable?> TryAcquireAsync(string name, CancellationToken cancellationToken)
        {
            var connection = new SqlConnection(_connectionString);
            try
            {
                await connection.OpenAsync(cancellationToken);
                var granted = await CallAsync(connection, "sp_getapplock", name, cancellationToken,
                    ("@LockMode", "Exclusive"), ("@LockTimeout", 0));
                // 0: granted, 1: granted after waiting; negative: not granted.
                if (granted >= 0)
                {
                    return new Held(connection, name);
                }
            }
            catch
            {
                await connection.DisposeAsync();
                throw;
            }
            await connection.DisposeAsync();
            return null;
        }

        private static async Task<int> CallAsync(SqlConnection connection, string procedure, string name,
            CancellationToken cancellationToken, params (string Name, object Value)[] extra)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = procedure;
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.Add(new SqlParameter("@Resource", SqlDbType.NVarChar, 255) { Value = name });
            command.Parameters.Add(new SqlParameter("@LockOwner", SqlDbType.VarChar, 32) { Value = "Session" });
            foreach (var (parameter, value) in extra)
            {
                command.Parameters.AddWithValue(parameter, value);
            }
            var result = command.Parameters.Add(new SqlParameter("@Result", SqlDbType.Int) { Direction = ParameterDirection.ReturnValue });
            await command.ExecuteNonQueryAsync(cancellationToken);
            return (int)result.Value;
        }

        private sealed class Held(SqlConnection connection, string name) : IAsyncDisposable
        {
            public async ValueTask DisposeAsync()
            {
                try
                {
                    // Closing the unpooled connection would drop the lock anyway;
                    // releasing first frees it before the close round trip.
                    await CallAsync(connection, "sp_releaseapplock", name, CancellationToken.None);
                }
                catch (SqlException)
                {
                    // The session is closing below, which releases the lock.
                }
                finally
                {
                    await connection.DisposeAsync();
                }
            }
        }
    }
}
