using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace VCare.Modules.Medications.Migrations
{
    /// <inheritdoc />
    public partial class OrderHistoryWitnessAndOutcomes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AllergyOverrideReason",
                schema: "Medication",
                table: "MedicationOrder",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedByStaffId",
                schema: "Medication",
                table: "MedicationOrder",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WitnessedByStaffId",
                schema: "Medication",
                table: "MedicationAdminstration",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OrderStatusChange",
                schema: "Medication",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MedicationOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    From = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    To = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ChangedByStaffId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderStatusChange", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderStatusChange_MedicationOrder_MedicationOrderId",
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
                    { new Guid("0b6f5e7a-91c2-4d3e-8f10-2a3b4c5d6e01"), "W", "Withheld" },
                    { new Guid("0b6f5e7a-91c2-4d3e-8f10-2a3b4c5d6e02"), "S", "Asleep" },
                    { new Guid("0b6f5e7a-91c2-4d3e-8f10-2a3b4c5d6e03"), "H", "In hospital" },
                    { new Guid("0b6f5e7a-91c2-4d3e-8f10-2a3b4c5d6e04"), "L", "On leave" },
                    { new Guid("0b6f5e7a-91c2-4d3e-8f10-2a3b4c5d6e05"), "O", "Out of stock" },
                    { new Guid("0b6f5e7a-91c2-4d3e-8f10-2a3b4c5d6e06"), "V", "Vomited" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderStatusChange_MedicationOrderId",
                schema: "Medication",
                table: "OrderStatusChange",
                column: "MedicationOrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderStatusChange",
                schema: "Medication");

            migrationBuilder.DeleteData(
                schema: "Medication",
                table: "OutcomeCode",
                keyColumn: "Id",
                keyValue: new Guid("0b6f5e7a-91c2-4d3e-8f10-2a3b4c5d6e01"));

            migrationBuilder.DeleteData(
                schema: "Medication",
                table: "OutcomeCode",
                keyColumn: "Id",
                keyValue: new Guid("0b6f5e7a-91c2-4d3e-8f10-2a3b4c5d6e02"));

            migrationBuilder.DeleteData(
                schema: "Medication",
                table: "OutcomeCode",
                keyColumn: "Id",
                keyValue: new Guid("0b6f5e7a-91c2-4d3e-8f10-2a3b4c5d6e03"));

            migrationBuilder.DeleteData(
                schema: "Medication",
                table: "OutcomeCode",
                keyColumn: "Id",
                keyValue: new Guid("0b6f5e7a-91c2-4d3e-8f10-2a3b4c5d6e04"));

            migrationBuilder.DeleteData(
                schema: "Medication",
                table: "OutcomeCode",
                keyColumn: "Id",
                keyValue: new Guid("0b6f5e7a-91c2-4d3e-8f10-2a3b4c5d6e05"));

            migrationBuilder.DeleteData(
                schema: "Medication",
                table: "OutcomeCode",
                keyColumn: "Id",
                keyValue: new Guid("0b6f5e7a-91c2-4d3e-8f10-2a3b4c5d6e06"));

            migrationBuilder.DropColumn(
                name: "AllergyOverrideReason",
                schema: "Medication",
                table: "MedicationOrder");

            migrationBuilder.DropColumn(
                name: "CreatedByStaffId",
                schema: "Medication",
                table: "MedicationOrder");

            migrationBuilder.DropColumn(
                name: "WitnessedByStaffId",
                schema: "Medication",
                table: "MedicationAdminstration");
        }
    }
}
