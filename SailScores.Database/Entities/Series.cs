using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Sailscores.Database.Entities
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
        
        public IList<SeriesRaces> RaceSeries { get; set; }

        [Required]
        public Season Season { get; set; }
    }
}
