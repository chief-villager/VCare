using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VCare.Modules.CarePlans.Migrations
{
    /// <inheritdoc />
    public partial class initialCarePlanMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "CarePlan");

            migrationBuilder.CreateTable(
                name: "CarePlans",
                schema: "CarePlan",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CareHomeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ModifiedDate = table.Column<DateOnly>(type: "date", nullable: false),
                    StaffId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarePlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CarePlanDiagnoses",
                schema: "CarePlan",
                columns: table => new
                {
                    CarePlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarePlanDiagnoses", x => new { x.CarePlanId, x.Id });
                    table.ForeignKey(
                        name: "FK_CarePlanDiagnoses_CarePlans_CarePlanId",
                        column: x => x.CarePlanId,
                        principalSchema: "CarePlan",
                        principalTable: "CarePlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CarePlanGoals",
                schema: "CarePlan",
                columns: table => new
                {
                    CarePlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GoalDescription = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarePlanGoals", x => new { x.CarePlanId, x.Id });
                    table.ForeignKey(
                        name: "FK_CarePlanGoals_CarePlans_CarePlanId",
                        column: x => x.CarePlanId,
                        principalSchema: "CarePlan",
                        principalTable: "CarePlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CarePlanInterventions",
                schema: "CarePlan",
                columns: table => new
                {
                    CarePlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Implementation = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarePlanInterventions", x => new { x.CarePlanId, x.Id });
                    table.ForeignKey(
                        name: "FK_CarePlanInterventions_CarePlans_CarePlanId",
                        column: x => x.CarePlanId,
                        principalSchema: "CarePlan",
                        principalTable: "CarePlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CarePlanDiagnoses",
                schema: "CarePlan");

            migrationBuilder.DropTable(
                name: "CarePlanGoals",
                schema: "CarePlan");

            migrationBuilder.DropTable(
                name: "CarePlanInterventions",
                schema: "CarePlan");

            migrationBuilder.DropTable(
                name: "CarePlans",
                schema: "CarePlan");
        }
    }
}
