using Microsoft.EntityFrameworkCore.Migrations;

namespace SailScores.Database.Migrations
{
    public partial class ScoringParticPercent : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Competitors_Clubs_ClubId",
                table: "Competitors");

            migrationBuilder.AddColumn<decimal>(
                name: "ParticipationPercent",
                table: "ScoringSystems",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Competitors_Clubs_ClubId",
                table: "Competitors",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Competitors_Clubs_ClubId",
                table: "Competitors");

            migrationBuilder.DropColumn(
                name: "ParticipationPercent",
                table: "ScoringSystems");

            migrationBuilder.AddForeignKey(
                name: "FK_Competitors_Clubs_ClubId",
                table: "Competitors",
                column: "ClubId",
                principalTable: "Clubs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
