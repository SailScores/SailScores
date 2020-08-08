using System;

namespace SailScores.Web.Models.SailScores
{
    public class ClubSeasonStatsViewModel
    {
        public string SeasonName { get; set; }
        public string SeasonUrlName { get; set; }
        public DateTime? SeasonStart { get; set; }

        public string ClassName { get; set; }

        public int? RaceCount { get; set; }
        public int? CompetitorsStarted { get; set; }
        public int? DistinctCompetitorsStarted { get; set; }
        public double? AverageCompetitorsPerRace { get; set; }
        public DateTime? LastRace { get; set; }
        public DateTime? FirstRace { get; set; }
    }
}