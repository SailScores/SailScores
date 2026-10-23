using SailScores.Api.Enumerations;

namespace SailScores.Core.Model;

// How a results template shows one score code.
public class SeriesResultsTemplateScoreCode
{
    public string Code { get; set; }
    public ScoreCodeDisplay Display { get; set; }
    public bool Faded { get; set; }

    // True when the code is shown the same as without a setting.
    public bool IsDefault => Display == ScoreCodeDisplay.CodeAndPoints && !Faded;
}
