using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SailScores.Web.Models.SailScores
{

    public class ClubStatsViewModel
    {
        public Guid Id { get; set; }

        public bool CanEdit { get; set; }

        [StringLength(200)]
        public String Name { get; set; }
        [StringLength(10)]
        public String Initials { get; set; }

        public String StatisticsDescription { get; set; }

        public IEnumerable<ClubSeasonStatsViewModel> SeasonStats { get; set; }

    }
}
