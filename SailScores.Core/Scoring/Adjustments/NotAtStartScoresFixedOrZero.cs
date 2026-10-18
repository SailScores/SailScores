namespace SailScores.Core.Scoring.Adjustments;

// VARC 2026: a code that didn't come to the start (DNC) scores its fixed or manual value,
// otherwise 0, whatever its formula. So does a missing result. Child series scores are left
// as they are.
public sealed class NotAtStartScoresFixedOrZero : IScoreAdjustment
{
    public void Apply(ScoringWorkspace workspace, SeriesResults results, SeriesCompetitorResults competitor)
    {
        foreach (var race in results.SailedRaces)
        {
            var score = competitor.CalculatedScores[race];
            if (score.IsPresetScore)
            {
                continue;
            }
            var scoreCode = workspace.Host.GetScoreCode(score.RawScore);
            if (scoreCode == null)
            {
                score.ScoreValue ??= 0m;
            }
            else if (!workspace.Host.CameToStart(score.RawScore))
            {
                score.ScoreValue = workspace.Host.FixedOrManualValue(score, scoreCode) ?? 0m;
            }
        }
    }
}
