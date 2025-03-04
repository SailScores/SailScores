using System;
using System.ComponentModel.DataAnnotations;

namespace SailScores.Core.Model
{
    public class CompetitorSeasonStats
    {
        [Display(Name = "Season")]
        public String SeasonName { get; set; }

#pragma warning disable CA1056 // Uri properties should not be strings
        public String SeasonUrlName { get; set; }
#pragma warning restore CA1056 // Uri properties should not be strings

        [Display(Name = "Season Start")]
        public DateTime SeasonStart { get; internal set; }
        [Display(Name = "Season End")]
        public DateTime SeasonEnd { get; internal set; }
        [Display(Name = "Number of Races")]
        public int RaceCount { get; internal set; }
        [Display(Name = "Average Finish Place")]
        public double? AverageFinishRank { get; internal set; }
        [Display(Name = "Days Raced")]
        public int? DaysRaced { get; internal set; }
        [Display(Name = "Boats Raced Against")]
        public int? BoatsRacedAgainst { get; internal set; }
        [Display(Name = "Boats Beat")]
        public int? BoatsBeat { get; internal set; }
        [Display(Name = "Latest Date Raced")]
        [DataType(DataType.Date)]
        public DateTime? LastRacedDate { get; internal set; }
    }
}
