using System.Security.Claims;

namespace SailScores.Web.Services.Interfaces;

public interface IAuthorizationService
{
    Task<bool> CanUserEdit(
        ClaimsPrincipal claimsPrincipal,
        string clubInitials);
    Task<bool> CanUserEdit(
        ClaimsPrincipal claimsPrincipal,
        Guid clubId);
    Task<bool> IsUserFullAdmin(ClaimsPrincipal user);
    Task<string> GetHomeClub(string email);
}