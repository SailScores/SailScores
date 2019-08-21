using AutoMapper;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using dbObj = SailScores.Database.Entities;

namespace SailScores.Core.Services
{
    public interface IDbObjectBuilder
    {

        Task<dbObj.Regatta> BuildDbRegattaAsync(Model.Regatta regatta);
        Task<dbObj.Series> BuildDbSeriesAsync(Model.Series series);
        Task<dbObj.Race> BuildDbRaceObj(Guid clubId, Model.Race race);
    }
}
