using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VCare.Modules.Patients.Migrations
{
    /// <inheritdoc />
    public partial class AllergiesAndAbsences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AllergyStatus",
                schema: "patients",
                table: "Patients",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                // Nobody has recorded an answer for existing residents yet.
                defaultValue: "Unknown");

            migrationBuilder.CreateTable(
                name: "PatientAbsences",
                schema: "patients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RecordedByStaffId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EndedByStaffId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientAbsences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientAbsences_Patients_PatientId",
                        column: x => x.PatientId,
                        principalSchema: "patients",
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PatientAllergies",
                schema: "patients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Substance = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Reaction = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Severity = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RecordedByStaffId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RemovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RemovedByStaffId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RemovalReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientAllergies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientAllergies_Patients_PatientId",
                        column: x => x.PatientId,
                        principalSchema: "patients",
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PatientAbsences_PatientId_StartedAt",
                schema: "patients",
                table: "PatientAbsences",
                columns: new[] { "PatientId", "StartedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PatientAllergies_PatientId",
                schema: "patients",
                table: "PatientAllergies",
                column: "PatientId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PatientAbsences",
                schema: "patients");

            migrationBuilder.DropTable(
                name: "PatientAllergies",
                schema: "patients");

            migrationBuilder.DropColumn(
                name: "AllergyStatus",
                schema: "patients",
                table: "Patients");
        }
    }
}
