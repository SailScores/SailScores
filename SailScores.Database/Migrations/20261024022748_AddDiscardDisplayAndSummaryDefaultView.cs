using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SailScores.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddDiscardDisplayAndSummaryDefaultView : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DiscardColor",
                table: "SeriesResultsTemplates",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // Existing views keep discards faded and struck through.
            migrationBuilder.AddColumn<bool>(
                name: "DiscardFaded",
                table: "SeriesResultsTemplates",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "DiscardStrikethrough",
                table: "SeriesResultsTemplates",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DefaultSummarySeriesResultsTemplateId",
                table: "Clubs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Clubs_DefaultSummarySeriesResultsTemplateId",
                table: "Clubs",
                column: "DefaultSummarySeriesResultsTemplateId");

            migrationBuilder.AddForeignKey(
                name: "FK_Clubs_SeriesResultsTemplates_DefaultSummarySeriesResultsTemplateId",
                table: "Clubs",
                column: "DefaultSummarySeriesResultsTemplateId",
                principalTable: "SeriesResultsTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Clubs_SeriesResultsTemplates_DefaultSummarySeriesResultsTemplateId",
                table: "Clubs");

            migrationBuilder.DropIndex(
                name: "IX_Clubs_DefaultSummarySeriesResultsTemplateId",
                table: "Clubs");

            migrationBuilder.DropColumn(
                name: "DiscardColor",
                table: "SeriesResultsTemplates");

            migrationBuilder.DropColumn(
                name: "DiscardFaded",
                table: "SeriesResultsTemplates");

            migrationBuilder.DropColumn(
                name: "DiscardStrikethrough",
                table: "SeriesResultsTemplates");

            migrationBuilder.DropColumn(
                name: "DefaultSummarySeriesResultsTemplateId",
                table: "Clubs");
        }
    }
}
