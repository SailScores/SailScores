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
    public class ScoringService : IScoringService
    {
        private readonly ISailscoresContext _dbContext;
        private readonly IMapper _mapper;

        public ScoringService(
            ISailscoresContext dbContext,
            IMapper mapper)
        {
            _dbContext = dbContext;
            _mapper = mapper;
        }

        public async Task<IEnumerable<Model.ScoreCode>> GetScoreCodesAsync(Guid clubId)
        {
            var dbObjects = await _dbContext
                .Clubs
                .Where(c => c.Id == clubId)
                .SelectMany(c => c.ScoreCodes)
                .ToListAsync();
            return _mapper.Map<List<Model.ScoreCode>>(dbObjects);
        }
    }
}
