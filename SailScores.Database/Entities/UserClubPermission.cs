using System;
using System.ComponentModel.DataAnnotations;

namespace Sailscores.Database.Entities
{
    public class UserClubPermission
    {
        public Guid Id { get; set; }
        [StringLength(254)]
        public string UserEmail { get; set; }
        public Guid? ClubId { get; set; }
        public bool CanEditAllClubs { get; set; }
    }
}
