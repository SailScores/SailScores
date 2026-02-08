using Microsoft.EntityFrameworkCore;
using SailScores.Database;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Caching.Memory;

namespace SailScores.Core.Services
{
    public class UserService : IUserService
    {
        private readonly ISailScoresContext _dbContext;
        private readonly IMemoryCache _cache;

        public UserService(
            ISailScoresContext dbContext,
            IMemoryCache cache)
        {
            _dbContext = dbContext;
            _cache = cache;
        }

        public async Task AddPermission(
            Guid clubId,
            string userEmail,
            string addedBy,
            Database.Entities.PermissionLevel permissionLevel = Database.Entities.PermissionLevel.ClubAdministrator)
        {
            var existingPermision = await _dbContext.UserPermissions
                .FirstOrDefaultAsync(p => p.UserEmail == userEmail && p.ClubId == clubId)
                .ConfigureAwait(false);

            if (existingPermision == null)
            {
                _dbContext.UserPermissions.Add(
                    new Database.Entities.UserClubPermission
                    {
                        ClubId = clubId,
                        UserEmail = userEmail,
                        CanEditAllClubs = false,
                        PermissionLevel = permissionLevel,
                        Created = DateTime.UtcNow,
                        CreatedBy = addedBy
                    });
                await _dbContext.SaveChangesAsync()
                    .ConfigureAwait(false);
            }
        }

        public async Task Delete(Guid permissionId)
        {
            var existingPermision = await _dbContext.UserPermissions
                .SingleOrDefaultAsync(p => p.Id == permissionId)
                .ConfigureAwait(false);
            _dbContext.UserPermissions.Remove(existingPermision);
            await _dbContext.SaveChangesAsync()
                .ConfigureAwait(false);
        }

        public async Task<IEnumerable<Database.Entities.UserClubPermission>> GetAllPermissionsForClub(Guid clubId)
        {
            return _dbContext.UserPermissions.Where(p => p.ClubId == clubId);
        }

        public async Task<IEnumerable<string>> GetClubInitials(string email)
        {
            var clubIds = await _dbContext.UserPermissions
                .Where(u => u.UserEmail == email
                            && !u.CanEditAllClubs)
                .Select(u => u.ClubId)
                .ToListAsync()
                .ConfigureAwait(false);
            
            var initials = (await _dbContext.Clubs
                .ToListAsync()
                .ConfigureAwait(false))
                .Where(c => clubIds.Contains(c.Id))
                .Select(c => c.Initials);

            return initials;
        }

        public async Task<Database.Entities.UserClubPermission> GetPermission(Guid permissionId)
        {
            return await _dbContext.UserPermissions
                .FirstOrDefaultAsync(p => p.Id == permissionId);
        }

        public async Task<bool> IsUserAllowedToEdit(string email, string clubInitials)
        {
            var clubGuid = Guid.Empty;

            if (!_cache.TryGetValue($"ClubId_{clubInitials}", out clubGuid))
            {
                clubGuid = await _dbContext.Clubs
                    .Where(c => c.Initials == clubInitials)
                    .Select(c => c.Id)
                    .SingleOrDefaultAsync()
                    .ConfigureAwait(false);
                _cache.Set($"ClubId_{clubInitials}", clubGuid, TimeSpan.FromMinutes(30));
            }

            return await IsUserAllowedToEdit(email, clubGuid)
                .ConfigureAwait(false);

        }

        public async Task<bool> IsUserAllowedToEdit(string email, Guid? clubId)
        {
            if (String.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            if (clubId.HasValue)
            {
                var permission = await _dbContext.UserPermissions
                    .AnyAsync(u => u.UserEmail == email && u.ClubId == clubId)
                    .ConfigureAwait(false);

                if (permission)
                {
                    return true;
                }
            }

            var editAny = await _dbContext.UserPermissions
                .Where(u => u.UserEmail == email)
                .AnyAsync(u => u.CanEditAllClubs)
                .ConfigureAwait(false);

            return editAny;
        }

        public async Task<bool> IsUserFullAdmin(string email)
        {
            if (String.IsNullOrWhiteSpace(email))
            {
                return false;
            }
            return await _dbContext.UserPermissions
                .AnyAsync(u => u.UserEmail == email &&
                               u.CanEditAllClubs)
                .ConfigureAwait(false);
        }

        public async Task<IList<Guid>> GetClubIdsForUserEmailAsync(string email)
        {
            // Use a single query with a join to get club IDs that exist in both tables
            return await _dbContext.UserPermissions
                .Where(u => u.UserEmail == email && u.ClubId.HasValue)
                .Join(_dbContext.Clubs,
                    permission => permission.ClubId.Value,
                    club => club.Id,
                    (permission, club) => club.Id)
                .ToListAsync()
                .ConfigureAwait(false);
        }

        public async Task<bool> CanEditSeries(string email, Guid clubId)
        {
            if (String.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            // Full admins can do everything
            if (await IsUserFullAdmin(email))
            {
                return true;
            }

            var permission = await _dbContext.UserPermissions
                .FirstOrDefaultAsync(u => u.UserEmail == email && u.ClubId == clubId)
                .ConfigureAwait(false);

            if (permission == null)
            {
                return false;
            }

            // ClubAdministrator and SeriesScorekeeper can edit series
            return permission.PermissionLevel == Database.Entities.PermissionLevel.ClubAdministrator ||
                   permission.PermissionLevel == Database.Entities.PermissionLevel.SeriesScorekeeper;
        }

        public async Task<bool> CanEditRaces(string email, Guid clubId)
        {
            if (String.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            // Full admins can do everything
            if (await IsUserFullAdmin(email))
            {
                return true;
            }

            var permission = await _dbContext.UserPermissions
                .FirstOrDefaultAsync(u => u.UserEmail == email && u.ClubId == clubId)
                .ConfigureAwait(false);

            if (permission == null)
            {
                return false;
            }

            // All permission levels can edit races
            return true;
        }

        public async Task<bool> CanEditRaces(string email, string clubInitials)
        {
            var clubGuid = Guid.Empty;

            if (!_cache.TryGetValue($"ClubId_{clubInitials}", out clubGuid))
            {
                clubGuid = await _dbContext.Clubs
                    .Where(c => c.Initials == clubInitials)
                    .Select(c => c.Id)
                    .SingleOrDefaultAsync()
                    .ConfigureAwait(false);
                _cache.Set($"ClubId_{clubInitials}", clubGuid, TimeSpan.FromMinutes(30));
            }

            return await CanEditRaces(email, clubGuid)
                .ConfigureAwait(false);
        }

        public async Task<Database.Entities.PermissionLevel?> GetPermissionLevel(string email, Guid clubId)
        {
            if (String.IsNullOrWhiteSpace(email))
            {
                return null;
            }

            var permission = await _dbContext.UserPermissions
                .FirstOrDefaultAsync(u => u.UserEmail == email && u.ClubId == clubId)
                .ConfigureAwait(false);

            return permission?.PermissionLevel;
        }

        public async Task<bool> IsUserClubAdministrator(string email, Guid clubId)
        {
            if (String.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            // Full admins are also club administrators
            if (await IsUserFullAdmin(email))
            {
                return true;
            }

            var permission = await _dbContext.UserPermissions
                .FirstOrDefaultAsync(u => u.UserEmail == email && u.ClubId == clubId)
                .ConfigureAwait(false);

            return permission?.PermissionLevel == Database.Entities.PermissionLevel.ClubAdministrator;
        }
    }
}
