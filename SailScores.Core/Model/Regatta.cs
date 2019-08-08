using SailScores.Core.Scoring;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SailScores.Core.Model
{
    public class Regatta
    {
        public Guid Id { get; set; }
        public Guid ClubId { get; set; }

        [Required]
        [StringLength(200)]
        public String Name { get; set; }

        [StringLength(2000)]
        public String Description { get; set; }

        public IList<Series> Series { get; set; }

        [Required]
        public Season Season { get; set; }

        public IList<Fleet> Fleets { get; set; }
        
        public DateTime? UpdatedDate { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public Guid? ScoringSystemId { get; set; }
        public ScoringSystem ScoringSystem { get; set; }

    }
}
