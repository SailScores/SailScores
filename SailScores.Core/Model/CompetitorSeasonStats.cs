using SailScores.Database.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace SailScores.Core.Model
{
    public class CompetitorSeasonStats
    {
        [Display(Name = "Season")]
        public String SeasonName { get; set; }

        public String SeasonUrlName { get; set; }

        [Display(Name = "Season Start")]
        public DateTime SeasonStart { get; internal set; }
        [Display(Name = "Season End")]
        public DateTime SeasonEnd { get; internal set; }
        [Display(Name = "Number of Races")]
        public int RaceCount { get; internal set; }
        [Display(Name = "Average Finish Place")]
        public double? AverageFinishPlace { get; internal set; }
        [Display(Name = "Days Raced")]
        public int? DaysRaced { get; internal set; }
        [Display(Name = "Boats Raced Against")]
        public int? BoatsRacedAgainst { get; internal set; }
        [Display(Name = "Boats Beat")]
        public int? BoatsBeat { get; internal set; }
    }
}
