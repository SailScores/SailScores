using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SailScores.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddSeriesResultsTemplateScoreCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SeriesResultsTemplateScoreCodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SeriesResultsTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Display = table.Column<int>(type: "int", nullable: false),
                    Faded = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeriesResultsTemplateScoreCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SeriesResultsTemplateScoreCodes_SeriesResultsTemplates_SeriesResultsTemplateId",
                        column: x => x.SeriesResultsTemplateId,
                        principalTable: "SeriesResultsTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SeriesResultsTemplateScoreCodes_SeriesResultsTemplateId_Code",
                table: "SeriesResultsTemplateScoreCodes",
                columns: new[] { "SeriesResultsTemplateId", "Code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SeriesResultsTemplateScoreCodes");
        }
    }
}
