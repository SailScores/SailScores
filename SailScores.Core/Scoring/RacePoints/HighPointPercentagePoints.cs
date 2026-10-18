using System;
using System.Collections.Generic;
using System.Linq;
using SailScores.Core.Model;

namespace SailScores.Core.Scoring.RacePoints;

// High point percentage: a boat scores one point for each boat that came to the start and
// finished behind it (the boats that came to the start less the boats ahead). Tied boats
// share the average. A win scores the number of boats that came to the start.
public sealed class HighPointPercentagePoints : IRacePointsRule
{
    public decimal? Score(ScoringWorkspace workspace, IEnumerable<Score> allScores, Score score)
    {
        var host = workspace.Host;
        // The whole race, or (places recalculated within a fleet) the scores being scored.
        var raceScores = workspace.UseOriginalPlace && score.Place.HasValue && string.IsNullOrEmpty(score.Code)
            ? workspace.ScoresIn(score.Race)
            : allScores.Where(s => s.Race == score.Race);

        int competitorsInRace = raceScores.Count(host.CameToStart);
        decimal? boatsAhead = Convert.ToDecimal(raceScores.Count(s =>
            score.Place.HasValue
            && s.Place < score.Place
            && !host.AdjustsOtherScores(s)));

        int numTied = raceScores.Count(s =>
            score.Place.HasValue
            && s.Place == score.Place
            && !host.AdjustsOtherScores(s));
        if (numTied > 1)
        {
            int total = 0;
            for (int i = 0; i < numTied; i++)
            {
                total += (int)score.Place + i;
            }
            boatsAhead = ((decimal)total / numTied) - 1;
        }

        return competitorsInRace - boatsAhead;
    }

    public decimal? PerfectScore(ScoringWorkspace workspace, IEnumerable<Score> allScores, Score score) =>
        allScores.Count(s => s.Race == score.Race && workspace.Host.CameToStart(s));
}
