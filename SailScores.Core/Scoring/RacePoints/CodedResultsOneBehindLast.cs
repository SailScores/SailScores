using System;
using System.Linq;
using SailScores.Core.Scoring.Formulas;

namespace SailScores.Core.Scoring.RacePoints;

// Cox-Sprague and Top X: replaces the race step of score code formulas. Every coded result
// that came to the start, except TIE, scores the points for one place behind the last
// starter among the series' boats.
//
// Quirk: so penalties that keep the finish (ZFP, SCP) don't use the place or a penalty, and
// no race formula (PLC% included) is evaluated (docs/Scoring-Quirks.md).
public sealed class CodedResultsOneBehindLast : ICodedResultsRule
{
    private readonly Func<int, int, decimal?> _pointsForPlace;

    // pointsForPlace(place, starters): the system's points table.
    public CodedResultsOneBehindLast(Func<int, int, decimal?> pointsForPlace)
    {
        _pointsForPlace = pointsForPlace;
    }

    public void Apply(ScoringWorkspace workspace, SeriesResults results, SeriesCompetitorResults competitor)
    {
        var host = workspace.Host;
        foreach (var race in results.SailedRaces)
        {
            var score = competitor.CalculatedScores[race];
            var scoreCode = host.GetScoreCode(score.RawScore);
            if (scoreCode != null && host.CameToStart(score.RawScore) && !TieFormula.IsTie(scoreCode))
            {
                var starters = workspace.ScoresIn(race)
                    .Where(s => results.Competitors.Any(c => c.Id == s.CompetitorId))
                    .Count(host.CountsAsStarted);
                score.ScoreValue = _pointsForPlace(starters + 1, starters);
            }
        }
    }
}
