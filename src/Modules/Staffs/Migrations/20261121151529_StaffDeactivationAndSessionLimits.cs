using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VCare.Modules.Staffs.Migrations
{
    /// <inheritdoc />
    public partial class StaffDeactivationAndSessionLimits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeactivatedAt",
                schema: "Auth",
                table: "Staffs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeactivatedByStaffId",
                schema: "Auth",
                table: "Staffs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeactivationReason",
                schema: "Auth",
                table: "Staffs",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SessionStartedAt",
                schema: "Auth",
                table: "RefreshTokens",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeactivatedAt",
                schema: "Auth",
                table: "AspNetUsers",
                type: "datetimeoffset",
                nullable: true);

            // A live session started no later than its current token was issued.
            migrationBuilder.Sql("UPDATE [Auth].[RefreshTokens] SET [SessionStartedAt] = [CreatedTime];");

            // Lockout only counts failures for users with it enabled, and users created
            // before it was configured have it off.
            migrationBuilder.Sql("UPDATE [Auth].[AspNetUsers] SET [LockoutEnabled] = 1;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeactivatedAt",
                schema: "Auth",
                table: "Staffs");

            migrationBuilder.DropColumn(
                name: "DeactivatedByStaffId",
                schema: "Auth",
                table: "Staffs");

            migrationBuilder.DropColumn(
                name: "DeactivationReason",
                schema: "Auth",
                table: "Staffs");

            migrationBuilder.DropColumn(
                name: "SessionStartedAt",
                schema: "Auth",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "DeactivatedAt",
                schema: "Auth",
                table: "AspNetUsers");
        }
    }
}
