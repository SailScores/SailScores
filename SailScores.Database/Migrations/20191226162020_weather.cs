using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SailScores.Database.Migrations
{
    public partial class weather : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "WeatherId",
                table: "Races",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "WeatherSettingsId",
                table: "Clubs",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Weather",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Description = table.Column<string>(maxLength: 1000, nullable: true),
                    Icon = table.Column<string>(maxLength: 32, nullable: true),
                    TemperatureString = table.Column<string>(maxLength: 32, nullable: true),
                    TemperatureDegreesKelvin = table.Column<decimal>(type: "decimal(18, 2)", nullable: true),
                    WindSpeedString = table.Column<string>(maxLength: 32, nullable: true),
                    WindSpeedMeterPerSecond = table.Column<decimal>(type: "decimal(18, 2)", nullable: true),
                    WindDirectionString = table.Column<string>(maxLength: 32, nullable: true),
                    WindDirectionDegrees = table.Column<decimal>(type: "decimal(18, 0)", nullable: true),
                    WindGustString = table.Column<string>(maxLength: 32, nullable: true),
                    WindGustMeterPerSecond = table.Column<decimal>(type: "decimal(18, 2)", nullable: true),
                    Humidity = table.Column<decimal>(type: "decimal(18, 2)", nullable: true),
                    CloudCoverPercent = table.Column<decimal>(type: "decimal(18, 0)", nullable: true),
                    CreatedDateUtc = table.Column<DateTime>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Weather", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WeatherSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Latitude = table.Column<decimal>(type: "decimal(18, 4)", nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(18, 4)", nullable: true),
                    TemperatureUnits = table.Column<string>(nullable: true),
                    WindSpeedUnits = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeatherSettings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Races_WeatherId",
                table: "Races",
                column: "WeatherId");

            migrationBuilder.CreateIndex(
                name: "IX_Clubs_WeatherSettingsId",
                table: "Clubs",
                column: "WeatherSettingsId");

            migrationBuilder.AddForeignKey(
                name: "FK_Clubs_WeatherSettings_WeatherSettingsId",
                table: "Clubs",
                column: "WeatherSettingsId",
                principalTable: "WeatherSettings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Races_Weather_WeatherId",
                table: "Races",
                column: "WeatherId",
                principalTable: "Weather",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Clubs_WeatherSettings_WeatherSettingsId",
                table: "Clubs");

            migrationBuilder.DropForeignKey(
                name: "FK_Races_Weather_WeatherId",
                table: "Races");

            migrationBuilder.DropTable(
                name: "Weather");

            migrationBuilder.DropTable(
                name: "WeatherSettings");

            migrationBuilder.DropIndex(
                name: "IX_Races_WeatherId",
                table: "Races");

            migrationBuilder.DropIndex(
                name: "IX_Clubs_WeatherSettingsId",
                table: "Clubs");

            migrationBuilder.DropColumn(
                name: "WeatherId",
                table: "Races");

            migrationBuilder.DropColumn(
                name: "WeatherSettingsId",
                table: "Clubs");
        }
    }
}
