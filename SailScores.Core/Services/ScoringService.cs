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
    public class ScoringService : IScoringService
    {
        private readonly ISailScoresContext _dbContext;
        private readonly IMapper _mapper;

        public ScoringService(
            ISailScoresContext dbContext,
            IMapper mapper)
        {
            _dbContext = dbContext;
            _mapper = mapper;
        }

        public async Task<IEnumerable<Model.ScoreCode>> GetScoreCodesAsync(Guid clubId)
        {
            var scoreCodes = await _dbContext
                .ScoringSystems
                .Where(s => s.ClubId == clubId || s.ClubId == null)
                .GroupBy(s => s.Name, (key, g) => g.OrderBy(e => e.Name).First())
                .ToListAsync();

            return _mapper.Map<List<Model.ScoreCode>>(scoreCodes);
        }

        public async Task<IList<Model.ScoringSystem>> GetScoringSystemsAsync(Guid clubId)
        {
            var dbObjects = await _dbContext
                .ScoringSystems
                .Where(s => s.ClubId == clubId)
                .Include(s => s.ScoreCodes)
                .ToListAsync();
            return _mapper.Map<List<Model.ScoringSystem>>(dbObjects);
        }
    }
}
