using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SailScores.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddSeasonDefaultScoringSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DefaultScoringSystemId",
                table: "Seasons",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Seasons_DefaultScoringSystemId",
                table: "Seasons",
                column: "DefaultScoringSystemId");

            migrationBuilder.AddForeignKey(
                name: "FK_Seasons_ScoringSystems_DefaultScoringSystemId",
                table: "Seasons",
                column: "DefaultScoringSystemId",
                principalTable: "ScoringSystems",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Seasons_ScoringSystems_DefaultScoringSystemId",
                table: "Seasons");

            migrationBuilder.DropIndex(
                name: "IX_Seasons_DefaultScoringSystemId",
                table: "Seasons");

            migrationBuilder.DropColumn(
                name: "DefaultScoringSystemId",
                table: "Seasons");
        }
    }
}
