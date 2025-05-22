using System;

namespace SailScores.Core.Model;

public enum HistoricalNoteAggregation
{
    None,       // Time-specific (not aggregated)
    Day,
    Month,
    Year
}

public class HistoricalNote
{
    public DateTime Date { get; set; }
    public String Summary { get; set; }

    public HistoricalNoteAggregation Aggregation { get; set; } = HistoricalNoteAggregation.None;
}
