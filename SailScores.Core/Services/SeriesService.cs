using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Sailscores.Core.Model;
using Sailscores.Core.Scoring;
using Sailscores.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sailscores.Core.Services
{
    public class SeriesService : ISeriesService
    {
        private readonly ISailscoresContext _dbContext;
        private readonly ISeriesCalculator _seriesCalculator;
        private readonly IMapper _mapper;

        public SeriesService(
            ISailscoresContext dbContext,
            ISeriesCalculator seriesCalculator,
            IMapper mapper)
        {
            _dbContext = dbContext;
            _seriesCalculator = seriesCalculator;
            _mapper = mapper;
        }

        public async Task<IEnumerable<Series>> GetAllSeriesAsync(Guid clubId)
        {
            var seriesDb = await _dbContext
                .Clubs
                .Where(c => c.Id == clubId)
                .SelectMany(c => c.Series)
                .ToListAsync();


            var returnObj = _mapper.Map<List<Series>>(seriesDb);
            return returnObj;
        }

        public async Task<Series> GetOneSeriesAsync(Guid guid)
        {
            var seriesDb = await _dbContext
                .Series
                .FirstAsync(c => c.Id == guid);

            return _mapper.Map<Series>(seriesDb);
        }

            public async Task<Series> GetSeriesDetailsAsync(
            string clubInitials,
            string seasonName,
            string seriesName)
        {
            var seriesDb = await _dbContext
                .Clubs
                .Where(c => c.Initials == clubInitials)
                .SelectMany(c => c.Series)
                .Where(s => s.Name == seriesName
                    && s.Season.Name == seasonName)
                .Include(s => s.RaceSeries)
                    .ThenInclude(rs => rs.Race)
                        .ThenInclude(r => r.Scores)
                            .ThenInclude(s => s.Competitor)
                    .Include(s => s.Season)
                .SingleAsync(s => s.Name == seriesName
                                  && s.Season.Name == seasonName);


            var returnObj = _mapper.Map<Series>(seriesDb);

            var results = _seriesCalculator.CalculateResults(returnObj);
            returnObj.Results = results;
            return returnObj;
        }
    }
}
