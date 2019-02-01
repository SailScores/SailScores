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

namespace SailScores.Core.Services
{
    public class RaceService : IRaceService
    {
        private readonly ISailScoresContext _dbContext;
        private readonly IMapper _mapper;

        public RaceService(
            ISailScoresContext dbContext,
            IMapper mapper)
        {
            _dbContext = dbContext;
            _mapper = mapper;
        }

        public async Task<IList<Model.Race>> GetRacesAsync(Guid clubId)
        {
            var dbObjects = await _dbContext
                .Clubs
                .Where(c => c.Id == clubId)
                .SelectMany(c => c.Races)
                .ToListAsync();
            return _mapper.Map<List<Model.Race>>(dbObjects);
        }
        public async Task<IList<Model.Race>> GetFullRacesAsync(Guid clubId)
        {
            var dbRaces = await _dbContext.Races
                .Where(r => r.Club.Id == clubId)
                .Include(r => r.Fleet)
                .Include( r => r.Scores)
                .Include( r => r.SeriesRaces)
                .ToListAsync();
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
            if (race.Id != default(Guid)) {
               dbRace = await _dbContext.Races.SingleOrDefaultAsync(r => r.Id == race.Id);
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
            return dbRace.Id;
        }

        public async Task Delete(Guid raceId)
        {
            var dbRace = await _dbContext
                .Races
                .SingleAsync(r => r.Id ==  raceId);
            _dbContext.Races.Remove(dbRace);
            await _dbContext.SaveChangesAsync();
        }
    }
}
