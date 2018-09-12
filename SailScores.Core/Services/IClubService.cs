using System.Collections.Generic;
using System.Threading.Tasks;
using SailScores.Core.Model;

namespace SailScores.Core.Services
{
    public interface IClubService
    {
        Task<IList<Model.Club>> GetClubs(bool includeHidden);
        Task<Club> GetFullClub(string id);
    }
}