using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SailScores.Core.Model;

namespace SailScores.Core.Services
{
    public interface IUserService
    {
        Task<bool> IsUserAllowedToEdit(string email, string clubInitials);
        Task<bool> IsUserAllowedToEdit(string email, Guid clubId);
    }
}