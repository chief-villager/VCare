using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VCare.Modules.Visitation.Migrations
{
    /// <inheritdoc />
    public partial class SplitVisitIntoVisitRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CheckedInAt",
                schema: "Visitation",
                table: "Visits");

            migrationBuilder.DropColumn(
                name: "CheckedOutAt",
                schema: "Visitation",
                table: "Visits");

            migrationBuilder.DropColumn(
                name: "FeedingTask_FeedingTaskNote",
                schema: "Visitation",
                table: "Visits");

            migrationBuilder.DropColumn(
                name: "FeedingTask_IsFeedingCompleted",
                schema: "Visitation",
                table: "Visits");

            migrationBuilder.DropColumn(
                name: "MedicationTask_IsMedicationCompleted",
                schema: "Visitation",
                table: "Visits");

            migrationBuilder.DropColumn(
                name: "MedicationTask_MedicationTaskNote",
                schema: "Visitation",
                table: "Visits");

            migrationBuilder.DropColumn(
                name: "PersonalcareTask_CareTaskNote",
                schema: "Visitation",
                table: "Visits");

            migrationBuilder.DropColumn(
                name: "PersonalcareTask_IsCareCompleted",
                schema: "Visitation",
                table: "Visits");

            migrationBuilder.DropColumn(
                name: "StaffId",
                schema: "Visitation",
                table: "Visits");

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "Visitation",
                table: "Visits");

            migrationBuilder.DropColumn(
                name: "VisitationSummary",
                schema: "Visitation",
                table: "Visits");

            migrationBuilder.AddColumn<string>(
                name: "StaffIds",
                schema: "Visitation",
                table: "Visits",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<DateOnly>(
                name: "VisitDay",
                schema: "Visitation",
                table: "Visits",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<TimeOnly>(
                name: "VisitTime",
                schema: "Visitation",
                table: "Visits",
                type: "time",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0));

            migrationBuilder.CreateTable(
                name: "VisitRecords",
                schema: "Visitation",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VisitId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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
                    VisitationSummary = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisitRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VisitRecords_Visits_VisitId",
                        column: x => x.VisitId,
                        principalSchema: "Visitation",
                        principalTable: "Visits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VisitRecords_StaffId",
                schema: "Visitation",
                table: "VisitRecords",
                column: "StaffId");

            migrationBuilder.CreateIndex(
                name: "IX_VisitRecords_VisitId",
                schema: "Visitation",
                table: "VisitRecords",
                column: "VisitId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VisitRecords",
                schema: "Visitation");

            migrationBuilder.DropColumn(
                name: "StaffIds",
                schema: "Visitation",
                table: "Visits");

            migrationBuilder.DropColumn(
                name: "VisitDay",
                schema: "Visitation",
                table: "Visits");

            migrationBuilder.DropColumn(
                name: "VisitTime",
                schema: "Visitation",
                table: "Visits");

            migrationBuilder.AddColumn<DateTime>(
                name: "CheckedInAt",
                schema: "Visitation",
                table: "Visits",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "CheckedOutAt",
                schema: "Visitation",
                table: "Visits",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FeedingTask_FeedingTaskNote",
                schema: "Visitation",
                table: "Visits",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "FeedingTask_IsFeedingCompleted",
                schema: "Visitation",
                table: "Visits",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "MedicationTask_IsMedicationCompleted",
                schema: "Visitation",
                table: "Visits",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MedicationTask_MedicationTaskNote",
                schema: "Visitation",
                table: "Visits",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PersonalcareTask_CareTaskNote",
                schema: "Visitation",
                table: "Visits",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PersonalcareTask_IsCareCompleted",
                schema: "Visitation",
                table: "Visits",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "StaffId",
                schema: "Visitation",
                table: "Visits",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "Status",
                schema: "Visitation",
                table: "Visits",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "VisitationSummary",
                schema: "Visitation",
                table: "Visits",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
