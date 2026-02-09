using Microsoft.AspNetCore.Authorization;
using SailScores.Database.Entities;

namespace SailScores.Web.Authorization;

public class ClubPermissionRequirement : IAuthorizationRequirement
{
    public PermissionLevel MinimumLevel { get; }

    public ClubPermissionRequirement(PermissionLevel minimumLevel)
    {
        MinimumLevel = minimumLevel;
    }
}
