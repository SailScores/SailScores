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

        public async Task<IList<Model.Race>> GetRacesAsync(Guid clubId)
        {
            var dbObjects = await _dbContext
                .Races
                .Where(r => r.ClubId == clubId)
                .OrderByDescending(r => r.Date)
                .ThenBy(r => r.Order)
                .Include(r => r.Fleet)
                .ToListAsync();
            return _mapper.Map<List<Model.Race>>(dbObjects);
        }

        public async Task<IList<Model.Race>> GetFullRacesAsync(
            Guid clubId,
            bool includeScheduled = true,
            bool includeAbandoned = true)
        {
            var dbRaces = (await _dbContext.Races
                .Where(r => r.ClubId == clubId)
                .Include(r => r.Fleet)
                .Include( r => r.Scores)
                .Include( r => r.SeriesRaces)
                .ToListAsync()
                ).Where(r => (r.State ?? RaceState.Raced) == RaceState.Raced ||
                        (includeScheduled && (r.State == RaceState.Scheduled)) ||
                        (includeAbandoned && (r.State == RaceState.Abandoned)));
            var modelRaces = _mapper.Map<List<Model.Race>>(dbRaces);
            
            var dbSeries = await _dbContext.Series
                .Where(s => s.ClubId == clubId).ToListAsync();
            var modelSeries = _mapper.Map<List<Model.Series>>(dbSeries);

            var dbSeasons = await _dbContext.Seasons
                .Where(s => s.ClubId == clubId).ToListAsync();
            var modelSeasons = _mapper.Map<List<Model.Season>>(dbSeasons);

            foreach (var race in modelRaces)
            {
                race.Series = modelSeries.Where(s => dbRaces
                        .First(r => r.Id == race.Id)
                        .SeriesRaces.Any(sr => sr.SeriesId == s.Id))
                    .ToList();
                race.Season = modelSeasons
                    .SingleOrDefault(s =>
                        race.Date.HasValue
                        && s.Start <= race.Date
                        && s.End > race.Date);
            }
            return modelRaces;
        }

        public async Task<Model.Race> GetRaceAsync(Guid raceId)
        {
            var race = await 
                _dbContext
                .Races
                .Include(r => r.Fleet)
                .Include(r => r.Scores)
                .Include(r => r.Weather)
                .FirstOrDefaultAsync(c => c.Id == raceId);
            if (race == null)
            {
                return null;
            }
            var modelRace = _mapper.Map<Model.Race>(race);
            
            await PopulateCompetitors(modelRace);

            var dbSeason = _dbContext.Clubs
                .Include(c => c.Seasons)
                .Single(c => c.Id == race.ClubId)
                .Seasons
                .SingleOrDefault(s => race.Date.HasValue
                       && s.Start <= race.Date
                       && s.End > race.Date);
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
            var dbCompetitors = await _dbContext.Competitors.Where(c => compIds.Contains(c.Id)).ToListAsync();

            var dtoComps = _mapper.Map<IList<Competitor>>(dbCompetitors);
            
            foreach (var score in race.Scores)
            {
                score.Competitor = dtoComps.First(c => c.Id == score.CompetitorId);
            }
        }

        public async Task<Guid> SaveAsync(RaceDto race)
        {
            Db.Race dbRace;
            IEnumerable<Guid> seriesIdsToUpdate = new List<Guid>();
            bool addToContext = false;
            if (race.Id != default) {
                dbRace = await _dbContext.Races
                    .Include(r => r.Scores)
                    .Include(r => r.SeriesRaces)
                    .Include(r => r.Weather)
                    .SingleAsync(r => r.Id == race.Id);
                var seriesFromRace = dbRace.SeriesRaces?.Select(r => r.SeriesId)?.ToList();
                if(seriesFromRace != null)
                {
                    seriesIdsToUpdate = seriesFromRace;
                }
            } else
            {
                dbRace = new Db.Race
                {
                    Id = Guid.NewGuid(),
                    ClubId = race.ClubId
                };
                addToContext = true;
            }
            dbRace.Name = race.Name;
            dbRace.Order = race.Order;
            dbRace.Date = race.Date;
            dbRace.Description = race.Description;
            dbRace.State = race.State;
            dbRace.TrackingUrl = race.TrackingUrl;
            dbRace.UpdatedDate = DateTime.UtcNow;

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

            if (race.FleetId != null)
            {
                dbRace.Fleet = _dbContext.Fleets.SingleOrDefault(f => f.Id == race.FleetId);
            }
            if(race.SeriesIds != null)
            {
                if(dbRace.SeriesRaces == null)
                {
                    dbRace.SeriesRaces = new List<Db.SeriesRace>();
                }
                dbRace.SeriesRaces.Clear();
                foreach(var seriesId in race.SeriesIds)
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
                if (dbRace.Scores == null)
                {
                    dbRace.Scores = new List<Db.Score>();
                }
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

                    dbRace.Scores.Add(newScore);
                }
            }
            if (addToContext)
            {
                _dbContext.Races.Add(dbRace);
            }

            await _dbContext.SaveChangesAsync();
            seriesIdsToUpdate = seriesIdsToUpdate.Union(dbRace.SeriesRaces.Select(rs => rs.SeriesId).ToList());
            foreach (var seriesId in seriesIdsToUpdate)
            {
                //await _seriesService.UpdateSeriesResults(seriesId);

                AddUpdateSeriesJob(seriesId);
            }

            return dbRace.Id;
        }

        private void AddUpdateSeriesJob(Guid seriesId)
        {
            Queue.QueueBackgroundWorkItem(async token =>
            {
                var guid = Guid.NewGuid().ToString();

                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var scopedServices = scope.ServiceProvider;
                    var seriesService = scopedServices.GetRequiredService<ISeriesService>();
                    // fake delay to test?

                    //await Task.Delay(TimeSpan.FromSeconds(5), token);
                    try
                    {
                        // Do background-y stuff here.
                        await seriesService.UpdateSeriesResults(seriesId);
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

        public async Task Delete(Guid raceId)
        {
            var dbRace = await _dbContext
                .Races
                .Include(r => r.SeriesRaces)
                .SingleAsync(r => r.Id ==  raceId);
            _dbContext.Races.Remove(dbRace);
            await _dbContext.SaveChangesAsync();
            foreach(var seriesId in dbRace.SeriesRaces.Select(rs => rs.SeriesId))
            {
                await _seriesService.UpdateSeriesResults(seriesId);
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
                .CountAsync(r => r.Date == raceDate);
        }
    }
}
