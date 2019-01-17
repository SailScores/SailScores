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

        public async Task<bool> IsUserAllowedToEdit(string email, string clubInitials)
        {
            var clubId = ( await _dbContext.Clubs
                    .SingleOrDefaultAsync(c =>
                        c.Initials.ToUpperInvariant()
                        == clubInitials.ToUpperInvariant()))
                    ?.Id;
            return await IsUserAllowedToEdit(email, clubId.Value);

        }

        public async Task<bool> IsUserAllowedToEdit(string email, Guid clubId)
        {
            var userMatches = _dbContext.UserPermissions
                .Where(u => u.UserEmail.ToUpperInvariant()
                == email.ToUpperInvariant());
            if (await userMatches.AnyAsync(u => u.CanEditAllClubs))
            {
                return true;
            }
            return await userMatches.AnyAsync(u => u.ClubId == clubId);
            
        }
    }
}
