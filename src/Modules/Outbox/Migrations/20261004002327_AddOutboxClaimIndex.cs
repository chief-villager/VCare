using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VCare.Modules.Outbox.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxClaimIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_Claim",
                schema: "Outbox",
                table: "OutboxMessages",
                columns: new[] { "Status", "NextAttemptAt", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OutboxMessages_Claim",
                schema: "Outbox",
                table: "OutboxMessages");
        }
    }
}
