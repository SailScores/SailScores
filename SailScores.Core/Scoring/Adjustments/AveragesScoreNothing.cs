namespace SailScores.Core.Scoring.Adjustments;

// Cox-Sprague: average-scored codes get no score (issue 413). They aren't discards, so a
// code that counts as participation still counts.
public sealed class AveragesScoreNothing : IScoreAdjustment
{
    public void Apply(ScoringWorkspace workspace, SeriesResults results, SeriesCompetitorResults competitor)
    {
        foreach (var race in results.Races)
        {
            if (competitor.CalculatedScores.TryGetValue(race, out var score)
                && score != null
                && workspace.Host.IsAverage(score.RawScore?.Code))
            {
                score.ScoreValue = null;
            }
        }
    }
}
