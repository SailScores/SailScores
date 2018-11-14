using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SailScores.Database;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using AutoMapper.QueryableExtensions;
using System.Linq;
using SailScores.Core.Model;
using Db = SailScores.Database.Entities;

namespace SailScores.Core.Services
{
    public class CompetitorService : ICompetitorService
    {
        private readonly ISailScoresContext _dbContext;
        private readonly IMapper _mapper;

        public CompetitorService(
            ISailScoresContext dbContext,
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
    }
}
