using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SailScores.Database.Entities
{
    public class Regatta
    {
        public Guid Id { get; set; }
        public Guid ClubId { get; set; }

        [Required]
        [StringLength(200)]
        public String Name { get; set; }

        [StringLength(200)]
        public String UrlName { get; set; }
        public String Description { get; set; }

        [StringLength(1000)]
        public String Url { get; set; }

        public IList<RegattaSeries> RegattaSeries { get; set; }

        public IList<RegattaFleet> RegattaFleet { get; set; } 
 
        [Required]
        public Season Season { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        [Column("UpdatedDateUtc")]
        public DateTime? UpdatedDate { get; set; }

        public Guid? ScoringSystemId { get; set; }
        public ScoringSystem ScoringSystem { get; set; }
    }
}
