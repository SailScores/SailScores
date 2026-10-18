using System;
using System.Collections.Generic;
using SailScores.Core.Scoring;

namespace SailScores.Core.FlatModel
{
    public class FlatSeriesScore
    {
        public Guid CompetitorId { get; set; }
        public int? Rank { get; set; }
        public int? Trend { get; set; }
        public Decimal? TotalScore { get; set; }
        public Decimal? TotalScoreIncludingDiscards { get; set; }
        public Decimal? Average { get; set; }
        public Decimal? PointsEarned { get; set; }
        public Decimal? PointsPossible { get; set; }

        public Decimal? ParticipationPercent { get; set; }
        // Values for FlatResults.Columns, by column key. Competitors without a value are left out.
        public Dictionary<string, ScoringColumnValue> ColumnValues { get; set; }
        public IEnumerable<FlatCalculatedScore> Scores { get; set; }
    }
}
