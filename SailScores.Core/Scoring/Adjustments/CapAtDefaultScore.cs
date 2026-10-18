namespace SailScores.Core.Scoring.Adjustments;

// Low point: no score is worse (higher) than the score for no result (DNC). Child series
// scores are left as they are.
public sealed class CapAtDefaultScore : IScoreAdjustment
{
    public void Apply(ScoringWorkspace workspace, SeriesResults results, SeriesCompetitorResults competitor)
    {
        foreach (var race in results.SailedRaces)
        {
            var score = competitor.CalculatedScores[race];
            var defaultScore = workspace.Host.DefaultScore(race, results);
            if (score?.ScoreValue != null && score.ScoreValue > defaultScore && !score.IsPresetScore)
            {
                score.ScoreValue = defaultScore;
            }
        }
    }
}
