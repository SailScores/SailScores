using Sailscores.Core.Scoring;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sailscores.Core.Model
{
    public class Series
    {
        public Guid Id { get; set; }
        public Guid ClubId { get; set; }

        [Required]
        [StringLength(200)]
        public String Name { get; set; }

        [StringLength(2000)]
        public String Description { get; set; }
        
        public IList<Race> Races { get; set; }

        [Required]
        public Season Season { get; set; }

        [NotMapped]
        public SeriesResults Results { get; set; }

        public IList<Competitor> Competitors { get; set; }

    }
}
