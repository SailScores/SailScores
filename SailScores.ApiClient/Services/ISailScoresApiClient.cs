using SailScores.Core.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SailScores.ApiClient.Services
{
    public interface ISailScoresApiClient
    {
        Task<List<Club>> GetClubsAsync();
    }
}