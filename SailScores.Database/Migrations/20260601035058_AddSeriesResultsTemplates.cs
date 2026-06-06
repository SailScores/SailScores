using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SailScores.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddSeriesResultsTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SeriesResultsTemplateId",
                table: "Series",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DefaultRegattaSeriesResultsTemplateId",
                table: "Clubs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DefaultSeriesResultsTemplateId",
                table: "Clubs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SeriesResultsTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClubId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SailNumberVisibility = table.Column<int>(type: "int", nullable: false),
                    CompetitorNameVisibility = table.Column<int>(type: "int", nullable: false),
                    CompetitorNameHeader = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    BoatNameVisibility = table.Column<int>(type: "int", nullable: false),
                    BoatNameHeader = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CompetitorClubVisibility = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeriesResultsTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SeriesResultsTemplates_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Series_SeriesResultsTemplateId",
                table: "Series",
                column: "SeriesResultsTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_Clubs_DefaultRegattaSeriesResultsTemplateId",
                table: "Clubs",
                column: "DefaultRegattaSeriesResultsTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_Clubs_DefaultSeriesResultsTemplateId",
                table: "Clubs",
                column: "DefaultSeriesResultsTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_SeriesResultsTemplates_ClubId",
                table: "SeriesResultsTemplates",
                column: "ClubId");

            migrationBuilder.AddForeignKey(
                name: "FK_Clubs_SeriesResultsTemplates_DefaultRegattaSeriesResultsTemplateId",
                table: "Clubs",
                column: "DefaultRegattaSeriesResultsTemplateId",
                principalTable: "SeriesResultsTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Clubs_SeriesResultsTemplates_DefaultSeriesResultsTemplateId",
                table: "Clubs",
                column: "DefaultSeriesResultsTemplateId",
                principalTable: "SeriesResultsTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Series_SeriesResultsTemplates_SeriesResultsTemplateId",
                table: "Series",
                column: "SeriesResultsTemplateId",
                principalTable: "SeriesResultsTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Clubs_SeriesResultsTemplates_DefaultRegattaSeriesResultsTemplateId",
                table: "Clubs");

            migrationBuilder.DropForeignKey(
                name: "FK_Clubs_SeriesResultsTemplates_DefaultSeriesResultsTemplateId",
                table: "Clubs");

            migrationBuilder.DropForeignKey(
                name: "FK_Series_SeriesResultsTemplates_SeriesResultsTemplateId",
                table: "Series");

            migrationBuilder.DropTable(
                name: "SeriesResultsTemplates");

            migrationBuilder.DropIndex(
                name: "IX_Series_SeriesResultsTemplateId",
                table: "Series");

            migrationBuilder.DropIndex(
                name: "IX_Clubs_DefaultRegattaSeriesResultsTemplateId",
                table: "Clubs");

            migrationBuilder.DropIndex(
                name: "IX_Clubs_DefaultSeriesResultsTemplateId",
                table: "Clubs");

            migrationBuilder.DropColumn(
                name: "SeriesResultsTemplateId",
                table: "Series");

            migrationBuilder.DropColumn(
                name: "DefaultRegattaSeriesResultsTemplateId",
                table: "Clubs");

            migrationBuilder.DropColumn(
                name: "DefaultSeriesResultsTemplateId",
                table: "Clubs");
        }
    }
}
