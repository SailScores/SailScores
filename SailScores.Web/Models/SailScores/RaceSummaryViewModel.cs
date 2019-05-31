using SailScores.Api.Enumerations;
using SailScores.Core.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SailScores.Web.Models.SailScores
{
    public class RaceSummaryViewModel
    {
        public Guid Id { get; set; }

        [StringLength(200)]
        public String Name { get; set; }

        [DisplayFormat(DataFormatString = "{0:MMMM d, yyyy}")]
        public DateTime? Date { get; set; }

        // Typically the order of the race for a given date, but may not be.
        // used for display order after date. 
        public int Order { get; set; }
        [StringLength(1000)]
        public String Description { get; set; }

        public String FleetName { get; set; }
        public String FleetShortName { get; set; }

        public IList<string> SeriesNames { get; set; }

        public Season Season { get; set; }

        public IList<ScoreViewModel> Scores { get; set; }

        public RaceState? State { get; set; }

        public String CalculatedName
        {
            get
            {
                return Date?.ToString("ddd, MMM d") + " Race " + Order;
            }
        }

        public int CompetitorCount
        {
            get
            {
                if(Scores == null)
                {
                    return 0;
                }
                return Scores.Where(s => s.ScoreCode?.CameToStart ??
                    (s.Place.HasValue && s.Place != 0)).Count();
            }
        }
    }
}
