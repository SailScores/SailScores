using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sailscores.Database.Entities
{
    [Table("BoatClasses")]
    public class BoatClass
    {
        public Guid Id { get; set; }
        public Guid ClubId { get; set; }
        public Club Club { get; set; }

        [StringLength(200)]
        public String Name { get; set; }

        [StringLength(2000)]
        public String Description { get; set; }
        
    }
}
