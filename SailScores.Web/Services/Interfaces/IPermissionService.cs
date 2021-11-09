using SailScores.Web.Models.SailScores;

namespace SailScores.Web.Services.Interfaces;

public interface IPermissionService
{
    Task<IList<UserViewModel>> GetUsersAsync(Guid clubId);
    Task UpdatePermission(Guid clubId, UserViewModel userModel);
    Task<UserViewModel> GetUserAsync(Guid permissionId);
    Task<bool> CanDelete(string email, Guid permissionId);
    Task Delete(Guid id);
}