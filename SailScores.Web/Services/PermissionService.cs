using SailScores.Web.Models.SailScores;
using SailScores.Identity.Entities;
using Microsoft.AspNetCore.Identity;
using SailScores.Web.Services.Interfaces;

namespace SailScores.Web.Services;

public class PermissionService : IPermissionService
{
    private readonly SailScores.Core.Services.IUserService _userService;
    private readonly IAuthorizationService _authService;
    private readonly UserManager<ApplicationUser> _userManager;

    public PermissionService(
        SailScores.Core.Services.IUserService userService,
        IAuthorizationService authService,
        UserManager<ApplicationUser> userManager,
        IMapper mapper
    )
    {
        _userService = userService;
        _authService = authService;
        _userManager = userManager;
    }

    public async Task<bool> CanDelete(string email, Guid permissionId)
    {
        // Only check so far is to keep user from deleting themselves.
        var permission = await _userService.GetPermission(permissionId);

        return !(permission.UserEmail.Equals(email, StringComparison.InvariantCultureIgnoreCase));
    }

    public async Task Delete(Guid id)
    {
        await _userService.Delete(id);
    }

    public async Task<UserViewModel> GetUserAsync(Guid permissionId)
    {
        var permission = await _userService.GetPermission(permissionId);

        var vm = new UserViewModel
        {
            Id = permission.Id,
            EmailAddress = permission.UserEmail,
            Registered = false,
            Created = permission.Created ?? new DateTime(2019, 2, 25),
            CreatedBy = permission.CreatedBy
        };

        var identityObj = await _userManager.FindByEmailAsync(permission.UserEmail);
        if (identityObj != null)
        {
            vm.Name = identityObj.FirstName + " " + identityObj.LastName;
            vm.Registered = true;
        }
        return vm;
        
    }

    public async Task<IList<UserViewModel>> GetUsersAsync(Guid clubId)
    {
        var permissions = await _userService.GetAllPermissionsForClub(clubId);

        var retList = new List<UserViewModel>();
        foreach(var user in permissions.ToList())
        {
            var vm = new UserViewModel
            {
                Id = user.Id,
                EmailAddress = user.UserEmail,
                Registered = false,
                Created = user.Created ?? new DateTime(2019, 2, 25),
                CreatedBy = user.CreatedBy
            };

            var identityObj = await _userManager.FindByEmailAsync(user.UserEmail);
            if (identityObj != null)
            {
                vm.Name = identityObj.FirstName + " " + identityObj.LastName;
                vm.Registered = true;
            }
            retList.Add(vm);

        }
        return retList;
    }

    public async Task UpdatePermission(Guid clubId, UserViewModel userModel)
    {

        var permissions = await _userService.GetAllPermissionsForClub(clubId);
        bool found = false;
            
        foreach(var permission in permissions)
        {
            found = permission.UserEmail.Equals(userModel.EmailAddress, StringComparison.InvariantCultureIgnoreCase);
            if (found) break;
        }
        if (!found) {
            await _userService.AddPermission(clubId, userModel.EmailAddress, userModel.CreatedBy);
        }

    }
}