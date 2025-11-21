using System.Collections.Generic;
using System.Linq;

namespace SailScores.Core.Scoring
{
    internal class LowPointSeriesCompComparer : IComparer<SeriesCompetitorResults>
    {
        public int Compare(SeriesCompetitorResults x, SeriesCompetitorResults y)
        {
            int totalComparison = CompareTotals(x, y);

            if (totalComparison != 0)
            {
                return totalComparison;
            }

            var tiebreakerOne = WhichHasFewerOfPlace(x, y);
            if (tiebreakerOne != 0)
            {
                return tiebreakerOne;
            }

            return WhichWonLatest(x, y);
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

        private static int WhichHasFewerOfPlace(SeriesCompetitorResults x, SeriesCompetitorResults y)
        {

            // return the score with the most firsts, then seconds, etc.
            // must not include discarded results.
            var xScoresLowToHigh =
                x.CalculatedScores.Values
                    .Where(s => !s.Discard && (s.ScoreValue ?? 0.0m) != 0.0m)
                    .Select(s => s.ScoreValue ?? decimal.MaxValue)
                    .OrderBy(s => s).ToArray();
            var yScoresLowToHigh =
                y.CalculatedScores.Values
                    .Where(s => !s.Discard && (s.ScoreValue ?? 0.0m) != 0.0m)
                    .Select(s => s.ScoreValue ?? decimal.MaxValue)
                    .OrderBy(s => s).ToArray();


            for (int i = 0; i < xScoresLowToHigh.Length; i++)
            {
                if (yScoresLowToHigh.Length <= i)
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
}
