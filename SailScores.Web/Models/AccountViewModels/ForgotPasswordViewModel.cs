using System.ComponentModel.DataAnnotations;

namespace SailScores.Web.Models.AccountViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "Username (usually your email address)")]
        public string Email { get; set; }
    }
}
