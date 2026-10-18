using System.Linq;

namespace SailScores.Core.Scoring.Tiebreaks;

// RRS A8.2: compare scores in the last race, then the race before, and so on, discards
// included; at the first difference the better score ranks ahead. A missing score value
// counts as decimal.MaxValue (worst in low point, best in high point).
public sealed class AppendixA82 : ITieBreak
{
    private readonly bool _lowerScoreWins;

    private AppendixA82(bool lowerScoreWins)
    {
        _lowerScoreWins = lowerScoreWins;
    }

    public static readonly AppendixA82 LowPoint = new(lowerScoreWins: true);
    public static readonly AppendixA82 HighPoint = new(lowerScoreWins: false);

    public int Compare(ScoringWorkspace workspace, SeriesCompetitorResults x, SeriesCompetitorResults y)
    {
        var xScores = LastRaceFirst(x);
        var yScores = LastRaceFirst(y);

        for (int i = 0; i < xScores.Length; i++)
        {
            if (xScores[i] != yScores[i])
            {
                return (xScores[i] < yScores[i]) == _lowerScoreWins ? -1 : 1;
            }
        }
        return 0;
    }

    private static decimal[] LastRaceFirst(SeriesCompetitorResults results) =>
        results.CalculatedScores
            .OrderByDescending(s => s.Key.Date)
            .ThenByDescending(s => s.Key.Order)
            .Select(s => s.Value.ScoreValue ?? decimal.MaxValue)
            .ToArray();
}
