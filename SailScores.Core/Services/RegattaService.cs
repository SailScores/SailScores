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
        private readonly IMapper _mapper;

        public RegattaService(
            ISailScoresContext dbContext,
            IMapper mapper)
        {
            _dbContext = dbContext;
            _mapper = mapper;
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

        public Task SaveNewRegatta(Regatta regatta)
        {
            throw new NotImplementedException();
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
