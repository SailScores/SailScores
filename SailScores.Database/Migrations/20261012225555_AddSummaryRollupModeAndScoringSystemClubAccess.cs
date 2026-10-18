using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SailScores.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddSummaryRollupModeAndScoringSystemClubAccess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IncludeUnrankedChildScores",
                table: "Series",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SummaryRollupMode",
                table: "Series",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // ChildrenSeriesAsSingleRace = true becomes SummaryRollupMode.ChildRankAsRace (1).
            // ChildrenSeriesAsSingleRace stays so main can share this database; drop it later
            // in its own migration, after re-running this update for rows main changed.
            migrationBuilder.Sql(Deferred(
                "UPDATE [Series] SET [SummaryRollupMode] = 1 WHERE [ChildrenSeriesAsSingleRace] = 1"));

            migrationBuilder.AddColumn<bool>(
                name: "IsRestrictedToClubs",
                table: "ScoringSystems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ScoringSystemClubAccess",
                columns: table => new
                {
                    ScoringSystemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClubId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScoringSystemClubAccess", x => new { x.ScoringSystemId, x.ClubId });
                    table.ForeignKey(
                        name: "FK_ScoringSystemClubAccess_Clubs_ClubId",
                        column: x => x.ClubId,
                        principalTable: "Clubs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ScoringSystemClubAccess_ScoringSystems_ScoringSystemId",
                        column: x => x.ScoringSystemId,
                        principalTable: "ScoringSystems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ScoringSystemClubAccess_ClubId",
                table: "ScoringSystemClubAccess",
                column: "ClubId");
        }

        // Runs the statement through EXEC so SQL Server compiles it only when it runs. In an
        // idempotent script (dotnet ef migrations script --idempotent) it shares a batch with
        // the ALTER TABLE that adds the column it uses, and would otherwise fail to compile.
        private static string Deferred(string sql) => "EXEC(N'" + sql.Replace("'", "''") + "');";

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScoringSystemClubAccess");

            migrationBuilder.DropColumn(
                name: "IncludeUnrankedChildScores",
                table: "Series");

            migrationBuilder.DropColumn(
                name: "SummaryRollupMode",
                table: "Series");

            migrationBuilder.DropColumn(
                name: "IsRestrictedToClubs",
                table: "ScoringSystems");
        }
    }
}
