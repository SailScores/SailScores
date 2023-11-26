using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SailScores.Database;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using SailScores.Core.Model;
using Db = SailScores.Database.Entities;
using SailScores.Api.Dtos;
using SailScores.Api.Enumerations;
using Microsoft.Extensions.DependencyInjection;
using SailScores.Core.JobQueue;
using Microsoft.Extensions.Logging;

namespace SailScores.Core.Services
{
    public class RaceService : IRaceService
    {
        private readonly ISailScoresContext _dbContext;
        private readonly ISeriesService _seriesService;
        private readonly IMapper _mapper;


        private readonly ILogger _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public IBackgroundTaskQueue Queue { get; }

        public RaceService(
            ISailScoresContext dbContext,
            ISeriesService seriesService,
            IBackgroundTaskQueue queue,
            ILogger<IRaceService> logger,
            IServiceScopeFactory serviceScopeFactory,
            IMapper mapper)
        {
            _dbContext = dbContext;
            _seriesService = seriesService;
            _logger = logger;
            Queue = queue;

            _serviceScopeFactory = serviceScopeFactory;
            _mapper = mapper;
        }

        public async Task<bool> HasRacesAsync(Guid clubId)
        {
            return await _dbContext.Races
                .AnyAsync(r => r.ClubId == clubId)
                .ConfigureAwait(false);
        }

        public async Task<IList<Model.Race>> GetRacesAsync(Guid clubId)
        {
            var dbObjects = await _dbContext
                .Races
                .Where(r => r.ClubId == clubId)
                .OrderByDescending(r => r.Date)
                .ThenBy(r => r.Order)
                .Include(r => r.Fleet)
                .ToListAsync()
                .ConfigureAwait(false);
            return _mapper.Map<List<Model.Race>>(dbObjects);
        }


        public async Task<IList<Model.Race>> GetRecentRacesAsync(
            Guid clubId,
            int daysBack)
        {
            var cutoffDate = DateTime.Today.AddDays(0 - Math.Abs(daysBack));
            var dbObjects = await _dbContext
                .Races
                .Where(r => r.ClubId == clubId
                && r.Date >= cutoffDate)
                .OrderByDescending(r => r.Date)
                .ThenBy(r => r.Order)
                .Include(r => r.Fleet)
                .ToListAsync()
                .ConfigureAwait(false);
            return _mapper.Map<List<Model.Race>>(dbObjects);
        }

        public async Task<IList<Model.Race>> GetFullRacesAsync(
            Guid clubId,
            string seasonName,
            bool includeScheduled = true,
            bool includeAbandoned = true)
        {
            var seasonToUse = await GetSeasonAsync(clubId, seasonName)
                .ConfigureAwait(false);
            var startDate = seasonToUse?.Start ?? DateTime.Today.AddYears(-5);
            var endDate = seasonToUse?.End ?? DateTime.Today.AddYears(1);

            var dbRaces = await _dbContext.Races
                .Where(r => r.ClubId == clubId
                    && r.Date >= startDate
                    && r.Date <= endDate)
                .Where(r => r.State == RaceState.Raced || r.State == null ||
                    r.State == RaceState.Abandoned ||
                        (includeScheduled && (r.State == RaceState.Scheduled)) ||
                        (includeAbandoned && (r.State == RaceState.Abandoned)))
                .Include(r => r.Fleet)
                .Include(r => r.Scores)
                .Include(r => r.SeriesRaces)
                .AsSplitQuery()
                .ToListAsync()
                .ConfigureAwait(false);

            var modelRaces = _mapper.Map<List<Model.Race>>(dbRaces);

            var dbSeries = await _dbContext.Series
                .Where(s => s.ClubId == clubId).ToListAsync()
                .ConfigureAwait(false);
            var modelSeries = _mapper.Map<List<Model.Series>>(dbSeries);

            foreach (var race in modelRaces)
            {
                race.Series = modelSeries.Where(s => dbRaces
                        .First(r => r.Id == race.Id)
                        .SeriesRaces.Any(sr => sr.SeriesId == s.Id))
                    .ToList();
                race.Season = _mapper.Map<Season>(seasonToUse);
            }
            return modelRaces;
        }

