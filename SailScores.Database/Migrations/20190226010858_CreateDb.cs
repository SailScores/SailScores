using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace SailScores.Database.Migrations
{
    public partial class CreateDb : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Clubs",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(maxLength: 200, nullable: false),
                    Initials = table.Column<string>(maxLength: 10, nullable: true),
                    Description = table.Column<string>(nullable: true),
                    IsHidden = table.Column<bool>(nullable: false),
                    Url = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clubs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Files",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    FileContents = table.Column<byte[]>(nullable: true),
                    Created = table.Column<DateTime>(nullable: false),
                    ImportedTime = table.Column<DateTime>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Files", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserPermissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    UserEmail = table.Column<string>(maxLength: 254, nullable: true),
                    ClubId = table.Column<Guid>(nullable: true),
                    CanEditAllClubs = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPermissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BoatClasses",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    ClubId = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(maxLength: 200, nullable: true),
                    Description = table.Column<string>(maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoatClasses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BoatClasses_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Fleets",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    ClubId = table.Column<Guid>(nullable: false),
                    ShortName = table.Column<string>(maxLength: 30, nullable: true),
                    Name = table.Column<string>(maxLength: 200, nullable: true),
                    Description = table.Column<string>(maxLength: 2000, nullable: true),
                    IsHidden = table.Column<bool>(nullable: false),
                    FleetType = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fleets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Fleets_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScoreCodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    ClubId = table.Column<Guid>(nullable: false),
                    ScoringSystemId = table.Column<Guid>(nullable: true),
                    Name = table.Column<string>(maxLength: 20, nullable: true),
                    Description = table.Column<string>(maxLength: 1000, nullable: true),
                    Formula = table.Column<string>(maxLength: 100, nullable: true),
                    FormulaValue = table.Column<int>(nullable: true),
                    ScoreLike = table.Column<string>(nullable: true),
                    Discardable = table.Column<bool>(nullable: true),
                    CameToStart = table.Column<bool>(nullable: true),
                    Started = table.Column<bool>(nullable: true),
                    Finished = table.Column<bool>(nullable: true),
                    PreserveResult = table.Column<bool>(nullable: true),
                    AdjustOtherScores = table.Column<bool>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScoreCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScoreCodes_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Seasons",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    ClubId = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(maxLength: 200, nullable: true),
                    Start = table.Column<DateTime>(nullable: false),
                    End = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Seasons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Seasons_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Competitors",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    ClubId = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(maxLength: 200, nullable: true),
                    SailNumber = table.Column<string>(maxLength: 20, nullable: true),
                    AlternativeSailNumber = table.Column<string>(maxLength: 20, nullable: true),
                    BoatName = table.Column<string>(maxLength: 200, nullable: true),
                    Notes = table.Column<string>(maxLength: 2000, nullable: true),
                    BoatClassId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Competitors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Competitors_BoatClasses_BoatClassId",
                        column: x => x.BoatClassId,
                        principalTable: "BoatClasses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Competitors_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FleetBoatClass",
                columns: table => new
                {
                    FleetId = table.Column<Guid>(nullable: false),
                    BoatClassId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FleetBoatClass", x => new { x.FleetId, x.BoatClassId });
                    table.ForeignKey(
                        name: "FK_FleetBoatClass_BoatClasses_BoatClassId",
                        column: x => x.BoatClassId,
                        principalTable: "BoatClasses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FleetBoatClass_Fleets_FleetId",
                        column: x => x.FleetId,
                        principalTable: "Fleets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Races",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    ClubId = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(maxLength: 200, nullable: true),
                    Date = table.Column<DateTime>(nullable: true),
                    Order = table.Column<int>(nullable: false),
                    Description = table.Column<string>(maxLength: 1000, nullable: true),
                    FleetId = table.Column<Guid>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Races", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Races_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Races_Fleets_FleetId",
                        column: x => x.FleetId,
                        principalTable: "Fleets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Series",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    ClubId = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(maxLength: 200, nullable: false),
                    Description = table.Column<string>(maxLength: 2000, nullable: true),
                    SeasonId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Series", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Series_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Series_Seasons_SeasonId",
                        column: x => x.SeasonId,
                        principalTable: "Seasons",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CompetitorFleet",
                columns: table => new
                {
                    CompetitorId = table.Column<Guid>(nullable: false),
                    FleetId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompetitorFleet", x => new { x.CompetitorId, x.FleetId });
                    table.ForeignKey(
                        name: "FK_CompetitorFleet_Competitors_CompetitorId",
                        column: x => x.CompetitorId,
                        principalTable: "Competitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CompetitorFleet_Fleets_FleetId",
                        column: x => x.FleetId,
                        principalTable: "Fleets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Scores",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    CompetitorId = table.Column<Guid>(nullable: false),
                    RaceId = table.Column<Guid>(nullable: false),
                    Place = table.Column<int>(nullable: true),
                    Code = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Scores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Scores_Competitors_CompetitorId",
                        column: x => x.CompetitorId,
                        principalTable: "Competitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Scores_Races_RaceId",
                        column: x => x.RaceId,
                        principalTable: "Races",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SeriesRace",
                columns: table => new
                {
                    RaceId = table.Column<Guid>(nullable: false),
                    SeriesId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeriesRace", x => new { x.SeriesId, x.RaceId });
                    table.ForeignKey(
                        name: "FK_SeriesRace_Races_RaceId",
                        column: x => x.RaceId,
                        principalTable: "Races",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SeriesRace_Series_SeriesId",
                        column: x => x.SeriesId,
                        principalTable: "Series",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BoatClasses_ClubId",
                table: "BoatClasses",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_CompetitorFleet_FleetId",
                table: "CompetitorFleet",
                column: "FleetId");

            migrationBuilder.CreateIndex(
                name: "IX_Competitors_BoatClassId",
                table: "Competitors",
                column: "BoatClassId");

            migrationBuilder.CreateIndex(
                name: "IX_Competitors_ClubId",
                table: "Competitors",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_FleetBoatClass_BoatClassId",
                table: "FleetBoatClass",
                column: "BoatClassId");

            migrationBuilder.CreateIndex(
                name: "IX_Fleets_ClubId",
                table: "Fleets",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_Races_ClubId",
                table: "Races",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_Races_FleetId",
                table: "Races",
                column: "FleetId");

            migrationBuilder.CreateIndex(
                name: "IX_ScoreCodes_ClubId",
                table: "ScoreCodes",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_Scores_CompetitorId",
                table: "Scores",
                column: "CompetitorId");

            migrationBuilder.CreateIndex(
                name: "IX_Scores_RaceId",
                table: "Scores",
                column: "RaceId");

            migrationBuilder.CreateIndex(
                name: "IX_Seasons_ClubId",
                table: "Seasons",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_Series_ClubId",
                table: "Series",
                column: "ClubId");

            migrationBuilder.CreateIndex(
                name: "IX_Series_SeasonId",
                table: "Series",
                column: "SeasonId");

            migrationBuilder.CreateIndex(
                name: "IX_SeriesRace_RaceId",
                table: "SeriesRace",
                column: "RaceId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompetitorFleet");

            migrationBuilder.DropTable(
                name: "Files");

            migrationBuilder.DropTable(
                name: "FleetBoatClass");

            migrationBuilder.DropTable(
                name: "ScoreCodes");

            migrationBuilder.DropTable(
                name: "Scores");

            migrationBuilder.DropTable(
                name: "SeriesRace");

            migrationBuilder.DropTable(
                name: "UserPermissions");

            migrationBuilder.DropTable(
                name: "Competitors");

            migrationBuilder.DropTable(
                name: "Races");

            migrationBuilder.DropTable(
                name: "Series");

            migrationBuilder.DropTable(
                name: "BoatClasses");

            migrationBuilder.DropTable(
                name: "Fleets");

            migrationBuilder.DropTable(
                name: "Seasons");

            migrationBuilder.DropTable(
                name: "Clubs");
        }
    }
}
