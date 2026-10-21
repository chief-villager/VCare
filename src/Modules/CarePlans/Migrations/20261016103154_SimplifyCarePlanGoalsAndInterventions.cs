using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VCare.Modules.CarePlans.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyCarePlanGoalsAndInterventions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CarePlanGoals",
                schema: "CarePlan");

            migrationBuilder.DropTable(
                name: "CarePlanInterventions",
                schema: "CarePlan");

            migrationBuilder.RenameColumn(
                name: "Description",
                schema: "CarePlan",
                table: "CarePlanDiagnoses",
                newName: "Name");

            migrationBuilder.AddColumn<string>(
                name: "Goal",
                schema: "CarePlan",
                table: "CarePlans",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "GoalDescription",
                schema: "CarePlan",
                table: "CarePlanDiagnoses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Intervention",
                schema: "CarePlan",
                table: "CarePlanDiagnoses",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Goal",
                schema: "CarePlan",
                table: "CarePlans");

            migrationBuilder.DropColumn(
                name: "GoalDescription",
                schema: "CarePlan",
                table: "CarePlanDiagnoses");

            migrationBuilder.DropColumn(
                name: "Intervention",
                schema: "CarePlan",
                table: "CarePlanDiagnoses");

            migrationBuilder.RenameColumn(
                name: "Name",
                schema: "CarePlan",
                table: "CarePlanDiagnoses",
                newName: "Description");

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
        }
    }
}
