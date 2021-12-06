using SailScores.Core.Model;

namespace SailScores.Web.Models.SailScores;

public class ScoringSystemDeleteViewModel : ScoringSystem
{
    public bool IsDeletable { get; set; }
    public string PreventDeleteReason { get; set; }
}