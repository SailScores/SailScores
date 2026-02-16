using SailScores.Database.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SailScores.Core.Services
{
    public interface IUserService
    {
        Task<bool> IsUserAllowedToEdit(string email, string clubInitials);
        Task<bool> IsUserAllowedToEdit(string email, Guid? clubId);
        Task<bool> IsUserFullAdmin(string email);
        Task<bool> IsUserClubAdministrator(string email, Guid clubId);
        Task<bool> CanEditSeries(string email, Guid clubId);
        Task<bool> CanEditRaces(string email, Guid clubId);
        Task<bool> CanEditRaces(string email, string clubInitials);
        Task<PermissionLevel?> GetPermissionLevel(string email, Guid clubId);
        Task AddPermission(Guid clubId, string userEmail, string addedBy = null, PermissionLevel permissionLevel = PermissionLevel.ClubAdministrator);
        Task UpdatePermissionLevel(Guid permissionId, PermissionLevel level);
        Task<IEnumerable<string>> GetClubInitials(string email);
        Task<IEnumerable<UserClubPermission>> GetAllPermissionsForClub(Guid clubId);
        Task<UserClubPermission> GetPermission(Guid permissionId);
        Task Delete(Guid permissionId);
        Task<IList<Guid>> GetClubIdsForUserEmailAsync(string email);
    }
}
