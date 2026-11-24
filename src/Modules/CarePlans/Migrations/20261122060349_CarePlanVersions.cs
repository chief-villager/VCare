using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VCare.Modules.CarePlans.Migrations
{
    /// <inheritdoc />
    public partial class CarePlanVersions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Version",
                schema: "CarePlan",
                table: "CarePlans",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "CarePlanVersions",
                schema: "CarePlan",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CarePlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Number = table.Column<int>(type: "int", nullable: false),
                    Goal = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Diagnoses = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EditedByStaffId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EditedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CarePlanVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CarePlanVersions_CarePlans_CarePlanId",
                        column: x => x.CarePlanId,
                        principalSchema: "CarePlan",
                        principalTable: "CarePlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CarePlanVersions_CarePlanId_Number",
                schema: "CarePlan",
                table: "CarePlanVersions",
                columns: new[] { "CarePlanId", "Number" },
                unique: true);

            // Every plan already on file becomes version 1, with a copy of itself as
            // it stands, so the first edit after this has something to be compared with.
            migrationBuilder.Sql("""
                UPDATE [CarePlan].[CarePlans] SET [Version] = 1;
                INSERT INTO [CarePlan].[CarePlanVersions] ([Id], [CarePlanId], [Number], [Goal], [Diagnoses], [EditedByStaffId], [EditedAt])
                SELECT NEWID(), p.[Id], 1, p.[Goal],
                       COALESCE((SELECT d.[Name], d.[GoalDescription], d.[Intervention]
                                 FROM [CarePlan].[CarePlanDiagnoses] d
                                 WHERE d.[CarePlanId] = p.[Id]
                                 ORDER BY d.[Id]
                                 FOR JSON PATH, INCLUDE_NULL_VALUES), N'[]'),
                       p.[StaffId], CAST(p.[ModifiedDate] AS datetime2)
                FROM [CarePlan].[CarePlans] p;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CarePlanVersions",
                schema: "CarePlan");

            migrationBuilder.DropColumn(
                name: "Version",
                schema: "CarePlan",
                table: "CarePlans");
        }
    }
}
