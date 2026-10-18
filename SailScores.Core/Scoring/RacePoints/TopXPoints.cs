using System.Collections.Generic;
using System.Linq;
using SailScores.Core.Model;

namespace SailScores.Core.Scoring.RacePoints;

// Top X high point, as VARC uses it: first scores 100, second 99, and so on. With fewer than
// five starters the scores drop by one for each starter short of five. Tied boats get the
// same place (the points for the lowest of them).
public sealed class TopXPoints : IRacePointsRule
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

        return ForPlace(place, starters);
    }

    // Quirk: "starters" here is every score in the series, so the perfect score is 100 in
    // any series with five or more scores in total. Only used for display.
    public decimal? PerfectScore(ScoringWorkspace workspace, IEnumerable<Score> allScores, Score score) =>
        ForPlace(1, allScores.Count());

    public static decimal? ForPlace(int place, int starters) =>
        starters < 5
            ? 101 - (5 - starters) - place
            : 101 - place;
}
