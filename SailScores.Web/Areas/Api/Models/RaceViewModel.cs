using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SailScores.Web.Areas.Api.Models
{
    public class RaceViewModel
    {
        public Guid Id { get; set; }
        [Required]
        [StringLength(200)]
        public String Name { get; set; }


        //public IList<Fleet> Fleets { get; set; }
        //public IList<Competitor> Competitors { get; set; }
        //public IList<BoatClass> BoatClasses { get; set; }
        //public IList<Season> Seasons { get; set; }
        //public IList<Series> Series { get; set; }
        //public IList<Race> Races { get; set; }
        //public IList<ScoreCode> ScoreCodes { get; set; }
    }
}
