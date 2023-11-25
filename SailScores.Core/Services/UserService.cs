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
            string addedBy)
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
                _cache.Set($"ClubId_{clubInitials}", clubGuid, TimeSpan.FromMinutes(10));
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
    }
}
