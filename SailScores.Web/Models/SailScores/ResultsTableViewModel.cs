using System;
using System.Collections.Generic;
using System.Linq;
using SailScores.Api.Enumerations;
using SailScores.Core.FlatModel;
using SailScores.Core.Model;
using SailScores.Core.Scoring;
using SailScores.Core.Services;

namespace SailScores.Web.Models.SailScores;

public class ResultsTableViewModel
{
    public Guid SeriesId { get; set; }
    public FlatResults Results { get; set; }

    public bool ShowTrend { get; set; }
    public bool ShowTrendOnAllDevices { get; set; }

    // Column visibility settings from template
    public ColumnVisibility SailNumberVisibility { get; set; } = ColumnVisibility.Always;
    public ColumnVisibility CompetitorNameVisibility { get; set; } = ColumnVisibility.Always;
    public string CompetitorNameHeader { get; set; } = "Helm";
    public ColumnVisibility BoatNameVisibility { get; set; } = ColumnVisibility.OnLargerScreens;
    public string BoatNameHeader { get; set; } = "Boat";
    public ColumnVisibility CompetitorClubVisibility { get; set; } = ColumnVisibility.Hidden;
    public IList<SeriesResultsTemplateCustomField> CustomFieldColumns { get; set; } = new List<SeriesResultsTemplateCustomField>();
    public IDictionary<Guid, CompetitorFieldDefinition> CustomFieldDefinitions { get; set; } = new Dictionary<Guid, CompetitorFieldDefinition>();

    // Backward compatibility - computed from CompetitorClubVisibility
    public bool ShowCompetitorClub => CompetitorClubVisibility != ColumnVisibility.Hidden;

    public bool IsExport { get; set; }
    public bool ShowExportButtons { get; set; }

    public bool ExcludeCompetitorLinks { get; set; }

    public bool PreferAlternativeSailNumbers { get; set; }

    // From the results template: shows the pre-discard total scoring column even when
    // the scoring system hides it.
    public bool ShowPreDiscardTotal { get; set; }

    // Scoring columns (after the total) that aren't hidden, with the template override applied.
    public IList<FlatResultColumn> VisibleScoringColumns =>
        (Results?.Columns ?? new List<FlatResultColumn>())
            .Select(c => ShowPreDiscardTotal
                    && c.Key == ScoringColumns.PreDiscardTotalKey
                    && c.Visibility == ColumnVisibility.Hidden
                ? new FlatResultColumn
                {
                    Key = c.Key,
                    Header = c.Header,
                    Format = c.Format,
                    Visibility = ColumnVisibility.Always
                }
                : c)
            .Where(c => c.Visibility != ColumnVisibility.Hidden)
            .ToList();

    public bool HighlightTopThree { get; set; } = true;
    public ResultsColumnSeparator ColumnSeparators { get; set; } = ResultsColumnSeparator.None;
    public ColumnVisibility RaceColumnsVisibility { get; set; } = ColumnVisibility.Always;

    public bool ShowRaceColumns => RaceColumnsVisibility != ColumnVisibility.Hidden;

    public IDictionary<string, SeriesResultsTemplateScoreCode> ScoreCodeDisplays { get; set; } =
        new Dictionary<string, SeriesResultsTemplateScoreCode>(StringComparer.OrdinalIgnoreCase);

    // The template's display setting for a score code, or null to show code and points.
    public SeriesResultsTemplateScoreCode GetScoreCodeDisplay(string code) =>
        !string.IsNullOrEmpty(code) && ScoreCodeDisplays.TryGetValue(code, out var display) ? display : null;

    // Copies the display settings of a results template into this table.
    public ResultsTableViewModel WithTemplate(ResolvedTemplate template)
    {
        SailNumberVisibility = template.SailNumberVisibility;
        CompetitorNameVisibility = template.CompetitorNameVisibility;
        CompetitorNameHeader = template.CompetitorNameHeader;
        BoatNameVisibility = template.BoatNameVisibility;
        BoatNameHeader = template.BoatNameHeader;
        CompetitorClubVisibility = template.CompetitorClubVisibility;
        ShowPreDiscardTotal = template.ShowPreDiscardTotal;
        HighlightTopThree = template.HighlightTopThree;
        ColumnSeparators = template.ColumnSeparators;
        RaceColumnsVisibility = template.RaceColumnsVisibility;
        ScoreCodeDisplays = template.ScoreCodes;
        return this;
    }

    private IList<FlatRace> _raceList;

    // Whether a separator line goes before the race column at this index.
    public bool HasSeparatorBefore(int raceIndex)
    {
        var races = _raceList ??= Results?.Races?.ToList();
        if (races == null || raceIndex <= 0 || raceIndex >= races.Count)
        {
            return false;
        }
        return ColumnSeparators switch
        {
            ResultsColumnSeparator.EveryColumn => true,
            ResultsColumnSeparator.RaceDays => races[raceIndex].Date?.Date != races[raceIndex - 1].Date?.Date,
            _ => false
        };
    }

    // CSS classes for a race column cell: the race column visibility and any separator.
    public string RaceColumnCssClass(int raceIndex)
    {
        var classes = RaceColumnsVisibility == ColumnVisibility.OnLargerScreens ? "d-none d-sm-table-cell" : "";
        return HasSeparatorBefore(raceIndex) ? $"{classes} results-separator".Trim() : classes;
    }

    public string TrendLegend { get; set; }
    public bool HideDncDiscards { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public string DefaultDateFormat { get; set; }
}
