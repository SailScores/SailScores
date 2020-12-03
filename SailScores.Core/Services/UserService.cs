using Microsoft.EntityFrameworkCore;
using SailScores.Database;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace SailScores.Core.Services
{
    public class UserService : IUserService
    {
        private readonly ISailScoresContext _dbContext;

        public UserService(
            ISailScoresContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddPermision(Guid clubId, string userEmail)
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
                        CanEditAllClubs = false
                    });
                await _dbContext.SaveChangesAsync()
                    .ConfigureAwait(false);
            }
        }

        public async Task<bool> IsUserAllowedToEdit(string email, string clubInitials)
        {
            var clubId = await _dbContext.Clubs
                .Where(c => c.Initials == clubInitials)
                .Select(c => c.Id)
                .SingleOrDefaultAsync()
                .ConfigureAwait(false);
            return await IsUserAllowedToEdit(email, clubId)
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
