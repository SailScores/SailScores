using SailScores.Core.Model;

namespace SailScores.Web.Models.SailScores;

public class ScoringSystemCanBeDeletedViewModel : ScoringSystem
{
    public bool InUse { get; set; }
}