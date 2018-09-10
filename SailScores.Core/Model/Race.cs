using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SailScores.Core.Model
{
    public class Race
    {
        public Guid Id { get; set; }
        
        public Guid ClubId { get; set; }
        public Club Club { get; set; }
        [StringLength(200)]
        public String Name { get; set; }

        public DateTime? Date { get; set; }

        // Typically the order of the race for a given date, but may not be.
        // used for display order after date. 
        public int Order { get; set; }
        [StringLength(1000)]
        public String Description { get; set; }

        public Fleet Fleet { get; set; }
        public IList<Score> Scores { get; set; }
        
        public IList<Series> Series { get; set; }
    }
}
