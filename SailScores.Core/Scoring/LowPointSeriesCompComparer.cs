using System.Collections.Generic;
using System.Linq;

namespace SailScores.Core.Scoring
{
    internal class LowPointSeriesCompComparer : IComparer<SeriesCompetitorResults>
    {
        public int Compare(SeriesCompetitorResults x, SeriesCompetitorResults y)
        {
            //put zeroes at the end of rankings 
            decimal? xScoreToUse =
                (x.TotalScore.HasValue && x.TotalScore == 0
                ? decimal.MaxValue
                : x.TotalScore);
            decimal? yScoreToUse =
                (y.TotalScore.HasValue && y.TotalScore == 0
                ? decimal.MaxValue
                : y.TotalScore);


            if (xScoreToUse != yScoreToUse)
            {
                return xScoreToUse < yScoreToUse ? -1 : 1;
            }

            // tied total, so return the score with the most firsts, then seconds, etc.
            // must not include discarded results.
            var xScoresLowToHigh =
                    x.CalculatedScores.Values
                    .Where(s => !s.Discard)
                    .Select(s => s.ScoreValue ?? decimal.MaxValue)
                    .OrderBy(s => s).ToArray();
            var yScoresLowToHigh =
                    y.CalculatedScores.Values
                    .Where(s => !s.Discard)
                    .Select(s => s.ScoreValue ?? decimal.MaxValue)
                    .OrderBy(s => s).ToArray();


            for (int i = 0; i < xScoresLowToHigh.Length; i++)
            {
                if (yScoresLowToHigh.Length < i)
                {
                    // x had more scores, so it wins tiebreaker. (all earlier scores were ties.)
                    return -1;
                }
                if( xScoresLowToHigh[i] !=
                    yScoresLowToHigh[i])
                {
                    return xScoresLowToHigh[i] <
                    yScoresLowToHigh[i] ? -1 : 1;
                }
            }
            if(xScoresLowToHigh.Length < yScoresLowToHigh.Length)
            {
                // y had more scores, but scores they both had were ties, so y wins tiebreaker.
                return 1;
            }

            // still tied, take the last race where the value wasn't the same for both
            for(int i = 0; i < x.CalculatedScores.Count; i++)
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