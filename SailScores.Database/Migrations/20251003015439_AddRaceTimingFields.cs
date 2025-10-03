using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SailScores.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddRaceTimingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeSpan>(
                name: "ElapsedTime",
                table: "Scores",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FinishTime",
                table: "Scores",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartTime",
                table: "Races",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TrackTimes",
                table: "Races",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ElapsedTime",
                table: "Scores");

            migrationBuilder.DropColumn(
                name: "FinishTime",
                table: "Scores");

            migrationBuilder.DropColumn(
                name: "StartTime",
                table: "Races");

            migrationBuilder.DropColumn(
                name: "TrackTimes",
                table: "Races");
        }
    }
}
