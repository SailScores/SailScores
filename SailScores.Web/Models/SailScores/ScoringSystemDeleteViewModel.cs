using SailScores.Core.Model;

namespace SailScores.Web.Models.SailScores;

public class ScoringSystemDeleteViewModel : ScoringSystem
{
    public bool InUse { get; set; }
}