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
        public Decimal? TotalScore { get; set; }
    }
}
