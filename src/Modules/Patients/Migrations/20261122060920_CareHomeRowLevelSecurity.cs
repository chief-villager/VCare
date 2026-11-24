using Microsoft.EntityFrameworkCore.Migrations;
using VCare.SharedKernel.Persistence;

#nullable disable

namespace VCare.Modules.Patients.Migrations
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
            migrationBuilder.Sql(RowLevelSecurity.EnsurePredicate);
            migrationBuilder.Sql(RowLevelSecurity.CreatePolicy("[patients].[CareHomeIsolation]", ("[patients].[Patients]", "CareHomeId")));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(RowLevelSecurity.DropPolicy("[patients].[CareHomeIsolation]"));
        }
    }
}
