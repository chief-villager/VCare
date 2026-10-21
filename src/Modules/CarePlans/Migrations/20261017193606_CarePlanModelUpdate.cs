using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VCare.Modules.CarePlans.Migrations
{
    /// <inheritdoc />
    public partial class CarePlanModelUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CarePlans_CareHomeId",
                schema: "CarePlan",
                table: "CarePlans");

            migrationBuilder.DropIndex(
                name: "IX_CarePlans_PatientId",
                schema: "CarePlan",
                table: "CarePlans");

            migrationBuilder.CreateIndex(
                name: "IX_CarePlans_PatientId",
                schema: "CarePlan",
                table: "CarePlans",
                column: "PatientId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CarePlans_PatientId",
                schema: "CarePlan",
                table: "CarePlans");

            migrationBuilder.CreateIndex(
                name: "IX_CarePlans_CareHomeId",
                schema: "CarePlan",
                table: "CarePlans",
                column: "CareHomeId");

            migrationBuilder.CreateIndex(
                name: "IX_CarePlans_PatientId",
                schema: "CarePlan",
                table: "CarePlans",
                column: "PatientId");
        }
    }
}
