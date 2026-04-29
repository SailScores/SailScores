using System.ComponentModel.DataAnnotations;

namespace SailScores.Web.Models.AccountViewModels
{
    public class ExternalLoginViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Username (usually your email address)")]
        public string Email { get; set; }

        [Display(Name = "First Name")]
        [StringLength(100)]
        public string FirstName { get; set; }

        [Display(Name = "Last Name")]
        [StringLength(100)]
        public string LastName { get; set; }

        [Display(Name = "Enable Browser Analytics")]
        public bool EnableAppInsights { get; set; }
    }
}