        private async Task<Season> GetSeasonAsync(Guid clubId, string seasonName)
        {
            if (String.IsNullOrWhiteSpace(seasonName))
            {
                return await GetMostRecentRaceSeasonAsync(clubId).ConfigureAwait(false);
            }
            var dbSeason = await _dbContext.Seasons.FirstOrDefaultAsync(s =>
                s.ClubId == clubId && s.UrlName == seasonName)
                .ConfigureAwait(false);

            return _mapper.Map<Season>(dbSeason);
        }

        public async Task<Season> GetMostRecentRaceSeasonAsync(Guid clubId)
        {
            var race = await _dbContext.Races
                .Where(r => r.ClubId == clubId)
                .OrderByDescending(r => r.Date)
                .FirstOrDefaultAsync()
                .ConfigureAwait(false);

            Db.Season dbSeason;
            if (race == null)
            {
                dbSeason = await _dbContext.Seasons
                    .Where(s => s.ClubId == clubId
                        && s.Start <= DateTime.UtcNow)
                    .OrderByDescending(s => s.Start)
                    .FirstOrDefaultAsync()
                    .ConfigureAwait(false);
            }
            else
            {
                dbSeason = await _dbContext.Seasons
                    .Where(s => s.ClubId == clubId)
                    .OrderByDescending(s => s.Start)
                    .FirstOrDefaultAsync(s => s.Start <= race.Date)
                    .ConfigureAwait(false);
            }
            if (dbSeason == null)
            {
                dbSeason = await _dbContext.Seasons
                    .Where(s => s.ClubId == clubId)
                    .OrderByDescending(s => s.Start)
                    .FirstOrDefaultAsync()
                    .ConfigureAwait(false);
            }
            return _mapper.Map<Season>(dbSeason);
        }

        public async Task<Model.Race> GetRaceAsync(Guid raceId)
        {
            var race = await
                _dbContext
                .Races
                .Include(r => r.Fleet)
                .Include(r => r.Scores)
                .Include(r => r.Weather)
                .FirstOrDefaultAsync(c => c.Id == raceId)
                .ConfigureAwait(false);
            if (race == null)
            {
                return null;
            }
            var modelRace = _mapper.Map<Model.Race>(race);

            await PopulateCompetitors(modelRace)
                .ConfigureAwait(false);

            var dbSeason = _dbContext.Clubs
                .Include(c => c.Seasons)
                .Single(c => c.Id == race.ClubId)
                .Seasons
                .SingleOrDefault(s => race.Date.HasValue
                       && s.Start <= race.Date
                       && s.End >= race.Date);
            modelRace.Season = _mapper.Map<Model.Season>(dbSeason);

            var dbSeries = _dbContext.Clubs
                .Where(c => c.Id == race.ClubId)
                .SelectMany(c => c.Series)
                .Where(s => s.RaceSeries.Any(rs => rs.RaceId == race.Id));
            var modelSeries = _mapper.Map<List<Model.Series>>(dbSeries);
            modelRace.Series = modelSeries;

            return modelRace;

        }

        private async Task PopulateCompetitors(Race race)
        {
            if (race?.Scores == null || !race.Scores.Any())
            {
                return;
            }
            var compIds = race.Scores
                .Select(s => s.CompetitorId);
            var dbCompetitors = await _dbContext.Competitors
                .Where(c => compIds.Contains(c.Id))
                .ToListAsync()
                .ConfigureAwait(false);

            var dtoComps = _mapper.Map<IList<Competitor>>(dbCompetitors);

            foreach (var score in race.Scores)
            {
                score.Competitor = dtoComps.First(c => c.Id == score.CompetitorId);
            }
        }

        public Task<Guid> SaveAsync(RaceDto race)
        {
            if (race == null)
            {
                throw new ArgumentNullException(nameof(race));
            }

            return SaveInternalAsync(race);
        }

