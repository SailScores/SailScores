using System.Linq;

namespace SailScores.Core.Scoring.Discards;

// VARC 2026: of the races that count, the competitor's best ScoresToCount scores count and
// the rest are discards (high point: the lowest scores go; among equal scores, the later
// races). Codes that can't be discarded always count. No ScoresToCount: every score in a
// counted race counts.
public sealed class KeepBestScores : IDiscardRule
{
    public void Apply(ScoringWorkspace workspace, SeriesResults results, SeriesCompetitorResults competitor)
    {
        var scoresToCount = workspace.System.EffectiveSettings?.ScoresToCount;
        if (scoresToCount == null)
        {
            return;
        }

        var excluded = results.SailedRaces
            .Where(r => !workspace.RacesNotCounted.Contains(r))
            .Select(r => competitor.CalculatedScores[r])
            .OrderByDescending(s => !(workspace.Host.GetScoreCode(s.RawScore)?.Discardable ?? true))
            .ThenByDescending(s => s.ScoreValue ?? 0m)
            .ThenBy(s => s.RawScore.Race.Date)
            .ThenBy(s => s.RawScore.Race.Order)
            .Skip(scoresToCount.Value);
        foreach (var score in excluded)
        {
            score.Discard = true;
        }
    }
}
