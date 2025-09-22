
using System.Collections.Generic;
using System.Linq;

namespace SailScores.Core.Scoring;

//
// When there is a tie in total points of an individual discipline the tie shall be broken…
// (A)  in favor of the competitor who has beaten the other more times.Award each competitor 1, 2, 3
//      etc.respectively, in each race/elimination and include the discarded scores.The lowest
//      accumulated score wins.
// (B)  in favor of the competitor who has the greater number of firsts, seconds and so on, discard
//      score included.Here the actual finishing positions of the competitors shall be
//      considered.
// (C)  in favor of the competitor who has discarded a better result.
// (D)  a sail-off between the competitors in question may be permitted if reasonably possible.
// (E)  If after all relevant tie break rules have been applied, there is a tie between competitors
//      ranked in the top 3 of an individual discipline at an event, the tie shall be broken in
//      favor of the competitor who had the highest finishing position in the last race or
//      elimination of that specific discipline at the event

// Notes:
// B is similar to Appendix A, but discadrd scores are included.
// D cannot be implemented here.
// E is the same as one of appendix A's tiebreakers.


internal class PwaSeriesCompComparer : IComparer<SeriesCompetitorResults>
{
    public int Compare(SeriesCompetitorResults x, SeriesCompetitorResults y)
    {
        int totalComparison = CompareTotals(x, y);

        if (totalComparison != 0)
        {
            return totalComparison;
        }

        var tiebreakerA = WhichBeatTheOtherMost(x, y);
        if (tiebreakerA != 0)
        {
            return tiebreakerA;
        }

        var tiebreakerB = WhichHasFewerOfPlaceIncludingDiscards(x, y);
        if (tiebreakerB != 0)
        {
            return tiebreakerB;
        }

        var tiebreakerC = WhichHasBetterDiscardedResult(x, y);
        if (tiebreakerC != 0)
        {
            return tiebreakerC;
        }

        var tiebreakerE = WhichWonLatest(x, y);
        return tiebreakerE;
        
    }

    private static int CompareTotals(SeriesCompetitorResults x, SeriesCompetitorResults y)
    {

        //put zeros at the end of rankings 
        decimal xScoreToUse = x.TotalScore ?? decimal.MaxValue;
        xScoreToUse = xScoreToUse == 0 ? decimal.MaxValue : xScoreToUse;

        decimal yScoreToUse = y.TotalScore ?? decimal.MaxValue;
        yScoreToUse = yScoreToUse == 0 ? decimal.MaxValue : yScoreToUse;

        return xScoreToUse.CompareTo(yScoreToUse);

    }

    private static int WhichBeatTheOtherMost(SeriesCompetitorResults x, SeriesCompetitorResults y)
    {
        // see notes above for method (A)
        int xBeatsY = 0;
        int yBeatsX = 0;

        var raceIds = x.CalculatedScores.Keys
            .Select(r => r.Id)
            .Union(y.CalculatedScores.Keys.Select(r => r.Id))
            .Distinct();
        // if there is a race that one boat participated in and the other didn't,
        // the one that participated gets the win.
        foreach (var raceId in raceIds)
        {
            var xScore = x.CalculatedScores
                .Where(s => s.Key.Id == raceId)
                .Select(s => s.Value.ScoreValue ?? decimal.MaxValue)
                .FirstOrDefault();
            var yScore = y.CalculatedScores
                .Where(s => s.Key.Id == raceId)
                .Select(s => s.Value.ScoreValue ?? decimal.MaxValue)
                .FirstOrDefault();

            if (xScore < yScore)
            {
                xBeatsY++;
            }
            else if (yScore < xScore)
            {
                yBeatsX++;
            }
        }
        if (xBeatsY > yBeatsX)
        {
            return -1;
        }
        else if (yBeatsX > xBeatsY)
        {
            return 1;
        }
        return 0;
    }

    private static int WhichHasFewerOfPlaceIncludingDiscards(SeriesCompetitorResults x, SeriesCompetitorResults y)
    {
        // see notes above for method (B)
        var xScoresLowToHigh =
            x.CalculatedScores.Values
                .Select(s => s.ScoreValue ?? decimal.MaxValue)
                .OrderBy(s => s).ToArray();
        var yScoresLowToHigh =
            y.CalculatedScores.Values
                .Select(s => s.ScoreValue ?? decimal.MaxValue)
                .OrderBy(s => s).ToArray();


        for (int i = 0; i < xScoresLowToHigh.Length; i++)
        {
            if (yScoresLowToHigh.Length < i)
            {
                // x had more scores, so it wins tiebreaker. (all earlier scores were ties.)
                return -1;
            }
            if (xScoresLowToHigh[i] !=
                yScoresLowToHigh[i])
            {
                return xScoresLowToHigh[i] <
                       yScoresLowToHigh[i] ? -1 : 1;
            }
        }
        if (xScoresLowToHigh.Length < yScoresLowToHigh.Length)
        {
            // y had more scores, but scores they both had were ties, so y wins tiebreaker.
            return 1;
        }

        return 0;
    }

    private static int WhichHasBetterDiscardedResult(SeriesCompetitorResults x, SeriesCompetitorResults y)
    {
        // see notes above for method (C)
        var xDiscardedScores =
            x.CalculatedScores.Values
                .Where(s => s.Discard)
                .Select(s => s.ScoreValue ?? decimal.MaxValue)
                .OrderBy(s => s).ToArray();
        var yDiscardedScores =
            x.CalculatedScores.Values
                .Where(s => s.Discard)
                .Select(s => s.ScoreValue ?? decimal.MaxValue)
                .OrderBy(s => s).ToArray();
        int minLength = 
            xDiscardedScores.Length < yDiscardedScores.Length
                ? xDiscardedScores.Length
                : yDiscardedScores.Length;
        for (int i = 0; i < minLength; i++)
        {
            if(xDiscardedScores[i] != yDiscardedScores[i])
            {
                return xDiscardedScores[i] < yDiscardedScores[i] ? -1 : 1;
            }
        }
        if (xDiscardedScores.Length != yDiscardedScores.Length)
        {
            // all the discarded scores they both had were ties, so the one with more discarded scores wins.
            return xDiscardedScores.Length > yDiscardedScores.Length ? -1 : 1;
        }
        return 0;
    }

    private static int WhichWonLatest(SeriesCompetitorResults x, SeriesCompetitorResults y)
    {
        // take the last race where the value wasn't the same for both
        for (int i = 0; i < x.CalculatedScores.Count; i++)
        {
            var xScore = x.CalculatedScores
                .OrderByDescending(s => s.Key.Date)
                .ThenByDescending(s => s.Key.Order)
                .Skip(i).First()
                .Value.ScoreValue ?? decimal.MaxValue;
            var yScore = y.CalculatedScores
                .OrderByDescending(s => s.Key.Date)
                .ThenByDescending(s => s.Key.Order)
                .Skip(i).First()
                .Value.ScoreValue ?? decimal.MaxValue;
            if (xScore != yScore)
            {
                return
                    xScore < yScore
                        ? -1
                        : 1;
            }
        }

        // wow, really tied.
        return 0;
    }

}