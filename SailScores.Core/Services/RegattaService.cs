using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SailScores.Core.Model;
using SailScores.Core.Scoring;
using SailScores.Database;
using dbObj = SailScores.Database.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SailScores.Core.FlatModel;

namespace SailScores.Core.Services
{
    public class RegattaService : IRegattaService
    {
        private readonly ISailScoresContext _dbContext;
        private readonly IDbObjectBuilder _dbObjectBuilder;
        private readonly IMapper _mapper;

        public RegattaService(
            ISailScoresContext dbContext,
            IDbObjectBuilder dbObjBuilder,
            IMapper mapper)
        {
            _dbContext = dbContext;
            _dbObjectBuilder = dbObjBuilder;
            _mapper = mapper;
        }
        public async Task<IList<Regatta>> GetAllRegattasAsync(Guid clubId)
        {
            var regattaDb = await _dbContext
                .Clubs
                .Where(c => c.Id == clubId)
                .SelectMany(c => c.Regattas)
                .OrderBy(r => r.StartDate)
                .ToListAsync();

            var returnObj = _mapper.Map<List<Regatta>>(regattaDb);
            return returnObj;
        }

        public async Task<Regatta> GetRegattaAsync(string clubInitials, string seasonName, string regattaName)
        {
            var clubId = await _dbContext.Clubs
                .Where(c =>
                   c.Initials == clubInitials
                ).Select(c => c.Id).SingleAsync();
            var regattaDb = await _dbContext
                .Regattas
                .Where(r =>
                    r.ClubId == clubId)
                .SingleAsync(r => r.Name == regattaName
                                  && r.Season.Name == seasonName);

            var fullRegatta = _mapper.Map<Regatta>(regattaDb);

            return fullRegatta;
        }

        public Task SaveNewRegatta(Regatta regatta, Club club)
        {
            throw new NotImplementedException();
        }

        public async Task SaveNewRegatta(Regatta regatta)
        {
            Database.Entities.Regatta dbRegatta = await _dbObjectBuilder.BuildDbRegattaAsync(regatta);
            
            dbRegatta.UpdatedDate = DateTime.UtcNow;
            if (dbRegatta.Season == null && regatta.Season.Id != Guid.Empty && regatta.Season.Start != default(DateTime))
            {
                var season = _mapper.Map<dbObj.Season>(regatta.Season);
                _dbContext.Seasons.Add(season);
                dbRegatta.Season = season;
            }
            if (dbRegatta.Season == null)
            {
                throw new InvalidOperationException("Could not find or create season for new Regatta.");
            }

            if (_dbContext.Series.Any(s =>
                s.Id == regatta.Id
                || (s.ClubId == regatta.ClubId
                    && s.Name == regatta.Name
                    && s.Season.Id == regatta.Season.Id)))
            {
                throw new InvalidOperationException("Cannot create regatta. A regatta with this name in this season already exists.");
            }

            _dbContext.Regattas.Add(dbRegatta);
            await _dbContext.SaveChangesAsync();
        }

        public Task Update(Regatta model)
        {
            throw new NotImplementedException();
        }
        
        public async Task Delete(Guid regattaId)
        {
            var dbRegatta = await _dbContext.Regattas
                .SingleAsync(c => c.Id == regattaId);

            _dbContext.Regattas.Remove(dbRegatta);

            await _dbContext.SaveChangesAsync();
        }
    }
}
