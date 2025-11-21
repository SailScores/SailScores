using SailScores.Core.Model;
using System;
using System.Collections.Generic;

namespace SailScores.Core.Scoring
{
    // The results for one competitor in a series
    public class SeriesCompetitorResults
    {
        public Competitor Competitor { get; set; }

        public Dictionary<Race, CalculatedScore> CalculatedScores { get; set; }
        public int? Rank { get; set; }
        public int? Trend { get; set; }
        public Decimal? TotalScore { get; set; }
        public Decimal? ParticipationPercent { get; set; }

        public Decimal? PointsEarned { get; set; }
        public Decimal? PointsPossible { get; set; }

        // In Average based scoring systems, this will be the same as TotalScore
        // unless a competitor isn't ranked (didn't meet participation requirements)
        // then this may have a value and TotalScore will be null.
        public Decimal? Average { get; set; }
    }
}
