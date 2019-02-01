using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SailScores.Api.Dtos;
using SailScores.Core.Model;

namespace SailScores.Core.Services
{
    public interface IRaceService
    {
        Task<IList<Model.Race>> GetRacesAsync(Guid clubId);
        Task<IList<Model.Race>> GetFullRacesAsync(Guid clubId);
        Task<Race> GetRaceAsync(Guid raceId);
        Task<Guid> SaveAsync(RaceDto race);
        Task Delete(Guid raceId);
    }
}