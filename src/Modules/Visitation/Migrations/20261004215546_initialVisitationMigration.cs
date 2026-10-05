using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VCare.Modules.Visitation.Migrations
{
    /// <inheritdoc />
    public partial class initialVisitationMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Visitation");

            migrationBuilder.CreateTable(
                name: "Visits",
                schema: "Visitation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CareHomeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StaffId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FeedingTask_IsFeedingCompleted = table.Column<bool>(type: "bit", nullable: true),
                    FeedingTask_FeedingTaskNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MedicationTask_IsMedicationCompleted = table.Column<bool>(type: "bit", nullable: true),
                    MedicationTask_MedicationTaskNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PersonalcareTask_IsCareCompleted = table.Column<bool>(type: "bit", nullable: true),
                    PersonalcareTask_CareTaskNote = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CheckedInAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CheckedOutAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancellationReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    VisitationSummary = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Visits", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Visits_CareHomeId",
                schema: "Visitation",
                table: "Visits",
                column: "CareHomeId");

            migrationBuilder.CreateIndex(
                name: "IX_Visits_PatientId",
                schema: "Visitation",
                table: "Visits",
                column: "PatientId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Visits",
                schema: "Visitation");
        }
    }
}
