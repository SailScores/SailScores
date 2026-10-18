namespace SailScores.Core.Scoring.Discards;

// Nothing is discarded (Top X: its total takes the best scores instead).
public sealed class NoDiscards : IDiscardRule
{
    public void Apply(ScoringWorkspace workspace, SeriesResults results, SeriesCompetitorResults competitor)
    {
    }
}
