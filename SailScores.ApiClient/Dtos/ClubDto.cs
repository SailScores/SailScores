using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SailScores.Api.Dtos
{
    public class ClubDto
    {
        public Guid Id { get; set; }

        [Required]
        [StringLength(200)]
        public String Name { get; set; }
        [StringLength(10)]
        public String Initials { get; set; }
        public String Description { get; set; }
        public bool IsHidden { get; set; }


        public IList<Guid> FleetIds { get; set; }
        public IList<Guid> CompetitorIds { get; set; }
        public IList<Guid> BoatClassIds { get; set; }
        public IList<Guid> SeasonIds { get; set; }
        public IList<Guid> SeriesIds { get; set; }
        public IList<Guid> RaceIds { get; set; }
        public IList<Guid> ScoreCodeIds {get; set;}
        
    }
}
