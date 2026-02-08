using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;
using SailScores.Web.Services.Interfaces;

namespace SailScores.Web.Services;

public class AuthorizationService : IAuthorizationService
{
    private readonly Core.Services.IUserService _userService;
    private readonly IMemoryCache _cache;

    public AuthorizationService(
        Core.Services.IUserService userService,
        IMemoryCache cache
    )
    {
        _userService = userService;
        _cache = cache;
    }

    public async Task<bool> CanUserEdit(
        ClaimsPrincipal claimsPrincipal,
        string clubInitials)
    {
        var email = claimsPrincipal?.FindFirst("sub")?.Value;
        if (String.IsNullOrWhiteSpace(email))
        {
            email = claimsPrincipal?.Identity?.Name;
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        var cacheKey = $"CanEdit_{email}_{clubInitials}";
        if (_cache.TryGetValue(cacheKey, out bool canEdit))
        {
            return canEdit;
        }

        canEdit = await _userService.IsUserAllowedToEdit(email, clubInitials);
        _cache.Set(cacheKey, canEdit, TimeSpan.FromMinutes(2));
        return canEdit;
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

    public async Task<bool> IsUserFullAdmin(ClaimsPrincipal user)
    {
        var email = user.FindFirst("sub")?.Value;
        if (String.IsNullOrWhiteSpace(email))
        {
            email = user.Identity.Name;
        }
        return await _userService.IsUserFullAdmin(
            email);
    }

    public async Task<string> GetHomeClub(string email)
    {
        var clubInitials = await _userService.GetClubInitials(email);

        if (clubInitials.Count() <= 1)
        {
            return clubInitials.FirstOrDefault();
        }

        return String.Empty;
    }

    public async Task<bool> CanUserEditSeries(
        ClaimsPrincipal claimsPrincipal,
        Guid clubId)
    {
        var email = claimsPrincipal?.FindFirst("sub")?.Value;
        if (String.IsNullOrWhiteSpace(email))
        {
            email = claimsPrincipal?.Identity?.Name;
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        return await _userService.CanEditSeries(email, clubId);
    }

    public async Task<bool> CanUserEditRaces(
        ClaimsPrincipal claimsPrincipal,
        Guid clubId)
    {
        var email = claimsPrincipal?.FindFirst("sub")?.Value;
        if (String.IsNullOrWhiteSpace(email))
        {
            email = claimsPrincipal?.Identity?.Name;
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        return await _userService.CanEditRaces(email, clubId);
    }

    public async Task<bool> CanUserEditRaces(
        ClaimsPrincipal claimsPrincipal,
        string clubInitials)
    {
        var email = claimsPrincipal?.FindFirst("sub")?.Value;
        if (String.IsNullOrWhiteSpace(email))
        {
            email = claimsPrincipal?.Identity?.Name;
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        var cacheKey = $"CanEditRaces_{email}_{clubInitials}";
        if (_cache.TryGetValue(cacheKey, out bool canEdit))
        {
            return canEdit;
        }

        canEdit = await _userService.CanEditRaces(email, clubInitials);
        _cache.Set(cacheKey, canEdit, TimeSpan.FromMinutes(2));
        return canEdit;
    }

    public async Task<bool> IsUserClubAdministrator(
        ClaimsPrincipal claimsPrincipal,
        Guid clubId)
    {
        var email = claimsPrincipal?.FindFirst("sub")?.Value;
        if (String.IsNullOrWhiteSpace(email))
        {
            email = claimsPrincipal?.Identity?.Name;
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        return await _userService.IsUserClubAdministrator(email, clubId);
    }
}
