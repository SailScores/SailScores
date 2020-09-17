using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace SailScores.Identity.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public string GetDisplayName()
        {
            return (!String.IsNullOrWhiteSpace(FirstName) && !String.IsNullOrWhiteSpace(LastName) )
                ? $"{FirstName} {LastName}"
                : Email;
        }
    }
}