        private async Task<Guid> SaveInternalAsync(RaceDto race)
        {
            var dbRace = new Db.Race
            {
                Id = Guid.NewGuid(),
                ClubId = race.ClubId
            };

            if (race.Id != default)
            {
                dbRace = await _dbContext.Races
                    .Include(r => r.Scores)
                    .Include(r => r.SeriesRaces)
                    .Include(r => r.Weather)
                    .AsSingleQuery()
                    .SingleAsync(r => r.Id == race.Id)
                    .ConfigureAwait(false);
            }
            IEnumerable<Guid> seriesIdsToUpdate =
                dbRace.SeriesRaces?.Select(r => r.SeriesId)?.ToList() ?? new List<Guid>();

            dbRace.Name = race.Name;
            dbRace.Order = race.Order;
            dbRace.Date = race.Date ?? DateTime.Today;
            dbRace.Description = race.Description;
            dbRace.State = race.State;
            dbRace.TrackingUrl = race.TrackingUrl;
            dbRace.UpdatedDate = DateTime.UtcNow;
            dbRace.UpdatedBy = race.UpdatedBy;

            PopulateWeather(race, dbRace);

            dbRace.Fleet = _dbContext.Fleets.SingleOrDefault(f => f.Id == race.FleetId);

            if (race.SeriesIds != null)
            {
                dbRace.SeriesRaces ??= new List<Db.SeriesRace>();
                dbRace.SeriesRaces.Clear();
                foreach (var seriesId in race.SeriesIds)
                {
                    dbRace.SeriesRaces.Add(new Db.SeriesRace
                    {
                        SeriesId = seriesId,
                        RaceId = dbRace.Id
                    });
                }
            }
            if (race.Scores != null)
            {
                dbRace.Scores ??= new List<Db.Score>();

                dbRace.Scores.Clear();
                foreach (var score in race.Scores)
                {
                    var newScore = new Db.Score
                    {
                        Id = Guid.NewGuid(),
                        CompetitorId = score.CompetitorId,
                        Race = dbRace,
                        Place = score.Place,
                        Code = score.Code,
                        CodePoints = score.CodePoints
                    };

                    _dbContext.Scores.Add(newScore);
                }
            }
            if (race.Id == default)
            {
                _dbContext.Races.Add(dbRace);
            }
            else
            {
                _dbContext.Races.Update(dbRace);
            }

            await _dbContext.SaveChangesAsync()
                .ConfigureAwait(false);
            seriesIdsToUpdate = seriesIdsToUpdate.Union(
                dbRace.SeriesRaces?.Select(rs => rs.SeriesId).ToList() ?? new List<Guid>());

            foreach (var seriesId in seriesIdsToUpdate)
            {
                await _seriesService.UpdateSeriesResults(seriesId, race.UpdatedBy);
                // background processing after returning response used to be:
                // AddUpdateSeriesJob(seriesId, race.UpdatedBy);
            }

            return dbRace.Id;
        }

        private void PopulateWeather(RaceDto race, Db.Race dbRace)
        {
            if (dbRace.Weather == null)
            {
                var dbObj = _mapper.Map<Database.Entities.Weather>(race.Weather);
                dbRace.Weather = dbObj;
            }
            else
            {
                var id = dbRace.Weather.Id;
                var createdDate = dbRace.Weather.CreatedDate;
                _mapper.Map<WeatherDto, Database.Entities.Weather>(race.Weather, dbRace.Weather);
                dbRace.Weather.CreatedDate = createdDate;
                dbRace.Weather.Id = id;
            }
        }

        private void AddUpdateSeriesJob(
            Guid seriesId,
            string updatedBy)
        {
            Queue.QueueBackgroundWorkItem(async token =>
            {
                var guid = Guid.NewGuid().ToString();

                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var scopedServices = scope.ServiceProvider;
                    var seriesService = scopedServices.GetRequiredService<ISeriesService>();

                    try
                    {
                        // Do background-y stuff here.
                        await seriesService.UpdateSeriesResults(seriesId, updatedBy)
                            .ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "An error occurred writing to the " +
                            "database. Error: {Message}", ex.Message);
                    }
                }

                _logger.LogInformation(
                    "Queued Background Task {Guid} is complete. 3/3", guid);
            });
        }

        public async Task Delete(Guid raceId, string deletedBy)
        {
            var dbRace = await _dbContext
                .Races
                .Include(r => r.SeriesRaces)
                .SingleAsync(r => r.Id == raceId)
                .ConfigureAwait(false);
            _dbContext.Races.Remove(dbRace);
            await _dbContext.SaveChangesAsync()
                .ConfigureAwait(false);
            foreach (var seriesId in dbRace.SeriesRaces.Select(rs => rs.SeriesId))
            {
                await _seriesService.UpdateSeriesResults(seriesId, deletedBy)
                    .ConfigureAwait(false);
            }
        }

        public async Task<int> GetRaceCountAsync(
            Guid clubId,
            DateTime? raceDate,
            Guid fleetId)
        {
            return await _dbContext
                .Races
                .Where(r => r.ClubId == clubId && r.Fleet.Id == fleetId)
                .CountAsync(r => r.Date == raceDate)
                .ConfigureAwait(false);
        }
    }
}
