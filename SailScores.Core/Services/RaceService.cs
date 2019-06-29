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

namespace SailScores.Core.Services
{
    public class RaceService : IRaceService
    {
        private readonly ISailScoresContext _dbContext;
        private readonly ISeriesService _seriesService;
        private readonly IMapper _mapper;

        public RaceService(
            ISailScoresContext dbContext,
            ISeriesService seriesService,
            IMapper mapper)
        {
            _dbContext = dbContext;
            _seriesService = seriesService;
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
                .Where(r => r.Club.Id == clubId)
                .Include(r => r.Fleet)
                .Include( r => r.Scores)
                .Include( r => r.SeriesRaces)
                .ToListAsync()
                ).Where(r => (r.State ?? RaceState.Raced) == RaceState.Raced ||
                        (includeScheduled && (r.State == RaceState.Scheduled)) ||
                        (includeAbandoned && (r.State == RaceState.Abandoned)));
            var modelRaces = _mapper.Map<List<Model.Race>>(dbRaces);
            var dbSeries = (await _dbContext.Clubs
                .Include(c => c.Series)
                .FirstAsync(c => c.Id == clubId))
                .Series;
            var modelSeries = _mapper.Map<List<Model.Series>>(dbSeries);


            var dbSeasons = (await _dbContext.Clubs
                .Include(c => c.Seasons)
                .FirstAsync(c => c.Id == clubId))
                .Seasons;
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
                .FirstOrDefaultAsync(c => c.Id == raceId);
            if (race == null)
            {
                return null;
            }
            var modelRace = _mapper.Map<Model.Race>(race);
            
            await PopulateCompetitors(modelRace);

            var dbSeason = _dbContext.Clubs
                .Include(c => c.Seasons)
                .First(c => c.Id == race.ClubId)
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
            if (race.Id != default(Guid)) {
                dbRace = await _dbContext.Races.SingleOrDefaultAsync(r => r.Id == race.Id);
                seriesIdsToUpdate = dbRace.SeriesRaces.Select(r => r.SeriesId).ToList();
            } else
            {
                dbRace = new Db.Race
                {
                    Id = Guid.NewGuid(),
                    ClubId = race.ClubId
                };
                _dbContext.Races.Add(dbRace);
            }
            dbRace.Name = race.Name;
            dbRace.Order = race.Order;
            dbRace.Date = race.Date;
            dbRace.Description = race.Description;
            dbRace.State = race.State;
            dbRace.TrackingUrl = race.TrackingUrl;
            dbRace.UpdatedDate = DateTime.UtcNow;

            if(race.FleetId != null)
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
                    dbRace.Scores.Add(new Db.Score
                    {
                        Id = Guid.NewGuid(),
                        CompetitorId = score.CompetitorId,
                        Race = dbRace,
                        Place = score.Place,
                        Code = score.Code
                    });
                }

            }
            await _dbContext.SaveChangesAsync();
            seriesIdsToUpdate = seriesIdsToUpdate.Union(dbRace.SeriesRaces.Select(rs => rs.SeriesId).ToList());
            foreach (var seriesId in seriesIdsToUpdate)
            {
                await _seriesService.UpdateSeriesResults(seriesId);
            }
            return dbRace.Id;
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
    }
}
