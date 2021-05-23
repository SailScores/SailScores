using Microsoft.AspNetCore.Identity;
using System;
using System.ComponentModel.DataAnnotations;

namespace SailScores.Identity.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public bool? EnableAppInsights { get; set; }

        [StringLength(10)]
        public string SpeechRecognitionLanguage { get; set; }

        public string GetDisplayName()
        {
            return (!String.IsNullOrWhiteSpace(FirstName) && !String.IsNullOrWhiteSpace(LastName) )
                ? $"{FirstName} {LastName}"
                : Email;
        }
    }
}
