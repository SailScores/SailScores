using System;
using System.Collections.Generic;
using SailScores.Api.Enumerations;

namespace SailScores.Core.Model;

public class SeriesResultsTemplate
{
    public Guid Id { get; set; }
    public Guid ClubId { get; set; }
    public string Name { get; set; }

    // Column visibility settings
    public ColumnVisibility SailNumberVisibility { get; set; }
    public ColumnVisibility CompetitorNameVisibility { get; set; }
    public string CompetitorNameHeader { get; set; }
    public ColumnVisibility BoatNameVisibility { get; set; }
    public string BoatNameHeader { get; set; }
    public ColumnVisibility CompetitorClubVisibility { get; set; }
    // Scoring columns every scoring system has; calculator-specific columns are set on the
    // scoring system.
    public ColumnVisibility PreDiscardTotalVisibility { get; set; } = ColumnVisibility.Hidden;
    public ColumnVisibility ParticipationVisibility { get; set; } = ColumnVisibility.Hidden;
    public bool ShowClubLogo { get; set; }
    public bool HighlightTopThree { get; set; } = true;
    public ResultsColumnSeparator ColumnSeparators { get; set; }
    public ColumnVisibility RaceColumnsVisibility { get; set; } = ColumnVisibility.Always;
    // A line between the totals and the race columns.
    public bool TotalsSeparator { get; set; }
    public bool DiscardStrikethrough { get; set; } = true;
    public bool DiscardFaded { get; set; } = true;
    public ResultsTextColor DiscardColor { get; set; }
    public IList<SeriesResultsTemplateCustomField> CustomFields { get; set; } = new List<SeriesResultsTemplateCustomField>();
    public IList<SeriesResultsTemplateScoreCode> ScoreCodes { get; set; } = new List<SeriesResultsTemplateScoreCode>();
}
