using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Vcare.Modules.Medications.Migrations
{
    /// <inheritdoc />
    public partial class initialMedicationMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Medication");

            migrationBuilder.CreateTable(
                name: "MedicationOrder",
                schema: "Medication",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CareHomeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Medication = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Route = table.Column<int>(type: "int", nullable: false),
                    Instructions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Prescriber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsPrn = table.Column<bool>(type: "bit", nullable: false),
                    PrnIndication = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PrnMinIntervalMinutes = table.Column<int>(type: "int", nullable: true),
                    PrnMaxDose24h = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsControlledDrug = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicationOrder", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutcomeCode",
                schema: "Medication",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DisplayLetter = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutcomeCode", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DoseSchedule",
                schema: "Medication",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MedicationOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Dose = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FType = table.Column<int>(type: "int", nullable: false),
                    Times = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IntervalDays = table.Column<int>(type: "int", nullable: false),
                    DaysOfWeek = table.Column<int>(type: "int", nullable: false),
                    AnchorDate = table.Column<DateOnly>(type: "date", nullable: true),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    Sequence = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoseSchedule", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DoseSchedule_MedicationOrder_MedicationOrderId",
                        column: x => x.MedicationOrderId,
                        principalSchema: "Medication",
                        principalTable: "MedicationOrder",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "MedicationAdminstration",
                schema: "Medication",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CareHomeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MedicationOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScheduledFor = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AdministeredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OutcomeCodeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AdministeredByStaffId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicationAdminstration", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedicationAdminstration_MedicationOrder_MedicationOrderId",
                        column: x => x.MedicationOrderId,
                        principalSchema: "Medication",
                        principalTable: "MedicationOrder",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                schema: "Medication",
                table: "OutcomeCode",
                columns: new[] { "Id", "DisplayLetter", "Name" },
                values: new object[,]
                {
                    { new Guid("e2b8c0d4-5f67-4890-b234-c567d890e123"), "T", "Taken" },
                    { new Guid("f3c9d1e5-6078-4901-c345-d678e901f234"), "R", "Refused" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_DoseSchedule_MedicationOrderId",
                schema: "Medication",
                table: "DoseSchedule",
                column: "MedicationOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicationAdminstration_CareHomeId",
                schema: "Medication",
                table: "MedicationAdminstration",
                column: "CareHomeId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicationAdminstration_MedicationOrderId",
                schema: "Medication",
                table: "MedicationAdminstration",
                column: "MedicationOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_MedicationOrder_CareHomeId",
                schema: "Medication",
                table: "MedicationOrder",
                column: "CareHomeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DoseSchedule",
                schema: "Medication");

            migrationBuilder.DropTable(
                name: "MedicationAdminstration",
                schema: "Medication");

            migrationBuilder.DropTable(
                name: "OutcomeCode",
                schema: "Medication");

            migrationBuilder.DropTable(
                name: "MedicationOrder",
                schema: "Medication");
        }
    }
}
