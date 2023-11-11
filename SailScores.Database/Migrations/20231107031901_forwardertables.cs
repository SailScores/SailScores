using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SailScores.Database.Migrations
{
    public partial class forwardertables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CompetitorForwarders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OldClubInitials = table.Column<string>(type: "nvarchar(10)", nullable: true),
                    OldCompetitorUrl = table.Column<string>(type: "nvarchar(200)", nullable: true),
                    CompetitorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompetitorForwarders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompetitorForwarders_Competitors_CompetitorId",
                        column: x => x.CompetitorId,
                        principalTable: "Competitors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RegattaForwarders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OldClubInitials = table.Column<string>(type: "nvarchar(10)", nullable: true),
                    OldSeasonUrlName = table.Column<string>(type: "nvarchar(200)", nullable: true),
                    OldRegattaUrlName = table.Column<string>(type: "nvarchar(200)", nullable: true),
                    NewRegattaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegattaForwarders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegattaForwarders_Regatta_NewRegattaId",
                        column: x => x.NewRegattaId,
                        principalTable: "Regattas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SeriesForwarders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OldClubInitials = table.Column<string>(type: "nvarchar(10)", nullable: true),
                    OldSeasonUrlName = table.Column<string>(type: "nvarchar(200)", nullable: true),
                    OldSeriesUrlName = table.Column<string>(type: "nvarchar(200)", nullable: true),
                    NewSeriesId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Created = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "getdate()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeriesForwarders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SeriesForwarders_Series_NewSeriesId",
                        column: x => x.NewSeriesId,
                        principalTable: "Series",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompetitorForwarders_CompetitorId",
                table: "CompetitorForwarders",
                column: "CompetitorId");

            migrationBuilder.CreateIndex(
                name: "IX_RegattaForwarders_NewRegattaId",
                table: "RegattaForwarders",
                column: "NewRegattaId");

            migrationBuilder.CreateIndex(
                name: "IX_SeriesForwarders_NewSeriesId",
                table: "SeriesForwarders",
                column: "NewSeriesId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompetitorForwarders");

            migrationBuilder.DropTable(
                name: "RegattaForwarders");

            migrationBuilder.DropTable(
                name: "SeriesForwarders");
        }
    }
}
