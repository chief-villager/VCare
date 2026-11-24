using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VCare.Modules.Patients.Migrations
{
    /// <inheritdoc />
    public partial class ResidentRecordDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "AdmittedOn",
                schema: "patients",
                table: "Patients",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DischargeDestination",
                schema: "patients",
                table: "Patients",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GpName",
                schema: "patients",
                table: "Patients",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GpPhone",
                schema: "patients",
                table: "Patients",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GpPractice",
                schema: "patients",
                table: "Patients",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LeftAt",
                schema: "patients",
                table: "Patients",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NhsNumber",
                schema: "patients",
                table: "Patients",
                type: "varchar(10)",
                unicode: false,
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Residency",
                schema: "patients",
                table: "Patients",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                // Every resident on file before this lives in the home.
                defaultValue: "Current");

            migrationBuilder.AddColumn<string>(
                name: "Room",
                schema: "patients",
                table: "Patients",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PatientAttorneys",
                schema: "patients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RegisteredOn = table.Column<DateOnly>(type: "date", nullable: true),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RecordedByStaffId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RemovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RemovedByStaffId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RemovalReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientAttorneys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientAttorneys_Patients_PatientId",
                        column: x => x.PatientId,
                        principalSchema: "patients",
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PatientWeights",
                schema: "patients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kilograms = table.Column<decimal>(type: "decimal(4,1)", precision: 4, scale: 1, nullable: false),
                    MeasuredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RecordedByStaffId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientWeights", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientWeights_Patients_PatientId",
                        column: x => x.PatientId,
                        principalSchema: "patients",
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Patients_CareHomeId_NhsNumber",
                schema: "patients",
                table: "Patients",
                columns: new[] { "CareHomeId", "NhsNumber" },
                unique: true,
                filter: "[NhsNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PatientAttorneys_PatientId",
                schema: "patients",
                table: "PatientAttorneys",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientWeights_PatientId_MeasuredAt",
                schema: "patients",
                table: "PatientWeights",
                columns: new[] { "PatientId", "MeasuredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PatientAttorneys",
                schema: "patients");

            migrationBuilder.DropTable(
                name: "PatientWeights",
                schema: "patients");

            migrationBuilder.DropIndex(
                name: "IX_Patients_CareHomeId_NhsNumber",
                schema: "patients",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "AdmittedOn",
                schema: "patients",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "DischargeDestination",
                schema: "patients",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "GpName",
                schema: "patients",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "GpPhone",
                schema: "patients",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "GpPractice",
                schema: "patients",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "LeftAt",
                schema: "patients",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "NhsNumber",
                schema: "patients",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "Residency",
                schema: "patients",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "Room",
                schema: "patients",
                table: "Patients");
        }
    }
}
