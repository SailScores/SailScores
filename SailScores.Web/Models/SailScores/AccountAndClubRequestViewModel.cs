using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace SailScores.Web.Models.SailScores
{
    public class AccountAndClubRequestViewModel : ClubRequestViewModel
    {
        
        [Required]
        [DataType(DataType.Text)]
        [Display(Name = "First Name")]
        public string ContactFirstName { get; set; }

        [Required]
        [DataType(DataType.Text)]
        [Display(Name = "Last Name")]
        public string ContactLastName { get; set; }

        [Display(Name = "Enable Browser Analytics")]
        public bool EnableAppInsights { get; set; }


        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at most {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

    }
}
