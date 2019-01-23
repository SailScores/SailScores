using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SailScores.Web.Services
{
    public class AuthorizationService : IAuthorizationService
    {
        private readonly Core.Services.IUserService _userService;

        public AuthorizationService(
            Core.Services.IUserService userService
            )
        {
            _userService = userService;
        }

        public async Task<bool> CanUserEdit(
            ClaimsPrincipal claimsPrincipal,
            string clubInitials)
        {
            var email = claimsPrincipal.FindFirst("sub")?.Value;
            return await _userService.IsUserAllowedToEdit(
                email,
                clubInitials);
        }

        public async Task<bool> CanUserEdit(
            ClaimsPrincipal claimsPrincipal,
            Guid clubId)
        {
            var email = claimsPrincipal.FindFirst("sub")?.Value;
            if (String.IsNullOrWhiteSpace(email))
            {
                email = claimsPrincipal.Identity.Name;
            }
            return await _userService.IsUserAllowedToEdit(
                email,
                clubId);
        }
    }
}
