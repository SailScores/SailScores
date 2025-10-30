using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace SailScores.Web.Models.SailScores
{
    public class SupporterWithOptionsViewModel
    {
        public Guid Id { get; set; }

        [Required]
        [Display(Name = "Name")]
        [StringLength(200)]
        public string Name { get; set; }

        public Guid? LogoFileId { get; set; }

        [Display(Name = "Logo File")]
        public IFormFile LogoFile { get; set; }

        [Display(Name = "Logo URL")]
        [StringLength(500)]
        public string LogoUrl { get; set; }

        [Display(Name = "Website URL")]
        [StringLength(500)]
        public string WebsiteUrl { get; set; }

        [Display(Name = "Note")]
        public string Note { get; set; }

        [Display(Name = "Club Initials")]
        [StringLength(10)]
        public string ClubInitials { get; set; }

        [Display(Name = "Expiration Date")]
        public DateTime? ExpirationDate { get; set; }

        [Display(Name = "Visible")]
        public bool IsVisible { get; set; }

        public DateTime CreatedDate { get; set; }

        public string CreatedBy { get; set; }

        public DateTime? UpdatedDate { get; set; }

        public string UpdatedBy { get; set; }
    }
}
