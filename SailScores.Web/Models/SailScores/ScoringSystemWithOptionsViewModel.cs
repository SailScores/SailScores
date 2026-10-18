using SailScores.Core.Model;

namespace SailScores.Web.Models.SailScores;

// Discard pattern and settings validation depends on the parent system, so it is done by
// ScoringSystemController using ScoringCalculatorRegistry.GetValidationErrors.
public class ScoringSystemWithOptionsViewModel : ScoringSystem
{
    // Parent options are resolved by ScoringService, so they carry CalculatorType,
    // EffectiveSettings and EffectiveDiscardMethod for the settings UI.
    public IList<ScoringSystem> ParentSystemOptions { get; set; }
    public IList<ScoreCode> ScoreCodeOptions { get; set; }
}
