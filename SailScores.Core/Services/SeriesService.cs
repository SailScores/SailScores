using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SailScores.Core.Model;
using SailScores.Core.Scoring;
using SailScores.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SailScores.Core.Services
{
    public class SeriesService : ISeriesService
    {
        private readonly ISailScoresContext _dbContext;
        private readonly ISeriesCalculator _seriesCalculator;
        private readonly IMapper _mapper;

        public SeriesService(
            ISailScoresContext dbContext,
            ISeriesCalculator seriesCalculator,
            IMapper mapper)
        {
            _dbContext = dbContext;
            _seriesCalculator = seriesCalculator;
            _mapper = mapper;
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
