using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SailScores.Database.Migrations
{
    public partial class ScoreCodeChangesForReal : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UseAverageResult",
                table: "ScoreCodes",
                newName: "Started");

            migrationBuilder.RenameColumn(
                name: "Text",
                table: "ScoreCodes",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "CountAsCompetitor",
                table: "ScoreCodes",
                newName: "PreserveResult");

            migrationBuilder.RenameColumn(
                name: "CompetitorCountPlus",
                table: "ScoreCodes",
                newName: "FormulaValue");

            migrationBuilder.AddColumn<bool>(
                name: "AdjustOtherScores",
                table: "ScoreCodes",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CameToStart",
                table: "ScoreCodes",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Finished",
                table: "ScoreCodes",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Formula",
                table: "ScoreCodes",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScoreLike",
                table: "ScoreCodes",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ScoringSystemId",
                table: "ScoreCodes",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdjustOtherScores",
                table: "ScoreCodes");

            migrationBuilder.DropColumn(
                name: "CameToStart",
                table: "ScoreCodes");

            migrationBuilder.DropColumn(
                name: "Finished",
                table: "ScoreCodes");

            migrationBuilder.DropColumn(
                name: "Formula",
                table: "ScoreCodes");

            migrationBuilder.DropColumn(
                name: "ScoreLike",
                table: "ScoreCodes");

            migrationBuilder.DropColumn(
                name: "ScoringSystemId",
                table: "ScoreCodes");

            migrationBuilder.RenameColumn(
                name: "Started",
                table: "ScoreCodes",
                newName: "UseAverageResult");

            migrationBuilder.RenameColumn(
                name: "PreserveResult",
                table: "ScoreCodes",
                newName: "CountAsCompetitor");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "ScoreCodes",
                newName: "Text");

            migrationBuilder.RenameColumn(
                name: "FormulaValue",
                table: "ScoreCodes",
                newName: "CompetitorCountPlus");
        }
    }
}
