namespace VCare.SharedKernel.Persistence;

/// <summary>
/// SQL for the row-level security that keeps care homes apart in the database.
/// Each module's migration protects its own tables with its own policy. Migrations
/// already applied depend on this text: change it with a new member, not an edit.
/// </summary>
public static class RowLevelSecurity
{
    /// <summary>
    /// The predicate every policy uses: a row is visible, and may be written, when
    /// its care home is the connection's (CareHomeSessionContext), or when the
    /// connection is allowed every home. Safe to run more than once, in any order.
    /// </summary>
    public const string EnsurePredicate = """
        IF SCHEMA_ID(N'security') IS NULL EXEC(N'CREATE SCHEMA [security]');
        IF OBJECT_ID(N'[security].[fn_CareHomeAccess]') IS NULL EXEC(N'
        CREATE FUNCTION [security].[fn_CareHomeAccess](@CareHomeId uniqueidentifier)
        RETURNS TABLE
        WITH SCHEMABINDING
        AS RETURN
            SELECT 1 AS Allowed
            WHERE @CareHomeId = CAST(SESSION_CONTEXT(N''CareHomeId'') AS uniqueidentifier)
               OR CAST(SESSION_CONTEXT(N''AllCareHomes'') AS bit) = 1');
        """;

    /// <summary>
    /// A policy filtering reads and blocking writes outside the connection's care
    /// home, on each (table, care home column) given, written as [schema].[table].
    /// </summary>
    public static string CreatePolicy(string policy, params (string Table, string Column)[] tables) =>
        $"CREATE SECURITY POLICY {policy}\n"
        + string.Join(",\n", tables.SelectMany(t => new[]
        {
            $"    ADD FILTER PREDICATE [security].[fn_CareHomeAccess]([{t.Column}]) ON {t.Table}",
            $"    ADD BLOCK PREDICATE [security].[fn_CareHomeAccess]([{t.Column}]) ON {t.Table} AFTER INSERT",
            $"    ADD BLOCK PREDICATE [security].[fn_CareHomeAccess]([{t.Column}]) ON {t.Table} AFTER UPDATE",
        }))
        + "\nWITH (STATE = ON);";

    public static string DropPolicy(string policy) => $"DROP SECURITY POLICY IF EXISTS {policy};";
}
