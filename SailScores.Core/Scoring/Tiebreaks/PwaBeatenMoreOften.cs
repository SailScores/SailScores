using System.Linq;

namespace SailScores.Core.Scoring.Tiebreaks;

// PWA tiebreak (A): the boat that beat the other in more races ranks ahead, discards
// included. A score with no value counts as the worst; a race a boat has no score for at
// all counts as 0 for it (every boat has a score in every race, so this doesn't arise).
public sealed class PwaBeatenMoreOften : ITieBreak
{
    public int Compare(ScoringWorkspace workspace, SeriesCompetitorResults x, SeriesCompetitorResults y)
    {
        int xBeatsY = 0;
        int yBeatsX = 0;

        var raceIds = x.CalculatedScores.Keys.Select(r => r.Id)
            .Union(y.CalculatedScores.Keys.Select(r => r.Id))
            .Distinct();
        foreach (var raceId in raceIds)
        {
            var xScore = ScoreIn(x, raceId);
            var yScore = ScoreIn(y, raceId);
            if (xScore < yScore)
            {
                xBeatsY++;
            }
            else if (yScore < xScore)
            {
                yBeatsX++;
            }
        }
        return xBeatsY > yBeatsX ? -1 : yBeatsX > xBeatsY ? 1 : 0;
    }

    private static decimal ScoreIn(SeriesCompetitorResults results, System.Guid raceId) =>
        results.CalculatedScores
            .Where(s => s.Key.Id == raceId)
            .Select(s => s.Value.ScoreValue ?? decimal.MaxValue)
            .FirstOrDefault();
}
