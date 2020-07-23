using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SailScores.Web.Services
{
    public interface IAuthorizationService
    {
        Task<bool> CanUserEdit(
            ClaimsPrincipal claimsPrincipal,
            string clubInitials);
        Task<bool> CanUserEdit(
            ClaimsPrincipal claimsPrincipal,
            Guid clubId);
        Task<bool> IsUserFullAdmin(ClaimsPrincipal user);
    }
}