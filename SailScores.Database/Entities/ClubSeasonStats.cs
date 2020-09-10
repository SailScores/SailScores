using System;

namespace SailScores.Database.Entities
{
    public class ClubSeasonStats
    {
        public string ClubName { get; set; }
        public string ClubInitials { get; set; }
        public string SeasonName { get; set; }
        public string SeasonUrlName { get; set; }
        public DateTime? SeasonStart { get; set; }

        public string ClassName { get; set; }

        public int? RaceCount { get; set; }
        public int? CompetitorsStarted { get; set; }
        public int? DistinctCompetitorsStarted { get; set; }
        public decimal? AverageCompetitorsPerRace { get; set; }
        public int? DistinctDaysRaced { get; set; }
        public DateTime? LastRace { get; set; }
        public DateTime? FirstRace { get; set; }
    }
}
