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

    // opted for speedier code with few allocations over readability: this method will be called on every access from an
    // authenticated user, so speed is paramount.
    public string? GetUserEmailOrName(ClaimsPrincipal? claimsPrincipal)
    {
        string? fallback = null;

        if (TryUseCandidate(claimsPrincipal?.FindFirst(ClaimTypes.Email)?.Value, ref fallback, out var value)) return value;
        if (TryUseCandidate(claimsPrincipal?.FindFirst("email")?.Value, ref fallback, out value)) return value;
        if (TryUseCandidate(claimsPrincipal?.FindFirst(ClaimTypes.Name)?.Value, ref fallback, out value)) return value;
        if (TryUseCandidate(claimsPrincipal?.FindFirst("name")?.Value, ref fallback, out value)) return value;
        if (TryUseCandidate(claimsPrincipal?.FindFirst(ClaimTypes.NameIdentifier)?.Value, ref fallback, out value))
            return value;
        if (TryUseCandidate(claimsPrincipal?.FindFirst("sub")?.Value, ref fallback, out value)) return value;
        if (TryUseCandidate(claimsPrincipal?.Identity?.Name, ref fallback, out value)) return value;

        return fallback;
    }

    private static bool TryUseCandidate(string? candidate, ref string? fallback, out string? email)
    {
        email = null;
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return false;
        }

        if (fallback is null)
        {
            fallback = candidate;
        }

        if (!LooksLikeEmail(candidate))
        {
            return false;
        }

        email = candidate;
        return true;
    }

    private static bool LooksLikeEmail(string value)
    {
        var at = value.IndexOf('@');
        if (at <= 0 || at != value.LastIndexOf('@') || at == value.Length - 1)
        {
            return false;
        }

        for (var i = 0; i < at; i++)
        {
            if (char.IsWhiteSpace(value[i]))
            {
                return false;
            }
        }

        var domain = value.AsSpan(at + 1);
        var hasDot = false;

        for (var i = 0; i < domain.Length; i++)
        {
            var c = domain[i];
            if (char.IsWhiteSpace(c))
            {
                return false;
            }

            if (c != '.')
            {
                continue;
            }

            if (i == 0 || i == domain.Length - 1 || domain[i - 1] == '.')
            {
                return false;
            }

            hasDot = true;
        }

        return hasDot;
    }

    public async Task<bool> CanUserEdit(
        ClaimsPrincipal claimsPrincipal,
        string clubInitials)
    {
        var email = GetUserEmailOrName(claimsPrincipal);
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
        var email = GetUserEmailOrName(claimsPrincipal);
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        return await _userService.IsUserAllowedToEdit(
            email,
            clubId);
    }

    public async Task<bool> IsUserFullAdmin(ClaimsPrincipal user)
    {
        var email = GetUserEmailOrName(user);
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
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
        var email = GetUserEmailOrName(claimsPrincipal);

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
        var email = GetUserEmailOrName(claimsPrincipal);

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
        var email = GetUserEmailOrName(claimsPrincipal);

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
        var email = GetUserEmailOrName(claimsPrincipal);

        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        return await _userService.IsUserClubAdministrator(email, clubId);
    }
}
