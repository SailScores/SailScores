using System;
using System.Collections.Generic;
using System.Linq;
using SailScores.Core.Model;
using SailScores.Core.Scoring.Formulas;

namespace SailScores.Core.Scoring.RacePoints;

// Appendix A low point: a boat's points are its place (first 1, second 2...), or with
// Appendix A's alternatives, a different value for first. Tied boats share the average of
// the places they cover.
//
// The place is the boat's place in the whole race, or, when places are recalculated within a
// fleet, one more than the boats ahead of it that don't have a code which moves others up.
// Ties: boats with the same place, or a run of boats where each after the first has a TIE
// code.
public sealed class LowPointPlaces : IRacePointsRule
{
    public static readonly LowPointPlaces AppendixA = new(firstPlace: 1m, tiedFirstPlace: 1m);
    // Quirk: low point averages always recalculate places from the boats ahead, ignoring
    // the series' "use full race scores" setting (docs/Scoring-Quirks.md).
    public static readonly LowPointPlaces LowPointAverage =
        new(firstPlace: 1m, tiedFirstPlace: 1m, useWholeRacePlace: false);
    public static readonly LowPointPlaces FirstIsZero = new(firstPlace: 0m, tiedFirstPlace: 0m);
    public static readonly LowPointPlaces FirstIsPoint7 = new(firstPlace: 0.7m, tiedFirstPlace: 0.7m);
    // Quirk (bug): a tie for first counts first place as 0.7, a value copied from
    // First = .7 (docs/Scoring-Quirks.md).
    public static readonly LowPointPlaces FirstIsPoint75 = new(firstPlace: 0.75m, tiedFirstPlace: 0.7m);

    private readonly decimal _firstPlace;
    private readonly decimal _tiedFirstPlace;
    private readonly bool _useWholeRacePlace;

    // firstPlace: the points for first. tiedFirstPlace: what first counts as when it is
    // averaged into a tie. useWholeRacePlace: take the boat's place in the whole race when
    // the workspace says to (no fleet, or full race scores); otherwise always count the boats
    // ahead.
    private LowPointPlaces(decimal firstPlace, decimal tiedFirstPlace, bool useWholeRacePlace = true)
    {
        _firstPlace = firstPlace;
        _tiedFirstPlace = tiedFirstPlace;
        _useWholeRacePlace = useWholeRacePlace;
    }

    public decimal? PerfectScore(ScoringWorkspace workspace, IEnumerable<Score> allScores, Score score) => 1;

    public decimal? Score(ScoringWorkspace workspace, IEnumerable<Score> allScores, Score score)
    {
        if (score == null)
        {
            throw new ArgumentNullException(nameof(score));
        }

        decimal? points;
        if (_useWholeRacePlace && workspace.UseOriginalPlace && score.Place.HasValue && string.IsNullOrEmpty(score.Code))
        {
            points = Convert.ToDecimal(score.Place.Value);
        }
        else
        {
            points = Convert.ToDecimal(allScores.Count(s =>
                score.Place.HasValue
                && s.Race == score.Race
                && s.Place < score.Place
                && !workspace.Host.AdjustsOtherScores(s)) + 1);
        }

        // Checks the points, not the place, so it works when places are recalculated
        // within a fleet.
        if (points == 1)
        {
            points = _firstPlace;
        }

        return TiedScore(workspace.Host, allScores, score) ?? points;
    }

    // The average of the places a tie covers, or null when the boat isn't tied.
    private decimal? TiedScore(IScoringHost host, IEnumerable<Score> allScores, Score score)
    {
        // Other results with the same place are ties.
        int numTied = allScores.Count(s =>
            score.Place.HasValue
            && s.Race == score.Race
            && s.Place == score.Place
            && !host.AdjustsOtherScores(s));

        int tieBase = score.Place ?? 1;
        if (numTied == 1)
        {
            var tmpScore = score;
            bool tie;
            // A TIE code: look back through the boats ahead until one isn't a tie.
            if (TieFormula.IsTie(host.GetScoreCode(score?.Code)))
            {
                do
                {
                    numTied++;
                    var previousScore = PreviousScore(allScores, tmpScore);
                    tieBase = previousScore?.Place ?? tieBase;
                    tie = TieFormula.IsTie(host.GetScoreCode(previousScore?.Code));
                    tmpScore = previousScore;
                } while (tie);
            }

            // And forward through boats behind with a TIE code.
            tmpScore = score;
            do
            {
                var nextScore = NextScore(allScores, tmpScore);
                tie = TieFormula.IsTie(host.GetScoreCode(nextScore?.Code));
                if (tie)
                {
                    numTied++;
                }
                tmpScore = nextScore;
            } while (tie);
        }

        if (numTied > 1)
        {
            decimal total = 0;
            for (int i = 0; i < numTied; i++)
            {
                total += tieBase == 1 && i == 0 ? _tiedFirstPlace : tieBase + i;
            }
            return total / numTied;
        }

        return null;
    }

    private static Score NextScore(IEnumerable<Score> allScores, Score score) =>
        allScores.FirstOrDefault(s => s.Race == score.Race && s.Place == score.Place + 1);

    private static Score PreviousScore(IEnumerable<Score> allScores, Score score) =>
        allScores
            .Where(s => s.Race == score.Race && s.Place < score.Place)
            .OrderByDescending(s => s.Place)
            .FirstOrDefault();
}
