using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SailScores.Database.Migrations
{
    public partial class ScoringSystem : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScoringSystems",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    ClubId = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(maxLength: 100, nullable: true),
                    DiscardPattern = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScoringSystems", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScoreCodes_ScoringSystemId",
                table: "ScoreCodes",
                column: "ScoringSystemId");

            migrationBuilder.AddForeignKey(
                name: "FK_ScoreCodes_ScoringSystems_ScoringSystemId",
                table: "ScoreCodes",
                column: "ScoringSystemId",
                principalTable: "ScoringSystems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScoreCodes_ScoringSystems_ScoringSystemId",
                table: "ScoreCodes");

            migrationBuilder.DropTable(
                name: "ScoringSystems");

            migrationBuilder.DropIndex(
                name: "IX_ScoreCodes_ScoringSystemId",
                table: "ScoreCodes");
        }
    }
}
