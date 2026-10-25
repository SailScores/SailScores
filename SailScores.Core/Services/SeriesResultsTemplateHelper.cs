using System;
using System.Collections.Generic;
using System.Linq;
using SailScores.Api.Enumerations;
using SailScores.Core.FlatModel;
using SailScores.Core.Model;
using SailScores.Core.Scoring;

namespace SailScores.Core.Services;

public static class SeriesResultsTemplateHelper
{
    public static ResolvedTemplate GetResolvedTemplate(SeriesResultsTemplate template, bool isRegatta = false)
    {
        return GetResolvedTemplate(template, isRegatta, fallbackTemplate: null);
    }

    public static ResolvedTemplate GetResolvedTemplate(
        SeriesResultsTemplate template,
        bool isRegatta,
        SeriesResultsTemplate fallbackTemplate)
    {
        var effectiveTemplate = template ?? fallbackTemplate;
        if (effectiveTemplate == null)
        {
            return GetDefaultTemplate(isRegatta);
        }

        return new ResolvedTemplate
        {
            SailNumberVisibility = effectiveTemplate.SailNumberVisibility,
            CompetitorNameVisibility = effectiveTemplate.CompetitorNameVisibility,
            CompetitorNameHeader = effectiveTemplate.CompetitorNameHeader ?? "Helm",
            BoatNameVisibility = effectiveTemplate.BoatNameVisibility,
            BoatNameHeader = effectiveTemplate.BoatNameHeader ?? "Boat",
            CompetitorClubVisibility = effectiveTemplate.CompetitorClubVisibility,
            PreDiscardTotalVisibility = effectiveTemplate.PreDiscardTotalVisibility,
            ParticipationVisibility = effectiveTemplate.ParticipationVisibility,
            ShowClubLogo = effectiveTemplate.ShowClubLogo,
            HighlightTopThree = effectiveTemplate.HighlightTopThree,
            ColumnSeparators = effectiveTemplate.ColumnSeparators,
            RaceColumnsVisibility = effectiveTemplate.RaceColumnsVisibility,
            TotalsSeparator = effectiveTemplate.TotalsSeparator,
            DiscardStrikethrough = effectiveTemplate.DiscardStrikethrough,
            DiscardFaded = effectiveTemplate.DiscardFaded,
            DiscardColor = effectiveTemplate.DiscardColor,
            ScoreCodes = (effectiveTemplate.ScoreCodes ?? new List<SeriesResultsTemplateScoreCode>())
                .Where(c => !string.IsNullOrWhiteSpace(c.Code))
                .GroupBy(c => c.Code, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase),
        };
    }

    public static ResolvedTemplate GetDefaultTemplate(bool isRegatta = false)
    {
        return new ResolvedTemplate
        {
            SailNumberVisibility = ColumnVisibility.Always,
            CompetitorNameVisibility = ColumnVisibility.Always,
            CompetitorNameHeader = "Helm",
            BoatNameVisibility = ColumnVisibility.OnLargerScreens,
            BoatNameHeader = "Boat",
            CompetitorClubVisibility = isRegatta ? ColumnVisibility.OnLargerScreens : ColumnVisibility.Hidden,
            PreDiscardTotalVisibility = ColumnVisibility.Hidden,
            ParticipationVisibility = ColumnVisibility.Hidden,
            ShowClubLogo = false,
            HighlightTopThree = true,
            ColumnSeparators = ResultsColumnSeparator.None,
            RaceColumnsVisibility = ColumnVisibility.Always,
            TotalsSeparator = false,
            DiscardStrikethrough = true,
            DiscardFaded = true,
            DiscardColor = ResultsTextColor.Default,
        };
    }
}

public class ResolvedTemplate
{
    public ColumnVisibility SailNumberVisibility { get; set; }
    public ColumnVisibility CompetitorNameVisibility { get; set; }
    public string CompetitorNameHeader { get; set; }
    public ColumnVisibility BoatNameVisibility { get; set; }
    public string BoatNameHeader { get; set; }
    public ColumnVisibility CompetitorClubVisibility { get; set; }
    public ColumnVisibility PreDiscardTotalVisibility { get; set; } = ColumnVisibility.Hidden;
    public ColumnVisibility ParticipationVisibility { get; set; } = ColumnVisibility.Hidden;
    public bool ShowClubLogo { get; set; }
    public bool HighlightTopThree { get; set; } = true;
    public ResultsColumnSeparator ColumnSeparators { get; set; }
    public ColumnVisibility RaceColumnsVisibility { get; set; } = ColumnVisibility.Always;
    public bool TotalsSeparator { get; set; }
    public bool DiscardStrikethrough { get; set; } = true;
    public bool DiscardFaded { get; set; } = true;
    public ResultsTextColor DiscardColor { get; set; }

    // Visibility of a scoring column: the shared columns follow the template, calculator-specific
    // columns keep the scoring system's visibility.
    public ColumnVisibility GetScoringColumnVisibility(FlatResultColumn column) =>
        ScoringColumns.GetVisibility(column, PreDiscardTotalVisibility, ParticipationVisibility);

    // Score code display settings keyed by code, ignoring case. Codes not listed show code and points.
    public IDictionary<string, SeriesResultsTemplateScoreCode> ScoreCodes { get; set; } =
        new Dictionary<string, SeriesResultsTemplateScoreCode>(StringComparer.OrdinalIgnoreCase);
}
