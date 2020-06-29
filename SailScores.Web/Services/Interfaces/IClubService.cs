using SailScores.Core.Model;
using System.Threading.Tasks;

namespace SailScores.Web.Services
{
    public interface IClubService
    {
        Task<Club> GetClubForClubHome(string clubInitials);
    }
}