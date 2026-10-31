using System.Collections.Generic;
using System.Linq;
using SailScores.Core.Model;
using SailScores.Core.Scoring.Formulas;

namespace SailScores.Core.Scoring.RacePoints;

// VARC 2026: Top X points for a boat's place in its division at an event. The boats racing
// are every boat with a score other than DNC (a result, or a code that came to the start), so
// a DNS counts toward the under-five reduction. Tied boats (the same place, or a TIE code
// after the boat ahead) both score the points for the better place: two tied for 2nd score
// 99 each, and the next boat 97.
public sealed class VarcEventPoints : IRacePointsRule
{
    public decimal? Score(ScoringWorkspace workspace, IEnumerable<Score> allScores, Score score)
    {
        var host = workspace.Host;
        // The whole race, or (places recalculated within a fleet) the scores being scored.
        var raceScores = (workspace.UseOriginalPlace && score.Place.HasValue && string.IsNullOrEmpty(score.Code)
            ? workspace.ScoresIn(score.Race)
            : allScores.Where(s => s.Race == score.Race))
            .ToList();

        int boatsRacing = raceScores.Count(s => IsRacing(host, s));
        var place = TiedPlace(host, raceScores, score);
        int position = place.HasValue
            ? 1 + raceScores.Count(s => s.Place < place && !host.AdjustsOtherScores(s))
            : 0;

        return TopXPoints.ForPlace(position, boatsRacing);
    }

    public decimal? PerfectScore(ScoringWorkspace workspace, IEnumerable<Score> allScores, Score score) =>
        TopXPoints.ForPlace(1, allScores.Count());

    // A result, or a code that came to the start. A score with neither isn't racing.
    public static bool IsRacing(IScoringHost host, Score score) =>
        host.GetScoreCode(score) == null
            ? (score.Place ?? 0) != 0 || score.PresetScoreValue.HasValue
            : host.CameToStart(score);

    // A TIE code takes the place of the boat just ahead, following a run of TIEs back.
    private static int? TiedPlace(IScoringHost host, IList<Score> raceScores, Score score)
    {
        var current = score;
        while (current.Place != null && TieFormula.IsTie(host.GetScoreCode(current)))
        {
            var ahead = raceScores
                .Where(s => s.Place < current.Place)
                .OrderByDescending(s => s.Place)
                .FirstOrDefault();
            if (ahead == null)
            {
                break;
            }
            current = ahead;
        }
        return current.Place;
    }
}
