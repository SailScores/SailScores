using SailScores.Core.FlatModel;
using SailScores.Core.Model;
using SailScores.Core.Scoring;

namespace SailScores.Web.Models.SailScores;

public class ScoreCellViewModel
{
    public FlatCalculatedScore Result { get; set; } 
    public bool IsPercentSystem { get; set; } 
    public bool LowerScoreWins { get; set; }
    public bool HighlightTopThree { get; set; } = true;
    public string CssClass { get; set; }

    // From the results template: how a discarded score looks.
    public bool DiscardStrikethrough { get; set; } = true;
    public string DiscardCssClass { get; set; } = "discardmuted";
    public bool DiscardParentheses { get; set; }

    // From the results template; null shows the code and points.
    public SeriesResultsTemplateScoreCode CodeDisplay { get; set; }
    public Dictionary<string, ScoreCodeSummary> ScoreCodesUsed { get; set; }
}