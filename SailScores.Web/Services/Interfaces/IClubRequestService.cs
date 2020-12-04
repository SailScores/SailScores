using SailScores.Web.Models.SailScores;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SailScores.Web.Services
{
    public interface IClubRequestService
    {
        Task SubmitRequest(ClubRequestViewModel request);
        Task<IList<ClubRequestViewModel>> GetPendingRequests();
        Task<ClubRequestWithOptionsViewModel> GetRequest(Guid id);
        Task ProcessRequest(Guid id, bool test, Guid? copyFromClubId);
        Task UpdateRequest(ClubRequestViewModel vm);
        Task<bool> VerifyInitials(string initials);
    }
}