using Microsoft.EntityFrameworkCore.Migrations;
using VCare.SharedKernel.Persistence;

#nullable disable

namespace VCare.Modules.Staffs.Migrations
{
    /// <inheritdoc />
    public partial class CareHomeRowLevelSecurity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // A second wall between care homes, under the query filters: SQL Server
            // itself hides other homes' rows and refuses writes into them, using the
            // care home EF sets on each connection (CareHomeSessionContext).
            // Not AspNetUsers: signing in, and Identity's check that a username or email
            // is free, must see every home's accounts.
            migrationBuilder.Sql(RowLevelSecurity.EnsurePredicate);
            migrationBuilder.Sql(RowLevelSecurity.CreatePolicy("[Auth].[CareHomeIsolation]", ("[Auth].[Staffs]", "CareHomeId")));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(RowLevelSecurity.DropPolicy("[Auth].[CareHomeIsolation]"));
        }
    }
}
