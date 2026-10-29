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
    // Custom header text; null or empty uses the default, translated header.
    public string SailNumberHeader { get; set; }
    public ColumnVisibility AlternativeSailNumberVisibility { get; set; } = ColumnVisibility.Hidden;
    public string AlternativeSailNumberHeader { get; set; }
    public ColumnVisibility CompetitorNameVisibility { get; set; } = ColumnVisibility.Always;
    public string CompetitorNameHeader { get; set; }
    public ColumnVisibility BoatNameVisibility { get; set; } = ColumnVisibility.OnLargerScreens;
    public string BoatNameHeader { get; set; }
    public ColumnVisibility CompetitorClubVisibility { get; set; } = ColumnVisibility.Hidden;
    public string CompetitorClubHeader { get; set; }
    // Null or empty uses the scoring system's total header.
    public string TotalHeader { get; set; }
    public IList<SeriesResultsTemplateCustomField> CustomFieldColumns { get; set; } = new List<SeriesResultsTemplateCustomField>();
    public IDictionary<Guid, CompetitorFieldDefinition> CustomFieldDefinitions { get; set; } = new Dictionary<Guid, CompetitorFieldDefinition>();

    // Backward compatibility - computed from CompetitorClubVisibility
    public bool ShowCompetitorClub => CompetitorClubVisibility != ColumnVisibility.Hidden;

    public bool IsExport { get; set; }
    public bool ShowExportButtons { get; set; }

    public bool ExcludeCompetitorLinks { get; set; }

    public bool PreferAlternativeSailNumbers { get; set; }

    // The sail number column shows the alternative number when the series prefers it, unless
    // the alternative number has its own column.
    public string GetSailNumber(FlatCompetitor competitor) =>
        PreferAlternativeSailNumbers
            && AlternativeSailNumberVisibility == ColumnVisibility.Hidden
            && !string.IsNullOrWhiteSpace(competitor?.AlternativeSailNumber)
            ? competitor.AlternativeSailNumber
            : competitor?.SailNumber;

    // The column repeated among the race columns of a long table, so rows can still be read:
    // the competitor name, or when it's hidden, the first shown of sail number, boat name and
    // alternative sail number. None when all of them are hidden.
    public RepeatedCompetitorColumn RepeatedColumn =>
        CompetitorNameVisibility != ColumnVisibility.Hidden ? RepeatedCompetitorColumn.CompetitorName
        : SailNumberVisibility != ColumnVisibility.Hidden ? RepeatedCompetitorColumn.SailNumber
        : BoatNameVisibility != ColumnVisibility.Hidden ? RepeatedCompetitorColumn.BoatName
        : AlternativeSailNumberVisibility != ColumnVisibility.Hidden ? RepeatedCompetitorColumn.AlternativeSailNumber
        : RepeatedCompetitorColumn.None;

    public string GetRepeatedValue(FlatCompetitor competitor) => RepeatedColumn switch
    {
        RepeatedCompetitorColumn.CompetitorName => competitor?.Name,
        RepeatedCompetitorColumn.SailNumber => GetSailNumber(competitor),
        RepeatedCompetitorColumn.BoatName => competitor?.BoatName,
        RepeatedCompetitorColumn.AlternativeSailNumber => competitor?.AlternativeSailNumber,
        _ => null
    };

    // From the results template: the scoring columns every scoring system has.
    public ColumnVisibility PreDiscardTotalVisibility { get; set; } = ColumnVisibility.Hidden;
    public ColumnVisibility ParticipationVisibility { get; set; } = ColumnVisibility.Hidden;

    // Scoring columns (after the total) that aren't hidden. The standard columns use the
    // template's visibility, others the scoring system's.
    public IList<FlatResultColumn> VisibleScoringColumns =>
        (Results?.Columns ?? new List<FlatResultColumn>())
            .Select(c => new FlatResultColumn
            {
                Key = c.Key,
                Header = c.Header,
                Format = c.Format,
                Visibility = ScoringColumns.GetVisibility(c, PreDiscardTotalVisibility, ParticipationVisibility)
            })
            .Where(c => c.Visibility != ColumnVisibility.Hidden)
            .ToList();

    public bool HighlightTopThree { get; set; } = true;
    public ResultsColumnSeparator ColumnSeparators { get; set; } = ResultsColumnSeparator.None;
    public ColumnVisibility RaceColumnsVisibility { get; set; } = ColumnVisibility.Always;
    public bool TotalsSeparator { get; set; }
    public bool ShowWeather { get; set; } = true;

    public bool ShowRaceColumns => RaceColumnsVisibility != ColumnVisibility.Hidden;

    // Results pages use the full page width for more than seven race columns, and the
    // normal width when the view hides the race columns.
    public static bool UsesWideLayout(FlatResults results, ColumnVisibility raceColumnsVisibility)
    {
        return raceColumnsVisibility != ColumnVisibility.Hidden
            && (results?.Races?.Count() ?? 0) > 7;
    }

    public bool DiscardStrikethrough { get; set; } = true;
    public bool DiscardFaded { get; set; } = true;

    // CSS class for a discarded score.
    public string DiscardCssClass => DiscardFaded ? "discardmuted" : string.Empty;

    // With no other discard styling, discards are shown in parentheses.
    public bool DiscardParentheses => !DiscardStrikethrough && !DiscardFaded;

    public IDictionary<string, SeriesResultsTemplateScoreCode> ScoreCodeDisplays { get; set; } =
        new Dictionary<string, SeriesResultsTemplateScoreCode>(StringComparer.OrdinalIgnoreCase);

    // The template's display setting for a score code, or null to show code and points.
    public SeriesResultsTemplateScoreCode GetScoreCodeDisplay(string code) =>
        !string.IsNullOrEmpty(code) && ScoreCodeDisplays.TryGetValue(code, out var display) ? display : null;

    // Copies the display settings of a results template into this table.
    public ResultsTableViewModel WithTemplate(ResolvedTemplate template)
    {
        SailNumberVisibility = template.SailNumberVisibility;
        SailNumberHeader = template.SailNumberHeader;
        AlternativeSailNumberVisibility = template.AlternativeSailNumberVisibility;
        AlternativeSailNumberHeader = template.AlternativeSailNumberHeader;
        CompetitorNameVisibility = template.CompetitorNameVisibility;
        CompetitorNameHeader = template.CompetitorNameHeader;
        BoatNameVisibility = template.BoatNameVisibility;
        BoatNameHeader = template.BoatNameHeader;
        CompetitorClubVisibility = template.CompetitorClubVisibility;
        CompetitorClubHeader = template.CompetitorClubHeader;
        TotalHeader = template.TotalHeader;
        PreDiscardTotalVisibility = template.PreDiscardTotalVisibility;
        ParticipationVisibility = template.ParticipationVisibility;
        HighlightTopThree = template.HighlightTopThree;
        ColumnSeparators = template.ColumnSeparators;
        RaceColumnsVisibility = template.RaceColumnsVisibility;
        TotalsSeparator = template.TotalsSeparator;
        ShowWeather = template.ShowWeather;
        DiscardStrikethrough = template.DiscardStrikethrough;
        DiscardFaded = template.DiscardFaded;
        ScoreCodeDisplays = template.ScoreCodes;
        return this;
    }

    private IList<FlatRace> _raceList;

    // Whether a separator line goes before the race column at this index. The first race
    // column has one when the totals are set apart.
    public bool HasSeparatorBefore(int raceIndex)
    {
        var races = _raceList ??= Results?.Races?.ToList();
        if (races == null || raceIndex < 0 || raceIndex >= races.Count)
        {
            return false;
        }
        if (raceIndex == 0)
        {
            return TotalsSeparator;
        }
        return ColumnSeparators switch
        {
            ResultsColumnSeparator.EveryColumn => true,
            ResultsColumnSeparator.RaceDays => races[raceIndex].Date?.Date != races[raceIndex - 1].Date?.Date,
            ResultsColumnSeparator.ChildSeries => races[raceIndex].GetChildSeriesId() is Guid childSeriesId
                && childSeriesId != races[raceIndex - 1].GetChildSeriesId(),
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
    public DateTime? UpdatedDate { get; set; }
    public string DefaultDateFormat { get; set; }
}

public enum RepeatedCompetitorColumn
{
    None,
    CompetitorName,
    SailNumber,
    BoatName,
    AlternativeSailNumber
}
