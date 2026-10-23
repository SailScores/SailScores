using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SailScores.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddSeriesResultsTemplateDisplayOptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Existing templates keep today's display: place colors on, race columns always shown.
            migrationBuilder.AddColumn<int>(
                name: "ColumnSeparators",
                table: "SeriesResultsTemplates",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "HighlightTopThree",
                table: "SeriesResultsTemplates",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "RaceColumnsVisibility",
                table: "SeriesResultsTemplates",
                type: "int",
                nullable: false,
                defaultValue: 1); // ColumnVisibility.Always
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ColumnSeparators",
                table: "SeriesResultsTemplates");

            migrationBuilder.DropColumn(
                name: "HighlightTopThree",
                table: "SeriesResultsTemplates");

            migrationBuilder.DropColumn(
                name: "RaceColumnsVisibility",
                table: "SeriesResultsTemplates");
        }
    }
}
