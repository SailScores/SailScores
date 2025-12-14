using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SailScores.Database.Migrations
{
    /// <inheritdoc />
    public partial class seriesDates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DateRestricted",
                table: "Series",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "EnforcedEndDate",
                table: "Series",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "EnforcedStartDate",
                table: "Series",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateRestricted",
                table: "Series");

            migrationBuilder.DropColumn(
                name: "EnforcedEndDate",
                table: "Series");

            migrationBuilder.DropColumn(
                name: "EnforcedStartDate",
                table: "Series");
        }
    }
}
