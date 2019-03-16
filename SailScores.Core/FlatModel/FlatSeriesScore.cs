using System;
using System.Collections.Generic;
using System.Text;

namespace SailScores.Core.FlatModel
{
    public class FlatSeriesScore
    {
        public Guid CompetitorId { get; set; }
        public int? Rank { get; set; }
        public Decimal? TotalScore { get; set; }
        public IEnumerable<FlatCalculatedScore> Scores { get; set; }
    }
}