using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vcare.Modules.Medications.Migrations
{
    /// <inheritdoc />
    public partial class SeedMissedOutcomeCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "Medication",
                table: "OutcomeCode",
                columns: new[] { "Id", "DisplayLetter", "Name" },
                values: new object[] { new Guid("db5662a7-5f67-4890-841f-c567d890e123"), "M", "Missed" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "Medication",
                table: "OutcomeCode",
                keyColumn: "Id",
                keyValue: new Guid("db5662a7-5f67-4890-841f-c567d890e123"));
        }
    }
}
