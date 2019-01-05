using SailScores.Api.Dtos;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SailScores.Api.Services
{
    public interface ISailScoresApiClient
    {
        Task<List<ClubDto>> GetClubsAsync();
    }
}