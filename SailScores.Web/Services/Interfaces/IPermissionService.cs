using SailScores.Web.Models.SailScores;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SailScores.Web.Services
{
    public interface IPermissionService
    {
        Task<IList<UserViewModel>> GetUsersAsync(Guid clubId);
        Task UpdatePermission(Guid clubId, UserViewModel userModel);
        Task<UserViewModel> GetUserAsync(Guid permissionId);
        Task<bool> CanDelete(string email, Guid permissionId);
        Task Delete(Guid id);
    }
}