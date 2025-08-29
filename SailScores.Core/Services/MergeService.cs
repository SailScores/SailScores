using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SailScores.Database;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using SailScores.Core.Model;
using Microsoft.Extensions.Logging;
using Db = SailScores.Database.Entities;


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
            return await _dbContext.Scores.CountAsync(s => s.CompetitorId == competitorId)
                .ConfigureAwait(false);
        }

        public async Task<IList<Season>> GetSeasonsFor(Guid competitorId)
        {
            var clubId =
                (await _dbContext.Competitors
                .SingleAsync(c => c.Id == competitorId)
                .ConfigureAwait(false))
                .ClubId;
            var raceDates = _dbContext.Competitors
                .Where(c => c.Id == competitorId)
                .SelectMany(c => c.Scores)
                .Select(s => s.Race)
                .Select(r => r.Date);

            var seasons = _dbContext.Seasons
                .Where(s => s.ClubId == clubId)
                .Where(s => raceDates.Any(r => r.HasValue && r.Value > s.Start && r.Value <= s.End))
                .OrderBy(s => s.Start);

            return _mapper.Map<IList<Season>>(await seasons.ToListAsync()
                .ConfigureAwait(false));
        }

        public async Task<IList<Competitor>> GetSourceOptionsFor(Guid targetCompetitorId)
        {
            var targetComp = await _competitorService.GetCompetitorAsync(targetCompetitorId)
                .ConfigureAwait(false);

            var targetRaceIds = GetRacesForComp(targetCompetitorId);

            var sourceList =
                _dbContext.Competitors.Where(c => c.ClubId == targetComp.ClubId
                && c.Id != targetComp.Id
                && !(c.Scores.Any(s => targetRaceIds.Contains(s.RaceId))))
                .OrderBy(c => c.Name);

            return _mapper.Map<IList<Competitor>>(await sourceList.ToListAsync()
                .ConfigureAwait(false));

        }

        public async Task Merge(Guid targetCompetitorId, Guid sourceCompetitorId, string mergedBy)
        {
            _logger.LogInformation("Merging competitors {0} and {1}", targetCompetitorId, sourceCompetitorId);
            var scoresToMove = _dbContext.Scores
                .Where(s => s.CompetitorId == sourceCompetitorId);
            var seriesIds = await scoresToMove
                .Select(s => s.Race)
                .SelectMany(s => s.SeriesRaces)
                .Select(s => s.SeriesId)
                .Distinct()
                .ToListAsync()
                .ConfigureAwait(false);

            await scoresToMove.ForEachAsync(s => s.CompetitorId = targetCompetitorId)
                .ConfigureAwait(false);
            var sourceCompetitor = await _dbContext.Competitors
                .SingleAsync(c => c.Id == sourceCompetitorId)
                .ConfigureAwait(false);
            _dbContext.CompetitorChanges.Add(new Database.Entities.CompetitorChange
            {
                CompetitorId = targetCompetitorId,
                ChangeTypeId = Db.ChangeType.MergedId,
                ChangedBy = mergedBy,
                ChangeTimeStamp = DateTime.UtcNow,
                Summary = $"Merged {sourceCompetitor.SailNumber} : {sourceCompetitor.Name} : {sourceCompetitor.BoatName} into this competitor."
            });
            await _dbContext.SaveChangesAsync()
                .ConfigureAwait(false);

            await _competitorService.DeleteCompetitorAsync(sourceCompetitorId)
                .ConfigureAwait(false);


            foreach (var seriesId in seriesIds)
            {
                await _seriesService.UpdateSeriesResults(seriesId, mergedBy, false)
                    .ConfigureAwait(false);
            }
            foreach (var seriesId in seriesIds)
            {
                await _seriesService.UpdateParentSeriesResults(seriesId, mergedBy)
                    .ConfigureAwait(false);
            }
        }

        private IQueryable<Guid> GetRacesForComp(Guid competitorId)
        {
            return _dbContext.Scores.Where(s => s.CompetitorId == competitorId).Select(s => s.RaceId);
        }
    }
}
