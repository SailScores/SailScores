namespace SailScores.Core.Scoring.Discards;

// Marks which of a competitor's scores are discarded. A calculator applies its discard rules
// in order.
public interface IDiscardRule
{
    void Apply(ScoringWorkspace workspace, SeriesResults results, SeriesCompetitorResults competitor);
}
