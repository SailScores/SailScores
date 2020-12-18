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
        Task AddPermision(Guid clubId, string userEmail);
        Task<IEnumerable<string>> GetClubInitials(string email);
    }
}