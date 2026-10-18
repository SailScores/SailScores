using System.Collections.Generic;
using System.Linq;
using SailScores.Core.Model;

namespace SailScores.Core.Scoring.RacePoints;

// Cox-Sprague: points from the Cox-Sprague table for the boat's place and the number of
// starters. Tied boats get the same place (the table value for the lowest of them).
public sealed class CoxSpraguePoints : IRacePointsRule
{
    public decimal? Score(ScoringWorkspace workspace, IEnumerable<Score> allScores, Score score)
    {
        var host = workspace.Host;
        // The whole race, or (places recalculated within a fleet) the scores being scored.
        var raceScores = workspace.UseOriginalPlace && score.Place.HasValue && string.IsNullOrEmpty(score.Code)
            ? workspace.ScoresIn(score.Race)
            : allScores.Where(s => s.Race == score.Race);

        int starters = raceScores.Count(host.CountsAsStarted);
        int place = raceScores.Count(s =>
            score.Place.HasValue
            && s.Place <= score.Place
            && !host.AdjustsOtherScores(s));

        return CoxSpragueTable.GetScore(place, starters);
    }

    public decimal? PerfectScore(ScoringWorkspace workspace, IEnumerable<Score> allScores, Score score) =>
        CoxSpragueTable.GetScore(1, allScores.Count(s => s.Race == score.Race && workspace.Host.CountsAsStarted(s)));
}
