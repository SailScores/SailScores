using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using SailScores.Core.Services;
using SailScores.Database.Entities;

namespace SailScores.Web.Authorization;

public class ClubPermissionHandler : AuthorizationHandler<ClubPermissionRequirement>
{
    private readonly IUserService _userService;
    private readonly SailScores.Core.Services.IClubService _clubService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ClubPermissionHandler(
        IUserService userService,
        SailScores.Core.Services.IClubService clubService,
        IHttpContextAccessor httpContextAccessor)
    {
        _userService = userService;
        _clubService = clubService;
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ClubPermissionRequirement requirement)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var clubInitials = httpContext?.Request.RouteValues["clubInitials"]?.ToString();

        if (string.IsNullOrEmpty(clubInitials))
        {
            return; // Fail silently, requirement not met
        }

        var email = context.User.FindFirst("sub")?.Value 
                    ?? context.User.Identity?.Name;

        if (string.IsNullOrEmpty(email))
        {
            return;
        }

        // Check if user is a super admin (CanEditAllClubs)
        var isFullAdmin = await _userService.IsUserFullAdmin(email);
        if (isFullAdmin)
        {
            context.Succeed(requirement);
            return;
        }

        // Otherwise, check club-specific permission
        var clubId = await _clubService.GetClubId(clubInitials);
        var userLevel = await _userService.GetPermissionLevel(email, clubId);

        // Lower enum value = higher permission (Admin=0, SeriesScorekeeper=1, RaceScorekeeper=2)
        if (userLevel.HasValue && userLevel.Value <= requirement.MinimumLevel)
        {
            context.Succeed(requirement);
        }
    }
}
