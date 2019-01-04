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

        public async Task<Model.Race> GetRaceAsync(Guid id)
        {
            var race = await 
                _dbContext
                .Races
                .Include(r => r.Fleet)
                .Include(r => r.Scores)
                .FirstOrDefaultAsync(c => c.Id == id);

            var dtoRace = _mapper.Map<Model.Race>(race);
            await PopulateCompetitors(dtoRace);

            return dtoRace;

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
    }
}
