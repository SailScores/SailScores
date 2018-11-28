using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Sailscores.Database;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using AutoMapper.QueryableExtensions;
using System.Linq;
using Sailscores.Core.Model;
using Db = Sailscores.Database.Entities;

namespace Sailscores.Core.Services
{
    public class CompetitorService : ICompetitorService
    {
        private readonly ISailscoresContext _dbContext;
        private readonly IMapper _mapper;

        public CompetitorService(
            ISailscoresContext dbContext,
            IMapper mapper)
        {
            _dbContext = dbContext;
            _mapper = mapper;
        }

        public async Task<IList<Model.Competitor>> GetCompetitorsAsync(Guid clubId)
        {
            var dbObjects = await _dbContext
                .Clubs
                .Where(c => c.Id == clubId)
                .SelectMany(c => c.Competitors)
                .ToListAsync();
            return _mapper.Map<List<Model.Competitor>>(dbObjects);
        }

        public async Task<Model.Competitor> GetCompetitorAsync(Guid id)
        {
            var competitor = await 
                _dbContext
                .Competitors
                .FirstOrDefaultAsync(c => c.Id == id);

            return _mapper.Map<Model.Competitor>(competitor);
        }

        public async Task SaveAsync(Competitor comp)
        {
            var dbObject = await _dbContext
                .Competitors
                .FirstOrDefaultAsync(
                    c =>
                    c.Id == comp.Id);
            var addingNew = dbObject == null;
            if(addingNew)
            {
                if(comp.Id == null || comp.Id == Guid.Empty)
                {
                    comp.Id = Guid.NewGuid();
                }
                dbObject = _mapper.Map<Db.Competitor>(comp);
                await _dbContext.Competitors.AddAsync(dbObject);
            }
            else
            {
                dbObject.Name = comp.Name;
                dbObject.SailNumber = comp.SailNumber;
                dbObject.AlternativeSailNumber = comp.AlternativeSailNumber;
                dbObject.BoatName = comp.BoatName;
                dbObject.Notes = comp.Notes;
                //todo: class
                //todo: fleets
                //todo: scores
            }

            await _dbContext.SaveChangesAsync();

        }
    }
}
