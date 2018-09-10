using System.Collections.Generic;
using System.Threading.Tasks;

namespace SailScores.Core.Services
{
    public interface IClubService
    {
        Task<IList<Model.Club>> GetClubs();
    }
}