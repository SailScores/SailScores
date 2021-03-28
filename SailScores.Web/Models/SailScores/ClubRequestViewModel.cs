using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace SailScores.Web.Models.SailScores
{
    public class ClubRequestViewModel
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Club name is required")]
        [StringLength(200)]
        [Display(Name = "Club name")]
        public String ClubName { get; set; }

        [StringLength(10, ErrorMessage = "Initials are limited to 10 characters or less.")]
        [Display(Name = "Club initials")]
        [Required(ErrorMessage = "Club initials are required")]
        [Remote(action: "VerifyInitials", controller: "ClubRequest")]
        public String ClubInitials { get; set; }

        [Display(Name = "Club location")]
        public String ClubLocation { get; set; }
        [Display(Name = "Club website")]
        public String ClubWebsite { get; set; }

        [Display(Name = "Contact name")]
        public String ContactName { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public String ContactEmail { get; set; }

        public bool? Hide { get; set; }
        public bool? ForTesting { get; set; }

        [Display(Name = "Classes", Description =
            "What classes of boats are raced?")]
        public String Classes { get; set; }

        [Display(Name = "Usual discard rules", Description =
            "Do you have a typical pattern for race discards?")]
        public String TypicalDiscardRules { get; set; }
        [Display(Name = "Other Comments")]
        public String Comments { get; set; }


        [Display(Name = "Submitted")]
        public DateTime? RequestSubmitted { get; set; }

        [Display(Name = "Approved")]
        public DateTime? RequestApproved { get; set; }
        [Display(Name = "Admin Notes")]
        public string AdminNotes { get; set; }

        public Guid? TestClubId { get; set; }
        public Guid? VisibleClubId { get; set; }
    }
}
