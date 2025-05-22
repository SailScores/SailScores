using SailScores.Core.Model;

namespace SailScores.Web.Models.SailScores;

public class HistoricalNote
{

    public HistoricalNote(CompetitorChange change)
    {
        Date = change.ChangeTimeStamp;
        Summary = change.Summary;
        if(!String.IsNullOrWhiteSpace(change.NewValue))
        {
            Summary += " to " + change.NewValue;
        }
        ChangedBy = change.ChangedBy;
    }

    public HistoricalNote(Core.Model.HistoricalNote note)
    {
        Date = note.Date;
        Summary = note.Summary;
        ChangedBy = String.Empty;
        Aggregation = note.Aggregation;
    }

    public DateTime Date { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string ChangedBy { get; set; } = string.Empty;
    public HistoricalNoteAggregation Aggregation { get; set; } = HistoricalNoteAggregation.None;
}