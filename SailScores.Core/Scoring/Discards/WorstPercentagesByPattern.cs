using System.Linq;

namespace SailScores.Core.Scoring.Discards;

// Percentage systems (high point percentage, low point averages): the discard pattern is
// applied to the races the competitor sailed in each group, not the group's race count. The
// worst score as a share of the perfect score goes first, earlier races first among equals.
// Only codes that came to the start, and can be discarded, are candidates.
public sealed class WorstPercentagesByPattern : IDiscardRule
{
    private readonly bool _lowerScoreWins;
    private readonly bool _countAveragedRaces;

    // lowerScoreWins: low point averages discard the highest share; high point the lowest.
    // countAveragedRaces: races sailed are those included in the average (low point
    // averages), rather than those started; races counting as participation count either way.
    public WorstPercentagesByPattern(bool lowerScoreWins, bool countAveragedRaces)
    {
        _lowerScoreWins = lowerScoreWins;
        _countAveragedRaces = countAveragedRaces;
    }

    public void Apply(ScoringWorkspace workspace, SeriesResults results, SeriesCompetitorResults competitor)
    {
        var host = workspace.Host;
        PatternDiscards.Mark(
            workspace,
            results,
            competitor,
            (races, scores) => host.DiscardsForRaceCount(scores.Count(s =>
                (_countAveragedRaces ? host.IsIncludedInAverage(s.RawScore) : host.CountsAsStarted(s.RawScore))
                || host.CountsAsParticipation(s.RawScore))),
            scores => (_lowerScoreWins
                    ? scores.OrderByDescending(s => s.ScoreValue / s.PerfectScoreValue)
                    : scores.OrderBy(s => s.ScoreValue / s.PerfectScoreValue))
                .ThenBy(s => s.RawScore.Race.Date)
                .ThenBy(s => s.RawScore.Race.Order)
                .Where(s => host.CameToStart(s.RawScore) && (host.GetScoreCode(s.RawScore)?.Discardable ?? true)));
    }
}
