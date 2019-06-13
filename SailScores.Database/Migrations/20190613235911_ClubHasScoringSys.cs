using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SailScores.Database.Migrations
{
    public partial class ClubHasScoringSys : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScoreCodes_Clubs_ClubId",
                table: "ScoreCodes");

            migrationBuilder.DropIndex(
                name: "IX_ScoreCodes_ClubId",
                table: "ScoreCodes");

            migrationBuilder.DropColumn(
                name: "ClubId",
                table: "ScoreCodes");

            migrationBuilder.AddColumn<Guid>(
                name: "DefaultScoringSystemId",
                table: "Clubs",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScoringSystems_ClubId",
                table: "ScoringSystems",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_Clubs_DefaultScoringSystemId",
                table: "Clubs",
                column: "DefaultScoringSystemId");

            migrationBuilder.AddForeignKey(
                name: "FK_Clubs_ScoringSystems_DefaultScoringSystemId",
                table: "Clubs",
                column: "DefaultScoringSystemId",
                principalTable: "ScoringSystems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ScoringSystems_Clubs_ClubId",
                table: "ScoringSystems",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Clubs_ScoringSystems_DefaultScoringSystemId",
                table: "Clubs");

            migrationBuilder.DropForeignKey(
                name: "FK_ScoringSystems_Clubs_ClubId",
                table: "ScoringSystems");

            migrationBuilder.DropIndex(
                name: "IX_ScoringSystems_ClubId",
                table: "ScoringSystems");

            migrationBuilder.DropIndex(
                name: "IX_Clubs_DefaultScoringSystemId",
                table: "Clubs");

            migrationBuilder.DropColumn(
                name: "DefaultScoringSystemId",
                table: "Clubs");

            migrationBuilder.AddColumn<Guid>(
                name: "ClubId",
                table: "ScoreCodes",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ScoreCodes_ClubId",
                table: "ScoreCodes",
                column: "ClubId");

            migrationBuilder.AddForeignKey(
                name: "FK_ScoreCodes_Clubs_ClubId",
                table: "ScoreCodes",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
