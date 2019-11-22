using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SailScores.Api.Dtos;
using SailScores.Core.Model;

namespace SailScores.Core.Services
{
    public interface IClubRequestService
    {
        Task Submit(ClubRequest coreRequest);
        Task<IList<ClubRequest>> GetPendingRequests();
        Task<ClubRequest> GetRequest(Guid id);
        Task UpdateRequest(ClubRequest coreRequest);
    }
}