using System;
using System.Collections.Generic;
using System.Linq;

namespace SailScores.Core.Scoring
{
    internal class CoxSpragueCompComparer : IComparer<SeriesCompetitorResults>
    {
        public int Compare(SeriesCompetitorResults x, SeriesCompetitorResults y)
        {
            int totalComparison = CompareTotals(x, y);

            if (totalComparison != 0)
            {
                return totalComparison;
            }

            var participationComparison = CompareParticipation(x, y);
            if (participationComparison != 0)
            {
                return participationComparison;
            }
            var tiebreakerOne = WhichHasFewerOfPlace(x, y);
            if (tiebreakerOne != 0)
            {
                return tiebreakerOne;
            }

            // still tied, take the last race where the value wasn't the same for both

            return WhichWonLatest(x, y);

        }

        private static int CompareParticipation(SeriesCompetitorResults x, SeriesCompetitorResults y)
        {
            var xParticipation = x.CalculatedScores.Count(s =>
                (s.Value.RawScore?.Place.HasValue ?? false)
                || (s.Value.CountsAsParticipation ?? false));
            var yParticipation = y.CalculatedScores.Count(s =>
                (s.Value.RawScore?.Place.HasValue ?? false)
                || (s.Value.CountsAsParticipation ?? false));
            return xParticipation.CompareTo(yParticipation);
        }

        private static int CompareTotals(SeriesCompetitorResults x, SeriesCompetitorResults y)
        {
            // if total is null, drop to bottom.
            var xScoreToUse = x.TotalScore ?? -1;
            var yScoreToUse = y.TotalScore ?? -1;

            return yScoreToUse.CompareTo(xScoreToUse);
        }

        private static int WhichHasFewerOfPlace(SeriesCompetitorResults x, SeriesCompetitorResults y)
        {

            // Return the score with the most firsts, then seconds, etc.
            // must not include discarded results.
            var xScoresLowToHigh =
                x.CalculatedScores.Values
                    .Where(s => !s.Discard)
                    .Select(s => s.ScoreValue ?? decimal.MaxValue)
                    .OrderByDescending(s => s).ToArray();
            var yScoresLowToHigh =
                y.CalculatedScores.Values
                    .Where(s => !s.Discard)
                    .Select(s => s.ScoreValue ?? decimal.MaxValue)
                    .OrderByDescending(s => s).ToArray();

            for (int i = 0; i < xScoresLowToHigh.Length; i++)
            {
                if (yScoresLowToHigh.Length < i)
                {
                    // x had more scores, so it wins tiebreaker. (all earlier scores were ties.)
                    return -1;
                }

                if (xScoresLowToHigh[i] != yScoresLowToHigh[i])
                {
                    return yScoresLowToHigh[i].CompareTo(xScoresLowToHigh[i]);
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
                        xScore > yScore
                            ? -1
                            : 1;
                }
            }

            return 0;
        }

    }
}