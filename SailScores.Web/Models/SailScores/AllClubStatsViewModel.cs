using System;

namespace SailScores.Web.Models.SailScores
{
    public class AllClubStatsViewModel
    {
        public string ClubName { get; set; }
        public string ClubInitials { get; set; }
        public DateTime? LastRaceDate { get; set; }

        public DateTime? LastRaceUpdate { get; set; }

        public int? RaceCount { get; set; }
        public int? ScoreCount { get; set; }
    }
}