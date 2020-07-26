using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using SailScores.Api.Enumerations;

namespace SailScores.Api.Dtos
{
    // Fleet is a group of competitors that may be scored against one another.
    public class FleetDto
    {
        public Guid Id { get; set; }
        public Guid ClubId { get; set; }
        [StringLength(30)]
        public String ShortName { get; set; }
        [StringLength(200)]
        public String Name { get; set; }
        [StringLength(2000)]
        public String Description { get; set; }

        public FleetType FleetType { get; set; }
        public IList<Guid> BoatClassIds { get; set; }
        public IList<Guid> CompetitorIds { get; set; }
    }
}
