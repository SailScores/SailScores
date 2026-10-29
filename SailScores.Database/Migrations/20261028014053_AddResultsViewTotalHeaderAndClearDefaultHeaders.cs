using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SailScores.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddResultsViewTotalHeaderAndClearDefaultHeaders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TotalHeader",
                table: "SeriesResultsTemplates",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            // An empty header now means the translated default. The seeded "Helm" and "Boat"
            // become empty, so they show in the club's language ("Helm" shows as "Sailor(s)" in
            // English). The previous release also reads empty as "Helm" and "Boat".
            migrationBuilder.Sql(
                "UPDATE SeriesResultsTemplates SET CompetitorNameHeader = NULL WHERE CompetitorNameHeader = 'Helm'");
            migrationBuilder.Sql(
                "UPDATE SeriesResultsTemplates SET BoatNameHeader = NULL WHERE BoatNameHeader = 'Boat'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TotalHeader",
                table: "SeriesResultsTemplates");
        }
    }
}
