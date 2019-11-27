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
                .FirstOrDefaultAsync(p => p.UserEmail == userEmail && p.ClubId == clubId);

            if(existingPermision == null)
            {
                _dbContext.UserPermissions.Add(
                    new Database.Entities.UserClubPermission
                    {
                        ClubId = clubId,
                        UserEmail = userEmail,
                        CanEditAllClubs = false
                    });
                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task<bool> IsUserAllowedToEdit(string email, string clubInitials)
        {
            var clubId = ( await _dbContext.Clubs
                    .SingleOrDefaultAsync(c =>
                        c.Initials
                        == clubInitials))
                    ?.Id;
            return await IsUserAllowedToEdit(email, clubId);

        }

        public async Task<bool> IsUserAllowedToEdit(string email, Guid? clubId)
        {
            if(String.IsNullOrWhiteSpace(email))
            {
                return false;
            }
            var userMatches = _dbContext.UserPermissions
                .Where(u => u.UserEmail
                == email);
            if (await userMatches.AnyAsync(u => u.CanEditAllClubs))
            {
                return true;
            }
            if (!clubId.HasValue)
            {
                return false;
            }
            return await userMatches.AnyAsync(u => u.ClubId == clubId);
            
        }

        public async Task<bool> IsUserFullAdmin(string email)
        {
            if (String.IsNullOrWhiteSpace(email))
            {
                return false;
            }
            var userMatches = _dbContext.UserPermissions
                .Where(u => u.UserEmail
                == email);
            if (await userMatches.AnyAsync(u => u.CanEditAllClubs))
            {
                return true;
            }
            return false;
        }
    }
}
