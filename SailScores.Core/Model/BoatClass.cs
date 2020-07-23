using System;
using System.ComponentModel.DataAnnotations;

namespace SailScores.Core.Model
{
    public class BoatClass
    {
        public Guid Id { get; set; }
        public Guid ClubId { get; set; }
        public Club Club { get; set; }

        [Required]
        [StringLength(200)]
        public String Name { get; set; }

        [StringLength(2000)]
        public String Description { get; set; }

    }
}
