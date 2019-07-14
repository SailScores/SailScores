using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SailScores.Database.Entities
{
    public class Season
    {
        public Guid Id { get; set; }
        public Guid ClubId { get; set; }

        [StringLength(200)]
        public String Name { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        
        public IEnumerable<Series> Series { get; set; }
    }
}
