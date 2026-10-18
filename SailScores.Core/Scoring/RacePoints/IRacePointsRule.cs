using System.Collections.Generic;
using SailScores.Core.Model;

namespace SailScores.Core.Scoring.RacePoints;

// Points for a finish, before score code formulas: Appendix A places, high point tables...
public interface IRacePointsRule
{
    // The points for score in its race. allScores holds every score being scored (all races).
    decimal? Score(ScoringWorkspace workspace, IEnumerable<Score> allScores, Score score);

    // The points a win would have scored in the race (percentage systems divide by it).
    decimal? PerfectScore(ScoringWorkspace workspace, IEnumerable<Score> allScores, Score score);
}
