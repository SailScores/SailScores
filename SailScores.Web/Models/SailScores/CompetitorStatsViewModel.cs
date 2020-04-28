using Microsoft.AspNetCore.Mvc.Rendering;
using SailScores.Core.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SailScores.Web.Models.SailScores
{
    public class CompetitorStatsViewModel
    {
        public Guid Id { get; set; }

        [StringLength(200)]
        public String Name { get; set; }

        [Display(Name = "Sail Number")]
        [StringLength(20)]
        public String SailNumber { get; set; }
        
        [Display(Name = "Boat Name")]
        [StringLength(200)]
        public String BoatName { get; set; }

        [Display(Name = "Home Club Name")]
        [StringLength(200)]
        public String HomeClubName { get; set; }


        public override string ToString()
        {
            return BoatName + " : " + Name + " : " + SailNumber + " : " + Id;
        }

    }
}
