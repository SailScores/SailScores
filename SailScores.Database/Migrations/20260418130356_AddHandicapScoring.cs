using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SailScores.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddHandicapScoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "HandicapSystemId",
                table: "Series",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EnableHandicapScoring",
                table: "Clubs",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "CourseDistance",
                table: "Races",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DefaultHandicapSystemId",
                table: "Fleets",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DefaultHandicapSystemId",
                table: "Clubs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "HandicapSystems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClubId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SystemType = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HandicapSystems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CompetitorHandicaps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompetitorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HandicapSystemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompetitorHandicaps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompetitorHandicaps_Competitors_CompetitorId",
                        column: x => x.CompetitorId,
                        principalTable: "Competitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CompetitorHandicaps_HandicapSystems_HandicapSystemId",
                        column: x => x.HandicapSystemId,
                        principalTable: "HandicapSystems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "HandicapSystems",
                columns: new[] { "Id", "ClubId", "Description", "Name", "SystemType" },
                values: new object[,]
                {
                    { new Guid("a1b2c3d4-0001-0000-0000-000000000001"), null, "corrected = elapsed_sec - (rating × distance_nm). Rating is seconds-per-mile; scratch boat rating is 0.", "PHRF Time-on-Distance", 1 },
                    { new Guid("a1b2c3d4-0001-0000-0000-000000000002"), null, "corrected = elapsed_sec × 600 / (600 + rating). Uses the same PHRF rating as ToD but requires no course distance.", "PHRF Time-on-Time", 2 },
                    { new Guid("a1b2c3d4-0001-0000-0000-000000000003"), null, "corrected = elapsed_sec / PY × 1000. Ratings published by RYA; baseline is 1000.", "Portsmouth Yardstick", 3 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Series_FleetId",
                table: "Series",
                column: "FleetId");

            migrationBuilder.CreateIndex(
                name: "IX_Series_HandicapSystemId",
                table: "Series",
                column: "HandicapSystemId");

            migrationBuilder.CreateIndex(
                name: "IX_Fleets_DefaultHandicapSystemId",
                table: "Fleets",
                column: "DefaultHandicapSystemId");

            migrationBuilder.CreateIndex(
                name: "IX_Clubs_DefaultHandicapSystemId",
                table: "Clubs",
                column: "DefaultHandicapSystemId");

            migrationBuilder.CreateIndex(
                name: "IX_CompetitorHandicap_NullEnd",
                table: "CompetitorHandicaps",
                columns: new[] { "CompetitorId", "HandicapSystemId" },
                unique: true,
                filter: "[EffectiveTo] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CompetitorHandicap_NullStart",
                table: "CompetitorHandicaps",
                columns: new[] { "CompetitorId", "HandicapSystemId" },
                unique: true,
                filter: "[EffectiveFrom] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CompetitorHandicaps_HandicapSystemId",
                table: "CompetitorHandicaps",
                column: "HandicapSystemId");

            migrationBuilder.AddForeignKey(
                name: "FK_Clubs_HandicapSystems_DefaultHandicapSystemId",
                table: "Clubs",
                column: "DefaultHandicapSystemId",
                principalTable: "HandicapSystems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Fleets_HandicapSystems_DefaultHandicapSystemId",
                table: "Fleets",
                column: "DefaultHandicapSystemId",
                principalTable: "HandicapSystems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Series_Fleets_FleetId",
                table: "Series",
                column: "FleetId",
                principalTable: "Fleets",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Series_HandicapSystems_HandicapSystemId",
                table: "Series",
                column: "HandicapSystemId",
                principalTable: "HandicapSystems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Clubs_HandicapSystems_DefaultHandicapSystemId",
                table: "Clubs");

            migrationBuilder.DropForeignKey(
                name: "FK_Fleets_HandicapSystems_DefaultHandicapSystemId",
                table: "Fleets");

            migrationBuilder.DropForeignKey(
                name: "FK_Series_Fleets_FleetId",
                table: "Series");

            migrationBuilder.DropForeignKey(
                name: "FK_Series_HandicapSystems_HandicapSystemId",
                table: "Series");

            migrationBuilder.DropTable(
                name: "CompetitorHandicaps");

            migrationBuilder.DropTable(
                name: "HandicapSystems");

            migrationBuilder.DropIndex(
                name: "IX_Series_FleetId",
                table: "Series");

            migrationBuilder.DropIndex(
                name: "IX_Series_HandicapSystemId",
                table: "Series");

            migrationBuilder.DropIndex(
                name: "IX_Fleets_DefaultHandicapSystemId",
                table: "Fleets");

            migrationBuilder.DropIndex(
                name: "IX_Clubs_DefaultHandicapSystemId",
                table: "Clubs");

            migrationBuilder.DropColumn(
                name: "HandicapSystemId",
                table: "Series");

            migrationBuilder.DropColumn(
                name: "CourseDistance",
                table: "Races");

            migrationBuilder.DropColumn(
                name: "DefaultHandicapSystemId",
                table: "Fleets");

            migrationBuilder.DropColumn(
                name: "EnableHandicapScoring",
                table: "Clubs");

            migrationBuilder.DropColumn(
                name: "DefaultHandicapSystemId",
                table: "Clubs");
        }
    }
}
