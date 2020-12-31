﻿using SailScores.Database.Entities;
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
        Task AddPermission(Guid clubId, string userEmail, string addedBy = null);
        Task<IEnumerable<string>> GetClubInitials(string email);
        Task<IEnumerable<UserClubPermission>> GetAllPermissionsForClub(Guid clubId);
        Task<UserClubPermission> GetPermission(Guid permissionId);
        Task Delete(Guid permissionId);
    }
}