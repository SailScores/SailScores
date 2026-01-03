using System;
using System.ComponentModel.DataAnnotations;

namespace SailScores.Core.Model
{
    public class Supporter
    {
        public Guid Id { get; set; }

        [StringLength(200)]
        public string Name { get; set; }

        public Guid? LogoFileId { get; set; }

        [StringLength(500)]
        public string LogoUrl { get; set; }

        [StringLength(500)]
        public string WebsiteUrl { get; set; }

        public string Note { get; set; }

        [StringLength(10)]
        public string ClubInitials { get; set; }

        public Guid? ClubId { get; set; }

        public DateTime? ExpirationDate { get; set; }

        public bool IsVisible { get; set; }

        public DateTime CreatedDate { get; set; }

        [StringLength(128)]
        public string CreatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }

        [StringLength(128)]
        public string UpdatedBy { get; set; }
    }
}
