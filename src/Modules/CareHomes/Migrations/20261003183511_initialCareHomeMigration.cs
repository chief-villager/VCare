using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VCare.Modules.CareHome.Migrations
{
    /// <inheritdoc />
    public partial class initialCareHomeMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "CareHomes");

            migrationBuilder.CreateTable(
                name: "CareHomes",
                schema: "CareHomes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CareHomes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CareHomes_Email",
                schema: "CareHomes",
                table: "CareHomes",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CareHomes",
                schema: "CareHomes");
        }
    }
}
