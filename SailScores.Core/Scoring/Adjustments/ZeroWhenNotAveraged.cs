namespace SailScores.Core.Scoring.Adjustments;

// Low point averages: a score that isn't included in the average scores 0, which the
// average's sum and count both leave out.
public sealed class ZeroWhenNotAveraged : IScoreAdjustment
{
    public void Apply(ScoringWorkspace workspace, SeriesResults results, SeriesCompetitorResults competitor)
    {
        foreach (var race in results.SailedRaces)
        {
            var score = competitor.CalculatedScores[race];
            if (!workspace.Host.IsIncludedInAverage(score.RawScore))
            {
                score.ScoreValue = 0.0m;
            }
        }
    }
}
