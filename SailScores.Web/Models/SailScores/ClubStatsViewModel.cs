using SailScores.Core.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using SailScores.Api.Enumerations;

namespace SailScores.Web.Models.SailScores
{

#pragma warning disable CA2227 // Collection properties should be read only
    public class ClubStatsViewModel
    {
        public Guid Id { get; set; }

        public bool CanEdit { get; set; }

        [Required]
        [StringLength(200)]
        public String Name { get; set; }
        [StringLength(10)]
        public String Initials { get; set; }

        public IEnumerable<ClubSeasonStatsViewModel> SeasonStats { get; set; }

    }
#pragma warning restore CA2227 // Collection properties should be read only
}
