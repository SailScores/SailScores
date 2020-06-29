using System;

namespace SailScores.Database.Entities
{
    public class CompetitorStatsSummary
    {
        public string SeasonName { get; set; }
        public DateTime SeasonStart { get; set; }
        public DateTime SeasonEnd { get; set; }
        public int RaceCount { get; set; }
        public double? AverageFinishRank { get; set; }
        public int? DaysRaced { get; set; }
        public int? BoatsRacedAgainst { get; set; }
        public int? BoatsBeat { get; set; }
    }
}
