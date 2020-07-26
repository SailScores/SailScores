using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SailScores.Core.Model
{
    public class Regatta
    {
        public Guid Id { get; set; }
        public Guid ClubId { get; set; }

        [Required]
        [StringLength(200)]
        public String Name { get; set; }

        public String UrlName { get; set; }

        [StringLength(2000)]
        public String Description { get; set; }

        // Url of external regatta site
        public String Url { get; set; }

        public IList<Series> Series { get; set; }

        public Season Season { get; set; }

        public IList<Fleet> Fleets { get; set; }

        public DateTime? UpdatedDate { get; set; }

        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:MMMM d, yyyy}")]
        public DateTime? StartDate { get; set; }

        [Display(Name = "End Date")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:MMMM d, yyyy}")]
        public DateTime? EndDate { get; set; }

        public Guid? ScoringSystemId { get; set; }
        public ScoringSystem ScoringSystem { get; set; }

        public bool PreferAlternateSailNumbers { get; set; }
    }
}
