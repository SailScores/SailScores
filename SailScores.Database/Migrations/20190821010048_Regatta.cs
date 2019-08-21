using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SailScores.Database.Migrations
{
    public partial class Regatta : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Regattas",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    ClubId = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(maxLength: 200, nullable: false),
                    Description = table.Column<string>(nullable: true),
                    SeasonId = table.Column<Guid>(nullable: false),
                    StartDate = table.Column<DateTime>(nullable: true),
                    EndDate = table.Column<DateTime>(nullable: true),
                    UpdatedDateUtc = table.Column<DateTime>(nullable: true),
                    ScoringSystemId = table.Column<Guid>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Regattas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Regattas_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Regattas_ScoringSystems_ScoringSystemId",
                        column: x => x.ScoringSystemId,
                        principalTable: "ScoringSystems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Regattas_Seasons_SeasonId",
                        column: x => x.SeasonId,
                        principalTable: "Seasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RegattaFleet",
                columns: table => new
                {
                    RegattaId = table.Column<Guid>(nullable: false),
                    FleetId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegattaFleet", x => new { x.RegattaId, x.FleetId });
                    table.ForeignKey(
                        name: "FK_RegattaFleet_Fleets_FleetId",
                        column: x => x.FleetId,
                        principalTable: "Fleets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RegattaFleet_Regattas_RegattaId",
                        column: x => x.RegattaId,
                        principalTable: "Regattas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RegattaSeries",
                columns: table => new
                {
                    RegattaId = table.Column<Guid>(nullable: false),
                    SeriesId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegattaSeries", x => new { x.RegattaId, x.SeriesId });
                    table.ForeignKey(
                        name: "FK_RegattaSeries_Regattas_RegattaId",
                        column: x => x.RegattaId,
                        principalTable: "Regattas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RegattaSeries_Series_SeriesId",
                        column: x => x.SeriesId,
                        principalTable: "Series",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RegattaFleet_FleetId",
                table: "RegattaFleet",
                column: "FleetId");

            migrationBuilder.CreateIndex(
                name: "IX_Regattas_ClubId",
                table: "Regattas",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_Regattas_ScoringSystemId",
                table: "Regattas",
                column: "ScoringSystemId");

            migrationBuilder.CreateIndex(
                name: "IX_Regattas_SeasonId",
                table: "Regattas",
                column: "SeasonId");

            migrationBuilder.CreateIndex(
                name: "IX_RegattaSeries_SeriesId",
                table: "RegattaSeries",
                column: "SeriesId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RegattaFleet");

            migrationBuilder.DropTable(
                name: "RegattaSeries");

            migrationBuilder.DropTable(
                name: "Regattas");
        }
    }
}
