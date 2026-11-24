using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VCare.Modules.Staffs.Migrations
{
    /// <summary>
    /// Removes the Outbox module's tables. Nothing had written to the queue since
    /// emails moved to direct sending, and its drain no longer ran, so any row left
    /// is an old email nobody will send. Lives here because Staffs was the only
    /// module that wrote to it. A database that never had the outbox is untouched.
    /// </summary>
    public partial class DropOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF OBJECT_ID(N'[Outbox].[OutboxMessages]', N'U') IS NOT NULL DROP TABLE [Outbox].[OutboxMessages];
                IF OBJECT_ID(N'[Outbox].[__EFMigrationsHistory]', N'U') IS NOT NULL DROP TABLE [Outbox].[__EFMigrationsHistory];
                IF SCHEMA_ID(N'Outbox') IS NOT NULL EXEC(N'DROP SCHEMA [Outbox]');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Nothing to restore: the module and its migrations are gone.
        }
    }
}
