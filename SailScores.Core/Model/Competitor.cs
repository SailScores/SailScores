using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SailScores.Core.Model
{
    public class Competitor
    {
        public Guid Id { get; set; }
        public Guid ClubId { get; set; }
        public Club Club { get; set; }
        [StringLength(200)]
        public String Name { get; set; }
        [StringLength(20)]
        public String SailNumber { get; set; }
        [StringLength(20)]
        public String AlternativeSailNumber { get; set; }
        [StringLength(200)]
        public String BoatName { get; set; }
        [StringLength(2000)]
        public String Notes { get; set; }

        public Guid BoatClassId { get; set; }
        public BoatClass BoatClass { get; set; }
        public IList<Fleet> Fleets { get; set; }
        public IList<Score> Scores { get; set; }

        public override string ToString()
        {
            return BoatName + " : " + Name + " : " + SailNumber + " : " + Id;
        }

    }
}
