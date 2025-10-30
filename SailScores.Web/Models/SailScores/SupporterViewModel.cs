using System;
using System.ComponentModel.DataAnnotations;

namespace SailScores.Web.Models.SailScores
{
    public class SupporterViewModel
    {
        public Guid Id { get; set; }

        [Display(Name = "Name")]
        public string Name { get; set; }

        public Guid? LogoFileId { get; set; }

        [Display(Name = "Logo URL")]
        public string LogoUrl { get; set; }

        [Display(Name = "Website URL")]
        public string WebsiteUrl { get; set; }

        [Display(Name = "Note")]
        public string Note { get; set; }

        [Display(Name = "Club")]
        public string ClubInitials { get; set; }

        [Display(Name = "Expiration Date")]
        public DateTime? ExpirationDate { get; set; }

        public bool IsVisible { get; set; }

        public DateTime CreatedDate { get; set; }

        public string CreatedBy { get; set; }
    }
}
