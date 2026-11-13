using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vcare.Modules.Medications.Migrations
{
    /// <inheritdoc />
    public partial class UniqueAdministrationPerSlot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MedicationAdminstration_MedicationOrderId",
                schema: "Medication",
                table: "MedicationAdminstration");

            migrationBuilder.CreateIndex(
                name: "IX_MedicationAdminstration_MedicationOrderId_ScheduledFor",
                schema: "Medication",
                table: "MedicationAdminstration",
                columns: new[] { "MedicationOrderId", "ScheduledFor" },
                unique: true,
                filter: "[ScheduledFor] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MedicationAdminstration_MedicationOrderId_ScheduledFor",
                schema: "Medication",
                table: "MedicationAdminstration");

            migrationBuilder.CreateIndex(
                name: "IX_MedicationAdminstration_MedicationOrderId",
                schema: "Medication",
                table: "MedicationAdminstration",
                column: "MedicationOrderId");
        }
    }
}
