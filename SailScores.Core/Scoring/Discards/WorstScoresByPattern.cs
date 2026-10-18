using System.Linq;

namespace SailScores.Core.Scoring.Discards;

// Low point: in each group of races, the discard pattern for the group's race count gives
// how many scores to discard; the highest go, earlier races first among equal scores.
// Codes that can't be discarded stay.
public sealed class WorstScoresByPattern : IDiscardRule
{
    public void Apply(ScoringWorkspace workspace, SeriesResults results, SeriesCompetitorResults competitor)
    {
        PatternDiscards.Mark(
            workspace,
            results,
            competitor,
            (races, scores) => workspace.Host.DiscardsForRaceCount(races.Count),
            scores => scores.OrderByDescending(s => s.ScoreValue)
                .ThenBy(s => s.RawScore.Race.Date)
                .ThenBy(s => s.RawScore.Race.Order)
                .Where(s => workspace.Host.GetScoreCode(s.RawScore)?.Discardable ?? true));
    }
}
