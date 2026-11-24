using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VCare.Modules.CareHome.Migrations
{
    /// <inheritdoc />
    public partial class CareHomeTimeZone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Existing homes get UTC, which is what they ran on if the server was set to
            // UTC. Set each home's real zone (PUT /api/carehome/me) before relying on
            // dose times: a home in UTC+1 would otherwise have every window an hour off.
            migrationBuilder.AddColumn<string>(
                name: "TimeZoneId",
                schema: "CareHomes",
                table: "CareHomes",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "UTC");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TimeZoneId",
                schema: "CareHomes",
                table: "CareHomes");
        }
    }
}
