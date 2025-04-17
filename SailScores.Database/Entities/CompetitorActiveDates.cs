using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SailScores.Database.Entities
{

#pragma warning disable CA2227 // Collection properties should be read only
    public class CompetitorActiveDates
    {
        public Guid Id { get; set; }

        public DateTime? EarliestDate { get; set; }

        public DateTime? LatestDate { get; set; }
    }

#pragma warning restore CA2227 // Collection properties should be read only
}
