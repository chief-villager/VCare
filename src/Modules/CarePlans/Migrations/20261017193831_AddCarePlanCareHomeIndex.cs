using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VCare.Modules.CarePlans.Migrations
{
    /// <inheritdoc />
    public partial class AddCarePlanCareHomeIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_CarePlans_CareHomeId",
                schema: "CarePlan",
                table: "CarePlans",
                column: "CareHomeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CarePlans_CareHomeId",
                schema: "CarePlan",
                table: "CarePlans");
        }
    }
}
