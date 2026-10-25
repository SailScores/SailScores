using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SailScores.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddSharedScoringColumnsAndTotalsSeparatorToViews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 10 is ColumnVisibility.Hidden.
            migrationBuilder.AddColumn<int>(
                name: "ParticipationVisibility",
                table: "SeriesResultsTemplates",
                type: "int",
                nullable: false,
                defaultValue: 10);

            migrationBuilder.AddColumn<int>(
                name: "PreDiscardTotalVisibility",
                table: "SeriesResultsTemplates",
                type: "int",
                nullable: false,
                defaultValue: 10);

            migrationBuilder.AddColumn<bool>(
                name: "TotalsSeparator",
                table: "SeriesResultsTemplates",
                type: "bit",
                nullable: false,
                defaultValue: false);

            // The standard scoring columns were shown or hidden by the scoring system (and the
            // pre-discard total also by ShowPreDiscardTotal). Each of a club's views gets the
            // most visible setting from the club's scoring systems (Always = 1, before On
            // Larger Screens = 2), else Always when the view showed the pre-discard total.
            // The scoring system settings are left as they were.
            migrationBuilder.Sql(@"
UPDATE t SET
    PreDiscardTotalVisibility = COALESCE(s.PreDiscardTotal,
        CASE WHEN t.ShowPreDiscardTotal = 1 THEN 1 ELSE 10 END),
    ParticipationVisibility = COALESCE(s.Participation, 10)
FROM SeriesResultsTemplates t
OUTER APPLY (
    SELECT
        MIN(CASE WHEN v.PreDiscardTotal IN ('1', '2') THEN CAST(v.PreDiscardTotal AS int) END) AS PreDiscardTotal,
        MIN(CASE WHEN v.Participation IN ('1', '2') THEN CAST(v.Participation AS int) END) AS Participation
    FROM ScoringSystems ss
    CROSS APPLY (
        SELECT
            CASE WHEN ISJSON(ss.Settings) = 1
                THEN JSON_VALUE(ss.Settings, '$.ColumnVisibilities.preDiscardTotal') END AS PreDiscardTotal,
            CASE WHEN ISJSON(ss.Settings) = 1
                THEN JSON_VALUE(ss.Settings, '$.ColumnVisibilities.participation') END AS Participation
    ) v
    WHERE ss.ClubId = t.ClubId
) s;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ParticipationVisibility",
                table: "SeriesResultsTemplates");

            migrationBuilder.DropColumn(
                name: "PreDiscardTotalVisibility",
                table: "SeriesResultsTemplates");

            migrationBuilder.DropColumn(
                name: "TotalsSeparator",
                table: "SeriesResultsTemplates");
        }
    }
}
