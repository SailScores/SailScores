using SailScores.Core.Model;
using SailScores.Web.Models.SailScores;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SailScores.Web.Services
{
    public interface IClubService
    {
        Task<Club> GetClubForClubHome(string clubInitials);
    }
}