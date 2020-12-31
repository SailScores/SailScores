using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SailScores.Core.Model;
using SailScores.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using dbObj = SailScores.Database.Entities;

namespace SailScores.Core.Services
{
    public class DbObjectBuilder : IDbObjectBuilder
    {

        private readonly ISailScoresContext _dbContext;
        private readonly IMapper _mapper;

        public DbObjectBuilder(
            ISailScoresContext dbContext,
            IMapper mapper)
        {
            _dbContext = dbContext;
            _mapper = mapper;
        }

        public async Task<dbObj.Regatta> BuildDbRegattaAsync(Model.Regatta regatta)
        {
            var retObj = _mapper.Map<dbObj.Regatta>(regatta);
            if (retObj.RegattaSeries == null)
            {
                retObj.RegattaSeries = new List<dbObj.RegattaSeries>();
            }
            if (regatta.Series != null)
            {
                foreach (var series in regatta.Series)
                {
                    series.ClubId = regatta.ClubId;
                    var dbSeries = await BuildDbSeriesAsync(series)
                        .ConfigureAwait(false);
                    retObj.RegattaSeries.Add(new dbObj.RegattaSeries
                    {
                        Regatta = retObj,
                        Series = dbSeries
                    });
                }
            }

            if (retObj.RegattaFleet == null)
            {
                retObj.RegattaFleet = new List<dbObj.RegattaFleet>();
            }
            if (regatta.Fleets != null)
            {
                foreach (var fleet in regatta.Fleets)
                {
                    retObj.RegattaFleet.Add(new dbObj.RegattaFleet
                    {
                        Regatta = retObj,
                        FleetId = fleet.Id
                    });
                }
            }

            var dbSeason = await GetSeasonAsync(regatta.ClubId, regatta)
                .ConfigureAwait(false);

            retObj.Season = dbSeason;

            return retObj;
        }


        public async Task<dbObj.Series> BuildDbSeriesAsync(Model.Series series)
        {
            var retObj = _mapper.Map<dbObj.Series>(series);
            if (retObj.RaceSeries == null)
            {
                retObj.RaceSeries = new List<dbObj.SeriesRace>();
            }
            if (series.Races != null)
            {
                foreach (var race in series.Races)
                {
                    var dbRace = await BuildDbRaceObj(series.ClubId, race)
                        .ConfigureAwait(false);
                    retObj.RaceSeries.Add(new dbObj.SeriesRace
                    {
                        Series = retObj,
                        Race = dbRace
                    });
                }
            }

            var dbSeason = await GetSeasonAsync(series.ClubId, series)
                .ConfigureAwait(false);

            retObj.Season = dbSeason;

            return retObj;
        }


        public async Task<dbObj.Race> BuildDbRaceObj(Guid clubId, Race race)
        {
            var dbRace = _mapper.Map<dbObj.Race>(race);
            dbRace.ClubId = clubId;
            dbRace.Scores = new List<dbObj.Score>();
            // add scores
            foreach (var score in race.Scores)
            {
                var dbScore = _mapper.Map<dbObj.Score>(score);
                if (!String.IsNullOrWhiteSpace(dbScore.Code))
                {
                    dbScore.Place = null;
                }
                dbScore.Competitor = await FindOrBuildCompetitorAsync(clubId, score.Competitor)
                    .ConfigureAwait(false);
                dbRace.Scores.Add(dbScore);
                if (race.Fleet?.FleetType == Api.Enumerations.FleetType.SelectedBoats)
                {
                    await EnsureCompetitorIsInFleet(dbScore.Competitor, race.Fleet)
                        .ConfigureAwait(false);
                }
            }

            return dbRace;
        }

        private async Task EnsureCompetitorIsInFleet(dbObj.Competitor competitor, Fleet fleet)
        {
            var dbFleet = await _dbContext.Fleets.SingleAsync(f => f.Id == fleet.Id)
                .ConfigureAwait(false);
            var Exists = dbFleet.CompetitorFleets != null
                && dbFleet.CompetitorFleets.Any(cf => cf.CompetitorId == competitor.Id);
            if (!Exists)
            {
                if (dbFleet.CompetitorFleets == null)
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
            Guid clubId,
            Competitor competitor)
        {
            var existingCompetitors = _dbContext.Competitors.Local
                .Where(c => c.ClubId == clubId);
            foreach (var currentDbComp in existingCompetitors)
            {
                if (AreCompetitorsMatch(competitor, currentDbComp))
                {
                    return currentDbComp;
                }
            }

            var dbComp = _mapper.Map<dbObj.Competitor>(competitor);
            dbComp.ClubId = clubId;
            _dbContext.Competitors.Add(dbComp);
            return dbComp;
        }

        private bool AreCompetitorsMatch(
            Competitor competitor,
            dbObj.Competitor dbComp)
        {
            bool matchFound;

            matchFound = competitor.Id != Guid.Empty
                         && competitor.Id == dbComp.Id; 

            matchFound = matchFound || !(String.IsNullOrWhiteSpace(competitor.SailNumber))
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


        private async Task<dbObj.Season> GetSeasonAsync(Guid clubId, Regatta regatta)
        {
            dbObj.Season retSeason = null;
            if (regatta.Season != null)
            {
                retSeason = await _dbContext.Seasons
                    .FirstOrDefaultAsync(s =>
                        s.ClubId == clubId
                        && (s.Id == regatta.Season.Id
                            || s.Start == regatta.Season.Start))
                    .ConfigureAwait(false);
            }
            if (retSeason == null)
            {
                retSeason = await GetSeason(clubId, regatta.StartDate, regatta.EndDate, true)
                    .ConfigureAwait(false);
            }
            return retSeason;
        }

        private async Task<dbObj.Season> GetSeasonAsync(Guid clubId, Series series)
        {
            dbObj.Season retSeason = null;
            if (series.Season != null)
            {
                retSeason = await _dbContext.Seasons
                    .FirstOrDefaultAsync(s =>
                        s.ClubId == clubId
                        && (s.Id == series.Season.Id
                            || s.Start == series.Season.Start))
                    .ConfigureAwait(false);
            }
            if (retSeason == null)
            {
                DateTime? firstDate = series.Races?.Min(r => r.Date);
                DateTime? lastDate = series.Races?.Max(r => r.Date);
                retSeason = await GetSeason(clubId, firstDate, lastDate, true)
                    .ConfigureAwait(false);
            }
            return retSeason;
        }

        private async Task<dbObj.Season> GetSeason(
            Guid clubId,
            DateTime? minDate,
            DateTime? maxDate,
            bool createNew)
        {
            var minDateToUse = minDate ?? DateTime.Today;
            var maxDateToUse = maxDate ?? DateTime.Today;
            var retObj = await _dbContext.Seasons
                .FirstOrDefaultAsync(
                s => s.ClubId == clubId
                && s.Start <= minDateToUse
                && s.End >= maxDateToUse)
                .ConfigureAwait(false);
            if (retObj == null && createNew)
            {
                retObj = CreateNewSeason(clubId, minDateToUse, minDateToUse);
            }

            return retObj;
        }

        private dbObj.Season CreateNewSeason(Guid clubId, DateTime minDate, DateTime maxDate)
        {
            DateTime beginning = GetStartOfYear(minDate);
            DateTime end = GetStartOfYear(maxDate).AddYears(1);

            var season = new dbObj.Season
            {
                ClubId = clubId,
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
