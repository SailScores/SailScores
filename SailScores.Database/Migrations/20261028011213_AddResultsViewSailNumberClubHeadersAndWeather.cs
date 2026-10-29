using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SailScores.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddResultsViewSailNumberClubHeadersAndWeather : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Existing views keep today's display: no alternative sail number column, weather shown,
            // and empty headers use the default ones.
            migrationBuilder.AddColumn<string>(
                name: "AlternativeSailNumberHeader",
                table: "SeriesResultsTemplates",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AlternativeSailNumberVisibility",
                table: "SeriesResultsTemplates",
                type: "int",
                nullable: false,
                defaultValue: 10); // ColumnVisibility.Hidden

            migrationBuilder.AddColumn<string>(
                name: "CompetitorClubHeader",
                table: "SeriesResultsTemplates",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SailNumberHeader",
                table: "SeriesResultsTemplates",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShowWeather",
                table: "SeriesResultsTemplates",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AlternativeSailNumberHeader",
                table: "SeriesResultsTemplates");

            migrationBuilder.DropColumn(
                name: "AlternativeSailNumberVisibility",
                table: "SeriesResultsTemplates");

            migrationBuilder.DropColumn(
                name: "CompetitorClubHeader",
                table: "SeriesResultsTemplates");

            migrationBuilder.DropColumn(
                name: "SailNumberHeader",
                table: "SeriesResultsTemplates");

            migrationBuilder.DropColumn(
                name: "ShowWeather",
                table: "SeriesResultsTemplates");
        }
    }
}
