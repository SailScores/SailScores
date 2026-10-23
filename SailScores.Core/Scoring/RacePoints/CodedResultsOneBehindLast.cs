using System;
using System.Linq;
using SailScores.Core.Model;
using SailScores.Core.Scoring.Formulas;

namespace SailScores.Core.Scoring.RacePoints;

// Cox-Sprague, Top X and VARC 2026: replaces the race step of score code formulas. Every
// coded result that came to the start, except TIE, scores the points for one place behind
// the last starter among the series' boats. VARC passes its own count of boats (every boat
// racing, DNS included) in place of the starters, and keeps the points entered for a MAN
// code (RDG); a MAN code with none entered scores one behind like the others.
//
// Quirk: so penalties that keep the finish (ZFP, SCP) don't use the place or a penalty, and
// no race formula (PLC% included) is evaluated (docs/Scoring-Quirks.md).
public sealed class CodedResultsOneBehindLast : ICodedResultsRule
{
    private readonly Func<int, int, decimal?> _pointsForPlace;
    private readonly Func<IScoringHost, Score, bool> _counts;
    private readonly bool _keepManualPoints;

    // pointsForPlace(place, starters): the system's points table. counts: the boats counted
    // as starters (default: those that started). keepManualPoints: a MAN code with points
    // entered scores those points.
    public CodedResultsOneBehindLast(
        Func<int, int, decimal?> pointsForPlace,
        Func<IScoringHost, Score, bool> counts = null,
        bool keepManualPoints = false)
    {
        _pointsForPlace = pointsForPlace;
        _counts = counts ?? ((host, s) => host.CountsAsStarted(s));
        _keepManualPoints = keepManualPoints;
    }

    public void Apply(ScoringWorkspace workspace, SeriesResults results, SeriesCompetitorResults competitor)
    {
        var host = workspace.Host;
        foreach (var race in results.SailedRaces)
        {
            var score = competitor.CalculatedScores[race];
            var scoreCode = host.GetScoreCode(score.RawScore);
            if (_keepManualPoints && ManualFormula.IsManual(scoreCode) && score.RawScore.CodePoints.HasValue)
            {
                score.ScoreValue = score.RawScore.CodePoints;
            }
            else if (scoreCode != null && host.CameToStart(score.RawScore) && !TieFormula.IsTie(scoreCode))
            {
                var starters = workspace.ScoresIn(race)
                    .Where(s => results.Competitors.Any(c => c.Id == s.CompetitorId))
                    .Count(s => _counts(host, s));
                score.ScoreValue = _pointsForPlace(starters + 1, starters);
            }
        }
    }
}
