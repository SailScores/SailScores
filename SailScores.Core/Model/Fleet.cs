using Sailscores.Database.Enumerations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Sailscores.Core.Model
{
    // Fleet is a group of competitors that may be scored against one another.
    public class Fleet
    {
        public Guid Id { get; set; }
        public Guid ClubId { get; set; }
        public Club Club { get; set; }
        [StringLength(200)]
        public String Name { get; set; }
        [StringLength(2000)]
        public String Description { get; set; }

        public FleetType FleetType { get; set; }
        public IList<BoatClass> BoatClasses {get;set;}
        public IList<Competitor> Competitors { get; set; }
    }
}
