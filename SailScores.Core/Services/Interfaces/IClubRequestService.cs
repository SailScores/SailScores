using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SailScores.Core.Model;

namespace SailScores.Core.Services
{
    public interface IClubRequestService
    {
        Task Submit(ClubRequest clubRequest);
        Task<IList<ClubRequest>> GetPendingRequests();
        Task<ClubRequest> GetRequest(Guid id);
        Task UpdateRequest(ClubRequest clubRequest);
    }
}