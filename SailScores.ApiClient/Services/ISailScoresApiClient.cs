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
        Task<List<SeasonDto>> GetSeasonsAsync(Guid id);
        Task<List<CompetitorDto>> GetCompetitors(Guid clubId, Guid? fleetId);

        Task<Guid> SaveClub(ClubDto club);
        Task<Guid> SaveBoatClass(BoatClassDto boatClass);
        Task<Guid> SaveFleet(FleetDto fleet);
        Task<Guid> SaveSeries(SeriesDto series);
        Task<Guid> SaveSeason(SeasonDto season);
        Task<Guid> SaveCompetitor(CompetitorDto comp);
    }
}