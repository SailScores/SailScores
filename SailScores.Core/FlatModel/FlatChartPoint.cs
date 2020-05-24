using System;

namespace SailScores.Core.FlatModel
{
    public class FlatChartPoint
    {
        public Guid RaceId { get; set; }
        public Guid CompetitorId { get; set; }

        public Decimal? RacePlace { get; set; }
        public int? SeriesRank { get; set; }
        public Decimal? SeriesPoints { get; set; }

    }
}
