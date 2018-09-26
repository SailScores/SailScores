using System.Threading.Tasks;

namespace SailScores.Web.Services
{
    public interface IRaceService
    {
        Task GetAllRaceSummariesAsync(string clubInitials);
    }
}