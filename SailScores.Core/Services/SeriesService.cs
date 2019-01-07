using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SailScores.Core.Model;
using SailScores.Core.Scoring;
using SailScores.Database;
using dbObj = SailScores.Database.Entities;
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
                    .Include(s => s.Season)
                .SingleAsync(s => s.Name == seriesName
                                  && s.Season.Name == seasonName);

            var returnObj = _mapper.Map<Series>(seriesDb);

            await PopulateCompetitorsAsync(returnObj);

            var results = _seriesCalculator.CalculateResults(returnObj);
            returnObj.Results = results;
            return returnObj;
        }

        private async Task PopulateCompetitorsAsync(Series series)
        {
            var compIds = series.Races.SelectMany(r => r.Scores)
                .Select(s => s.CompetitorId);
            var dbCompetitors = await _dbContext.Competitors.Where(c => compIds.Contains(c.Id)).ToListAsync();

            series.Competitors = _mapper.Map<IList<Competitor>>(dbCompetitors);

            foreach(var score in series.Races.SelectMany(r => r.Scores))
            {
                score.Competitor = series.Competitors.First(c => c.Id == score.CompetitorId);
            }
        }

        public async Task SaveNewSeries(Series ssSeries, Club club)
        {
            Database.Entities.Series dbSeries = await BuildDbSeriesAsync(ssSeries, club);
            _dbContext.Series.Add(dbSeries);
            await _dbContext.SaveChangesAsync();
        }
        public async Task SaveNewSeries(Series series)
        {
            Database.Entities.Series dbSeries = _mapper.Map<dbObj.Series>(series);
            _dbContext.Series.Add(dbSeries);
            await _dbContext.SaveChangesAsync();
        }

        private async Task<dbObj.Series> BuildDbSeriesAsync(Model.Series ssSeries, Model.Club club)
        {
            var retObj = _mapper.Map<dbObj.Series>(ssSeries);
            if(retObj.RaceSeries == null) 
            {
                retObj.RaceSeries = new List<dbObj.SeriesRaces>();
            }
            foreach(var race in ssSeries.Races)
            {
                var dbRace = await BuildDbRaceObj(club, race);
                retObj.RaceSeries.Add(new dbObj.SeriesRaces
                {
                    Series = retObj,
                    Race = dbRace
                });
            }

            dbObj.Season dbSeason = await GetSeasonAsync(club, ssSeries);
            retObj.Season = dbSeason;

            return retObj;
        }

        private async Task<dbObj.Race> BuildDbRaceObj(Club club, Race race)
        {
            var dbRace = _mapper.Map<dbObj.Race>(race);
            dbRace.ClubId = club.Id;
            dbRace.Scores = new List<dbObj.Score>();
            // add scores
            foreach(var score in race.Scores)
            {
                var dbScore = _mapper.Map<dbObj.Score>(score);
                if (!String.IsNullOrWhiteSpace(dbScore.Code))
                {
                    dbScore.Place = null;
                }
                dbScore.Competitor = await FindOrBuildCompetitorAsync(club, score.Competitor);
                dbRace.Scores.Add(dbScore);
                if(race.Fleet?.FleetType == Api.Enumerations.FleetType.SelectedBoats)
                {
                    await EnsureCompetitorIsInFleet(dbScore.Competitor, race.Fleet);
                }
            }

            return dbRace;
        }

        private async Task EnsureCompetitorIsInFleet(dbObj.Competitor competitor, Fleet fleet)
        {
            var dbFleet = await _dbContext.Fleets.SingleAsync(f => f.Id == fleet.Id);
            var Exists = dbFleet.CompetitorFleets != null
                && dbFleet.CompetitorFleets.Any(cf => cf.CompetitorId == competitor.Id);
            if (!Exists)
            {
                if(dbFleet.CompetitorFleets == null)
                {
                    dbFleet.CompetitorFleets = new List<dbObj.CompetitorFleet>();
                }
                dbFleet.CompetitorFleets.Add(new dbObj.CompetitorFleet
                {
                    FleetId = dbFleet.Id,
                    CompetitorId = competitor.Id
                });
            }
        }

        private async Task<dbObj.Competitor> FindOrBuildCompetitorAsync(
            Club club,
            Competitor competitor)
        {
            var existingCompetitors = _dbContext.Competitors.Local
                .Where(c => c.ClubId == club.Id);
            foreach(var currentDbComp in existingCompetitors)
            {
                if(AreCompetitorsMatch(competitor, currentDbComp))
                {
                    return currentDbComp;
                }
            }

            var dbComp = _mapper.Map<dbObj.Competitor>(competitor);
            dbComp.ClubId = club.Id;
            _dbContext.Competitors.Add(dbComp);
            return dbComp;
        }

        private bool AreCompetitorsMatch(Competitor competitor, dbObj.Competitor dbComp)
        {
            bool matchFound = false;

            matchFound = !(String.IsNullOrWhiteSpace(competitor.SailNumber))
                && !(String.IsNullOrWhiteSpace(dbComp.SailNumber))
                && competitor.SailNumber.Equals(dbComp.SailNumber, StringComparison.InvariantCultureIgnoreCase);
            matchFound = matchFound || !(String.IsNullOrWhiteSpace(competitor.SailNumber))
                && !(String.IsNullOrWhiteSpace(dbComp.AlternativeSailNumber))
                && competitor.SailNumber.Equals(dbComp.AlternativeSailNumber, StringComparison.InvariantCultureIgnoreCase);

            matchFound = matchFound || !(String.IsNullOrWhiteSpace(competitor.AlternativeSailNumber))
                && !(String.IsNullOrWhiteSpace(dbComp.SailNumber))
                && competitor.AlternativeSailNumber.Equals(dbComp.SailNumber, StringComparison.InvariantCultureIgnoreCase);
            matchFound = matchFound || !(String.IsNullOrWhiteSpace(competitor.AlternativeSailNumber))
                && !(String.IsNullOrWhiteSpace(dbComp.AlternativeSailNumber))
                && competitor.AlternativeSailNumber.Equals(dbComp.AlternativeSailNumber, StringComparison.InvariantCultureIgnoreCase);
            return matchFound;

        }

        private async Task<dbObj.Season> GetSeasonAsync(Club club, Series ssSeries)
        {
            dbObj.Season retSeason = null;
            if (ssSeries.Season != null)
            {
                retSeason = await _dbContext.Seasons
                    .FirstOrDefaultAsync(s =>
                        s.ClubId == club.Id
                        && ( s.Id == ssSeries.Season.Id
                            || s.Start == ssSeries.Season.Start));
            }
            if (retSeason == null)
            {
                DateTime? firstDate = ssSeries.Races?.Min(r => r.Date);
                DateTime? lastDate = ssSeries.Races?.Max(r => r.Date);
                retSeason = await GetSeason(club, firstDate, lastDate, true);
            }
            return retSeason;
        }

        private async Task<dbObj.Season> GetSeason(
            Club club,
            DateTime? minDate,
            DateTime? maxDate,
            bool createNew)
        {
            var minDateToUse = minDate ?? DateTime.Today;
            var maxDateToUse = maxDate ?? DateTime.Today;
            var retObj = await _dbContext.Seasons
                .FirstOrDefaultAsync(
                s => s.ClubId == club.Id
                && s.Start <= minDateToUse
                && s.End > maxDateToUse);
            if(retObj == null && createNew)
            {
                retObj = CreateNewSeason(club, minDateToUse, minDateToUse);
            }

            return retObj;
        }

        private dbObj.Season CreateNewSeason(Club club, DateTime minDate, DateTime maxDate)
        {
            DateTime beginning = GetStartOfYear(minDate);
            DateTime end = GetStartOfYear(maxDate).AddYears(1);

            var season = new dbObj.Season
            {
                ClubId = club.Id,
                Name = $"{beginning.Year}",
                Start = beginning,
                End = end
            };
            return season;
        }

        private DateTime GetStartOfYear(DateTime minDate)
        {
            return new DateTime(minDate.Year, 1, 1);
        }
    }
}
