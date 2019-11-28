using SailScores.Core.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using SailScores.Api.Enumerations;

namespace SailScores.Web.Models.SailScores
{
    public class ClubRequestViewModel
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "A club name is required")]
        [StringLength(200)]
        [Display(Name = "Club name")]
        public String ClubName { get; set; }

        [StringLength(10, ErrorMessage ="Initials are limited to 10 characters or less.")]
        [Display(Name = "Club initials")]
        [Required(ErrorMessage = "Club initials are required")]
        public String ClubInitials { get; set; }

        [Display(Name = "Club location")]
        public String ClubLocation { get; set; }
        [Display(Name = "Club website")]
        public String ClubWebsite { get; set; }

        [Display(Name = "Contact name")]
        [Required(ErrorMessage = "Contact name is required")]
        public String ContactName { get; set; }

        [Display(Name = "Contact email")]
        [Required(ErrorMessage = "Contact email is required")]
        [EmailAddress(ErrorMessage = "Invalid Email")]
        public String ContactEmail { get; set; }

        public bool? Hide { get; set; }
        public bool? ForTesting { get; set; }

        [Display(Name = "Classes", Description =
            "What classes does your club race? We'll get started on the set up.")]
        public String Classes { get; set; }

        [Display(Name = "Usual discard rules", Description =
            "What is the typical pattern of discards used by your club?")]
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
