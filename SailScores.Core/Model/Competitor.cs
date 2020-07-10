using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SailScores.Core.Model
{
    public class Competitor
    {
        public Guid Id { get; set; }
        public Guid ClubId { get; set; }

        [Required]
        [StringLength(200)]
        public String Name { get; set; }

        [Display(Name = "Sail Number")]
        [StringLength(20)]
        public String SailNumber { get; set; }

        [Display(Name = "Alternative Sail Number")]
        [StringLength(20)]
        public String AlternativeSailNumber { get; set; }

        [Display(Name = "Boat Name")]
        [StringLength(200)]
        public String BoatName { get; set; }

        [Display(Name = "Home Club")]
        [StringLength(200)]
        public String HomeClubName { get; set; }

        [Display(Name = "Active?")]
        public bool IsActive { get; set; }

        [StringLength(2000)]
        public String Notes { get; set; }

        public Guid BoatClassId { get; set; }
        public BoatClass BoatClass { get; set; }
        public IList<Fleet> Fleets { get; set; }

        public override string ToString()
        {
            return BoatName + " : " + Name + " : " + SailNumber + " : " + Id;
        }

    }
}
