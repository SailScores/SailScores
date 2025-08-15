using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SailScores.Database.Migrations
{
    /// <inheritdoc />
    public partial class SummarySeriesFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Type",
                table: "Series",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<DateOnly>(
                name: "EndDate",
                table: "Series",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RaceCount",
                table: "Series",
                type: "int",
                nullable: true,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateOnly>(
                name: "StartDate",
                table: "Series",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "Series");

            migrationBuilder.DropColumn(
                name: "RaceCount",
                table: "Series");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "Series");

            migrationBuilder.AlterColumn<int>(
                name: "Type",
                table: "Series",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
