using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SailScores.Database.Migrations
{
    public partial class ScoringTweaks : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ScoringSystemId",
                table: "Series",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Series_ScoringSystemId",
                table: "Series",
                column: "ScoringSystemId");

            migrationBuilder.AddForeignKey(
                name: "FK_Series_ScoringSystems_ScoringSystemId",
                table: "Series",
                column: "ScoringSystemId",
                principalTable: "ScoringSystems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Series_ScoringSystems_ScoringSystemId",
                table: "Series");

            migrationBuilder.DropIndex(
                name: "IX_Series_ScoringSystemId",
                table: "Series");

            migrationBuilder.DropColumn(
                name: "ScoringSystemId",
                table: "Series");
        }
    }
}
