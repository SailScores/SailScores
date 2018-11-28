using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Sailscores.Database.Enumerations;

namespace Sailscores.Core.Model.Dto
{
    // Fleet is a group of competitors that may be scored against one another.
    public class FleetDto
    {
        public Guid Id { get; set; }
        public Guid ClubId { get; set; }
        public Club Club { get; set; }
        [StringLength(200)]
        public String Name { get; set; }
        [StringLength(2000)]
        public String Description { get; set; }

        public FleetType FleetType { get; set; }
        public IList<Guid> BoatClassIds {get;set;}
        public IList<Guid> CompetitorIds { get; set; }
    }
}
