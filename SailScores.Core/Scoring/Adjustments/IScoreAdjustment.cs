namespace SailScores.Core.Scoring.Adjustments;

// Changes a competitor's scores after score code formulas have run, before discards: caps
// at the DNC score, club rules. A calculator applies its adjustments in order.
public interface IScoreAdjustment
{
    void Apply(ScoringWorkspace workspace, SeriesResults results, SeriesCompetitorResults competitor);
}
