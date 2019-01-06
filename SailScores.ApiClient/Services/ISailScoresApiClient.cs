using SailScores.Api.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SailScores.Api.Services
{
    public interface ISailScoresApiClient
    {
        Task<List<ClubDto>> GetClubsAsync();
        Task<List<BoatClassDto>> GetBoatClassesAsync(Guid clubId);
        Task<List<FleetDto>> GetFleetsAsync(Guid clubId);

        Task<Guid> SaveClub(ClubDto club);
        Task<Guid> SaveBoatClass(BoatClassDto boatClass);
        Task<Guid> SaveFleet(FleetDto fleet);
    }
}