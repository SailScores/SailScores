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
using SailScores.Api.Dtos;
using Microsoft.Extensions.Logging;

namespace SailScores.Core.Services
{
    public class MergeService : IMergeService
    {
        private readonly ISailScoresContext _dbContext;
        private readonly ICompetitorService _competitorService;
        private readonly ISeriesService _seriesService;


        private readonly ILogger _logger;
        private readonly IMapper _mapper;

        public MergeService(
            ISailScoresContext dbContext,
            ICompetitorService competitorService,
            ISeriesService seriesService,
            ILogger<IMergeService> logger,
            IMapper mapper)
        {
            _dbContext = dbContext;
            _competitorService = competitorService;
            _seriesService = seriesService;
            _logger = logger;
            _mapper = mapper;
        }

        public async Task<int?> GetRaceCountFor(Guid competitorId)
        {
            return await _dbContext.Scores.CountAsync(s => s.CompetitorId == competitorId);
        }

        public async Task<IList<Season>> GetSeasonsFor(Guid competitorId)
        {
            var clubId = 
                (await _dbContext.Competitors
                .SingleAsync(c => c.Id == competitorId))
                .ClubId;
            var raceDates = _dbContext.Competitors
                .Where(c => c.Id == competitorId)
                .SelectMany(c => c.Scores)
                .Select(s => s.Race)
                .Select(r => r.Date);

            var seasons = _dbContext.Seasons
                .Where(s => s.ClubId == clubId)
                .Where(s => raceDates.Any(r => r.HasValue && r.Value > s.Start && r.Value <= s.End));
            return _mapper.Map<IList<Season>>(await seasons.ToListAsync());
        }

        public async Task<IList<Competitor>> GetSourceOptionsFor(Guid targetCompetitorId)
        {
            var targetComp = await _competitorService.GetCompetitorAsync(targetCompetitorId);
            
            var targetRaceIds = GetRacesForComp(targetCompetitorId);
            
            var sourceList = 
                _dbContext.Competitors.Where(c => c.ClubId == targetComp.ClubId
                && c.Id != targetComp.Id
                && !(c.Scores.Any(s => targetRaceIds.Contains(s.RaceId))))
                .OrderBy(c => c.Name);

            return _mapper.Map<IList<Competitor>>(await sourceList.ToListAsync());

        }

        public async Task Merge(Guid targetCompetitorId, Guid sourceCompetitorId)
        {
            _logger.LogInformation("Merging competitors {0} and {1}", targetCompetitorId, sourceCompetitorId);
            var scoresToMove = _dbContext.Scores
                .Where(s => s.CompetitorId == sourceCompetitorId);
            var seriesIds = await scoresToMove
                .Select(s => s.Race)
                .SelectMany(s => s.SeriesRaces)
                .Select(s => s.SeriesId)
                .Distinct()
                .ToListAsync();

            await scoresToMove.ForEachAsync(s => s.CompetitorId = targetCompetitorId);
            await _dbContext.SaveChangesAsync();

            await _competitorService.DeleteCompetitorAsync(sourceCompetitorId);

            foreach(var seriesId in seriesIds)
            {
                await _seriesService.UpdateSeriesResults(seriesId);
            }
        }

        private IQueryable<Guid> GetRacesForComp(Guid competitorId)
        {
            return _dbContext.Scores.Where(s => s.CompetitorId == competitorId).Select(s => s.RaceId);
        }
    }
}
