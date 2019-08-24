using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SailScores.Api.Dtos
{
    public class CompetitorDto
    {
        public Guid Id { get; set; }
        public Guid ClubId { get; set; }

        [StringLength(200)]
        public String Name { get; set; }
        [StringLength(20)]
        public String SailNumber { get; set; }
        [StringLength(20)]
        public String AlternativeSailNumber { get; set; }
        [StringLength(200)]
        public String BoatName { get; set; }
        [StringLength(200)]
        public String HomeClubName { get; set; }
        [StringLength(2000)]
        public String Notes { get; set; }
        public bool IsActive { get; set; }

        public Guid BoatClassId { get; set; }

        public IList<Guid> FleetIds { get; set; }
        public IList<Guid> ScoreIds { get; set; }

        public override string ToString()
        {
            return BoatName + " : " + Name + " : " + SailNumber + " : " + Id;
        }

    }
}
