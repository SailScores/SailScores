using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sailscores.Core.Model;

namespace Sailscores.Core.Services
{
    public interface IScoringService
    {
        Task<IEnumerable<ScoreCode>> GetScoreCodesAsync(Guid clubId);
    }
}