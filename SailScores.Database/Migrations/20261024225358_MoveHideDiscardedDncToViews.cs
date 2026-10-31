using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SailScores.Database.Migrations
{
    /// <inheritdoc />
    public partial class MoveHideDiscardedDncToViews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // "Hide discarded DNC scores" moves from each series (Series.HideDncDiscards) to the
            // results view, as the DNC score code shown "Hidden when discarded" (Display 3).
            // Series.HideDncDiscards is left as it was, for the previous release and rollback.
            //
            // 1. Clubs with hiding series but no views get the Standard and Regatta views
            //    that SeedDefaultTemplatesAsync would create, as their defaults.
            // 2. Each series' view is its own, else the club default for its type (regatta,
            //    summary falling back to standard, or standard).
            // 3. For each view used by a hiding series (and not already hiding every DNC):
            //    the view takes the setting most of its series use. Series using the other
            //    setting get a copy of the view with that setting, named for it, so no series
            //    changes how it looks.
            migrationBuilder.Sql(@"
DECLARE @seeded TABLE (ClubId uniqueidentifier PRIMARY KEY, StandardId uniqueidentifier, RegattaId uniqueidentifier);
INSERT @seeded (ClubId, StandardId, RegattaId)
SELECT c.Id, NEWID(), NEWID()
FROM Clubs c
WHERE EXISTS (SELECT 1 FROM Series s WHERE s.ClubId = c.Id AND s.HideDncDiscards = 1)
    AND NOT EXISTS (SELECT 1 FROM SeriesResultsTemplates t WHERE t.ClubId = c.Id);

INSERT SeriesResultsTemplates (Id, ClubId, Name, SailNumberVisibility, CompetitorNameVisibility,
    CompetitorNameHeader, BoatNameVisibility, BoatNameHeader, CompetitorClubVisibility,
    ShowPreDiscardTotal, ShowClubLogo, HighlightTopThree, ColumnSeparators, RaceColumnsVisibility,
    DiscardStrikethrough, DiscardFaded, DiscardColor, PreDiscardTotalVisibility,
    ParticipationVisibility, TotalsSeparator)
SELECT StandardId, ClubId, 'Standard', 1, 1, 'Helm', 2, 'Boat', 10, 0, 0, 1, 0, 1, 1, 1, 0, 10, 10, 0
FROM @seeded
UNION ALL
SELECT RegattaId, ClubId, 'Regatta', 1, 1, 'Helm', 2, 'Boat', 2, 0, 0, 1, 0, 1, 1, 1, 0, 10, 10, 0
FROM @seeded;

-- The seeded views show the standard scoring columns the club's scoring systems showed,
-- as SeedDefaultTemplatesAsync does.
UPDATE t SET
    PreDiscardTotalVisibility = COALESCE(s.PreDiscardTotal, 10),
    ShowPreDiscardTotal = CASE WHEN s.PreDiscardTotal IS NULL THEN 0 ELSE 1 END,
    ParticipationVisibility = COALESCE(s.Participation, 10)
FROM SeriesResultsTemplates t
JOIN @seeded x ON x.ClubId = t.ClubId
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
) s;

UPDATE c SET
    DefaultSeriesResultsTemplateId = x.StandardId,
    DefaultRegattaSeriesResultsTemplateId = x.RegattaId
FROM Clubs c
JOIN @seeded x ON x.ClubId = c.Id;

DECLARE @series TABLE (SeriesId uniqueidentifier PRIMARY KEY, ViewId uniqueidentifier, Hide int);
INSERT @series (SeriesId, ViewId, Hide)
SELECT s.Id,
    COALESCE(s.SeriesResultsTemplateId,
        CASE
            WHEN s.Type = 2 OR EXISTS (SELECT 1 FROM RegattaSeries rs WHERE rs.SeriesId = s.Id)
                THEN c.DefaultRegattaSeriesResultsTemplateId
            WHEN s.Type = 3
                THEN COALESCE(c.DefaultSummarySeriesResultsTemplateId, c.DefaultSeriesResultsTemplateId)
            ELSE c.DefaultSeriesResultsTemplateId
        END),
    CASE WHEN s.HideDncDiscards = 1 THEN 1 ELSE 0 END
FROM Series s
JOIN Clubs c ON c.Id = s.ClubId;

DECLARE @views TABLE (ViewId uniqueidentifier PRIMARY KEY, Hiders int, Others int,
    MostHide bit, CopyId uniqueidentifier);
INSERT @views (ViewId, Hiders, Others, MostHide, CopyId)
SELECT ViewId, SUM(Hide), SUM(1 - Hide),
    CASE WHEN SUM(Hide) >= SUM(1 - Hide) THEN 1 ELSE 0 END,
    CASE WHEN SUM(1 - Hide) > 0 THEN NEWID() END
FROM @series
WHERE ViewId IS NOT NULL
    AND NOT EXISTS (
        SELECT 1 FROM SeriesResultsTemplateScoreCodes sc
        WHERE sc.SeriesResultsTemplateId = ViewId AND sc.Code = 'DNC' AND sc.Display = 2)
GROUP BY ViewId
HAVING SUM(Hide) > 0;

-- Copies, with their custom fields and score codes, made before any view changes.
INSERT SeriesResultsTemplates (Id, ClubId, Name, SailNumberVisibility, CompetitorNameVisibility,
    CompetitorNameHeader, BoatNameVisibility, BoatNameHeader, CompetitorClubVisibility,
    ShowPreDiscardTotal, ShowClubLogo, HighlightTopThree, ColumnSeparators, RaceColumnsVisibility,
    DiscardStrikethrough, DiscardFaded, DiscardColor, PreDiscardTotalVisibility,
    ParticipationVisibility, TotalsSeparator)
SELECT v.CopyId, t.ClubId,
    LEFT(t.Name, 170) + CASE WHEN v.MostHide = 1
        THEN ' (discarded DNC shown)' ELSE ' (discarded DNC hidden)' END,
    t.SailNumberVisibility, t.CompetitorNameVisibility,
    t.CompetitorNameHeader, t.BoatNameVisibility, t.BoatNameHeader, t.CompetitorClubVisibility,
    t.ShowPreDiscardTotal, t.ShowClubLogo, t.HighlightTopThree, t.ColumnSeparators, t.RaceColumnsVisibility,
    t.DiscardStrikethrough, t.DiscardFaded, t.DiscardColor, t.PreDiscardTotalVisibility,
    t.ParticipationVisibility, t.TotalsSeparator
FROM SeriesResultsTemplates t
JOIN @views v ON v.ViewId = t.Id
WHERE v.CopyId IS NOT NULL;

INSERT SeriesResultsTemplateCustomFields (Id, SeriesResultsTemplateId, FieldDefinitionId, Visibility, DisplayOrder)
SELECT NEWID(), v.CopyId, f.FieldDefinitionId, f.Visibility, f.DisplayOrder
FROM SeriesResultsTemplateCustomFields f
JOIN @views v ON v.ViewId = f.SeriesResultsTemplateId
WHERE v.CopyId IS NOT NULL;

INSERT SeriesResultsTemplateScoreCodes (Id, SeriesResultsTemplateId, Code, Display, Faded)
SELECT NEWID(), v.CopyId, sc.Code, sc.Display, sc.Faded
FROM SeriesResultsTemplateScoreCodes sc
JOIN @views v ON v.ViewId = sc.SeriesResultsTemplateId
WHERE v.CopyId IS NOT NULL;

-- The views that hide discarded DNCs: originals most of whose series hide them, and the
-- copies made for hiding series.
DECLARE @hiding TABLE (TemplateId uniqueidentifier PRIMARY KEY);
INSERT @hiding (TemplateId)
SELECT CASE WHEN MostHide = 1 THEN ViewId ELSE CopyId END
FROM @views;

UPDATE sc SET Display = 3
FROM SeriesResultsTemplateScoreCodes sc
JOIN @hiding h ON h.TemplateId = sc.SeriesResultsTemplateId
WHERE sc.Code = 'DNC';

INSERT SeriesResultsTemplateScoreCodes (Id, SeriesResultsTemplateId, Code, Display, Faded)
SELECT NEWID(), h.TemplateId, 'DNC', 3, 0
FROM @hiding h
WHERE NOT EXISTS (
    SELECT 1 FROM SeriesResultsTemplateScoreCodes sc
    WHERE sc.SeriesResultsTemplateId = h.TemplateId AND sc.Code = 'DNC');

-- Series using the less common setting move to the copy.
UPDATE s SET SeriesResultsTemplateId = v.CopyId
FROM Series s
JOIN @series x ON x.SeriesId = s.Id
JOIN @views v ON v.ViewId = x.ViewId
WHERE v.CopyId IS NOT NULL
    AND x.Hide = CASE WHEN v.MostHide = 1 THEN 0 ELSE 1 END;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Data only. Series.HideDncDiscards was left in place, so the previous release
            // still hides discarded DNCs; the views and copies made here are kept.
        }
    }
}
