using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sailscores.Core.Model;

namespace Sailscores.Core.Services
{
    public interface IRaceService
    {
        Task<IList<Model.Race>> GetRacesAsync(Guid clubId);
        Task<IList<Model.Race>> GetFullRacesAsync(Guid clubId);
        Task<Race> GetRaceAsync(Guid id);
    }
}