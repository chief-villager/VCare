using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VCare.Modules.Staffs.Migrations
{
    /// <inheritdoc />
    public partial class AddStaffDocumentsAndProfilePicture : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                schema: "Auth",
                table: "Staffs",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETDATE()");

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedAt",
                schema: "Auth",
                table: "Staffs",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETDATE()");

            migrationBuilder.AddColumn<string>(
                name: "ProfilePictureUrl",
                schema: "Auth",
                table: "Staffs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StaffDocuments",
                schema: "Auth",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StaffId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StorageKeyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StaffDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StaffDocuments_Staffs_StaffId",
                        column: x => x.StaffId,
                        principalSchema: "Auth",
                        principalTable: "Staffs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StaffDocuments_StaffId",
                schema: "Auth",
                table: "StaffDocuments",
                column: "StaffId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StaffDocuments",
                schema: "Auth");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                schema: "Auth",
                table: "Staffs");

            migrationBuilder.DropColumn(
                name: "ModifiedAt",
                schema: "Auth",
                table: "Staffs");

            migrationBuilder.DropColumn(
                name: "ProfilePictureUrl",
                schema: "Auth",
                table: "Staffs");
        }
    }
}
