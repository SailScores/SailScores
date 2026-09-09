using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SailScores.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddScoreCodeGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ScoreCodeGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScoringSystemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    LimitationType = table.Column<int>(type: "int", nullable: false),
                    LimitationValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UseNonDiscardedRaces = table.Column<bool>(type: "bit", nullable: false),
                    OverageCodeName = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    OverageSelectionMethod = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScoreCodeGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScoreCodeGroups_ScoringSystems_ScoringSystemId",
                        column: x => x.ScoringSystemId,
                        principalTable: "ScoringSystems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScoreCodeGroupCodes",
                columns: table => new
                {
                    ScoreCodeGroupId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CodeName = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScoreCodeGroupCodes", x => new { x.ScoreCodeGroupId, x.CodeName });
                    table.ForeignKey(
                        name: "FK_ScoreCodeGroupCodes_ScoreCodeGroups_ScoreCodeGroupId",
                        column: x => x.ScoreCodeGroupId,
                        principalTable: "ScoreCodeGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScoreCodeGroups_ScoringSystemId",
                table: "ScoreCodeGroups",
                column: "ScoringSystemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScoreCodeGroupCodes");

            migrationBuilder.DropTable(
                name: "ScoreCodeGroups");
        }
    }
}
