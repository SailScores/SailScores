using System.Collections.Generic;
using System.Linq;

namespace SailScores.Core.Scoring.Tiebreaks;

// RRS A8.1 as VARC 2026 applies it, high point: compare the scores that count (not
// discarded), highest first; at the first difference the higher score ranks ahead. A missing
// score, or a score with no value, counts as 0.
public sealed class CountedScoresBestFirst : ITieBreak
{
    public int Compare(ScoringWorkspace workspace, SeriesCompetitorResults x, SeriesCompetitorResults y) =>
        HighPointSequence.Compare(Scores(x), Scores(y));

    private static List<decimal> Scores(SeriesCompetitorResults results) =>
        results.CalculatedScores.Values
            .Where(s => !s.Discard)
            .Select(s => s.ScoreValue ?? 0m)
            .OrderByDescending(s => s)
            .ToList();
}
