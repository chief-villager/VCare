using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vcare.Modules.Medications.Migrations
{
    /// <inheritdoc />
    public partial class MedicationSafetyRecording : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PrnMaxDosesPer24h",
                schema: "Medication",
                table: "MedicationOrder",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StatusChangedAt",
                schema: "Medication",
                table: "MedicationOrder",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DoseGiven",
                schema: "Medication",
                table: "MedicationAdminstration",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsLate",
                schema: "Medication",
                table: "MedicationAdminstration",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Reason",
                schema: "Medication",
                table: "MedicationAdminstration",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrnMaxDosesPer24h",
                schema: "Medication",
                table: "MedicationOrder");

            migrationBuilder.DropColumn(
                name: "StatusChangedAt",
                schema: "Medication",
                table: "MedicationOrder");

            migrationBuilder.DropColumn(
                name: "DoseGiven",
                schema: "Medication",
                table: "MedicationAdminstration");

            migrationBuilder.DropColumn(
                name: "IsLate",
                schema: "Medication",
                table: "MedicationAdminstration");

            migrationBuilder.DropColumn(
                name: "Reason",
                schema: "Medication",
                table: "MedicationAdminstration");
        }
    }
}
