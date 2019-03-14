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
using SailScores.Core.FlatModel;

namespace SailScores.Core.Services
{
    public class SeriesService : ISeriesService
    {
        private readonly ISailScoresContext _dbContext;
        private readonly IMapper _mapper;

        public SeriesService(
            ISailScoresContext dbContext,
            IMapper mapper)
        {
            _dbContext = dbContext;
            _mapper = mapper;
        }

        public async Task<IEnumerable<Series>> GetAllSeriesAsync(Guid clubId)
        {
            var seriesDb = await _dbContext
                .Clubs
                .Where(c => c.Id == clubId)
                .SelectMany(c => c.Series)
                .Include(s => s.Season)
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

        public async Task UpdateSeriesResults(
            string clubInitials,
            string seasonName,
            string seriesName)
        {
            var clubId = (await _dbContext.Clubs
                .SingleAsync(c =>
                   c.Initials == clubInitials.ToUpperInvariant()
                )).Id;
            var seriesDb = await _dbContext
                .Series
                .Where(s =>
                    s.ClubId == clubId)
                .Include(s => s.RaceSeries)
                    .ThenInclude(rs => rs.Race)
                        .ThenInclude(r => r.Scores)
                    .Include(s => s.Season)
                .SingleAsync(s => s.Name == seriesName
                                  && s.Season.Name == seasonName);

            var dbScoringSystem = await _dbContext
                .ScoringSystems
                .Where(s => s.ClubId == clubId)
                .Include(s => s.ScoreCodes)
                .SingleAsync();
            var calculator = new SeriesCalculator(
                _mapper.Map<ScoringSystem>(dbScoringSystem));

            var returnObj = _mapper.Map<Series>(seriesDb);

            await PopulateCompetitorsAsync(returnObj);

            var results = calculator.CalculateResults(returnObj);
            returnObj.Results = results;
            await SaveHistoricalResults(returnObj);
        }

        public async Task<Series> GetSeriesDetailsAsync(
            string clubInitials,
            string seasonName,
            string seriesName)
        {
            var clubId = (await _dbContext.Clubs
                .SingleAsync( c =>
                    c.Initials == clubInitials.ToUpperInvariant()
                )).Id;
            var seriesDb = await _dbContext
                .Series
                .Where(s =>
                    s.ClubId == clubId)
                .SingleAsync(s => s.Name == seriesName
                                  && s.Season.Name == seasonName);

            var returnObj = _mapper.Map<Series>(seriesDb);
            
            var flatResults = await GetHistoricalResults(returnObj);
            if(flatResults == null)
            {
                await UpdateSeriesResults(clubInitials,
                    seasonName,
                    seriesName);
                flatResults = await GetHistoricalResults(returnObj);
            }
            returnObj.FlatResults = flatResults;

            return returnObj;
        }

        private async Task SaveHistoricalResults(Series series)
        {
            FlatModel.FlatResults results = FlattenResults(series);

            var oldResults = await _dbContext
                .HistoricalResults
                .Where(r => r.SeriesId == series.Id).ToListAsync();
            oldResults.ForEach(r => r.IsCurrent = false);

            _dbContext.HistoricalResults.Add(new dbObj.HistoricalResults
            {
                SeriesId = series.Id,
                Results = Newtonsoft.Json.JsonConvert.SerializeObject(results),
                IsCurrent = true
            });

            await _dbContext.SaveChangesAsync();
        }

        private async Task<FlatResults> GetHistoricalResults(Series series)
        {
            var dbRow = await _dbContext.HistoricalResults
                .SingleOrDefaultAsync(r =>
                    r.SeriesId == series.Id
                    && r.IsCurrent);
            if(dbRow == null)
            {
                dbRow = await _dbContext.HistoricalResults
                    .OrderByDescending(r => r.Created)
                    .FirstOrDefaultAsync(r =>
                        r.SeriesId == series.Id);
            }
            if(String.IsNullOrWhiteSpace(dbRow?.Results))
            {
                return null;
            }
            return Newtonsoft.Json.JsonConvert.DeserializeObject<FlatResults>(dbRow.Results);
        }

        private FlatResults FlattenResults(Series series)
        {
            var flatResults = new FlatResults
            {
                SeriesId = series.Id,
                Competitors = FlattenCompetitors(series),
                Races = FlattenRaces(series),
                CalculatedScores = FlattenSeriesScores(series),
                NumberOfDiscards = series.Results.NumberOfDiscards
            };
            return flatResults;
        }

        private IEnumerable<FlatSeriesScore> FlattenSeriesScores(Series series)
        {
            return series.Results.Results.Select(
                kvp => new FlatSeriesScore
                {
                    CompetitorId = kvp.Key.Id,
                    Rank = kvp.Value.Rank,
                    TotalScore = kvp.Value.TotalScore,
                    Scores = FlattenScores(kvp.Value)
                });
        }

        private IEnumerable<FlatCalculatedScore> FlattenScores(SeriesCompetitorResults results)
        {
            return results.CalculatedScores.Select(
                s => new FlatCalculatedScore
                {
                    RaceId = s.Key.Id,
                    Place = s.Value.RawScore.Place,
                    Code = s.Value.RawScore.Code,
                    ScoreValue = s.Value.ScoreValue,
                    Discard = s.Value.Discard
                });
        }

        private IEnumerable<FlatRace> FlattenRaces(Series series)
        {
            return series.Races
                .OrderBy(r => r.Date)
                .ThenBy(r => r.Order)
                .Select(r =>
                    new FlatRace
                    {
                        Id = r.Id,
                        Name = r.Name,
                        Date = r.Date,
                        Order = r.Order,
                        Description = r.Description
                    });
        }

        private IEnumerable<FlatCompetitor> FlattenCompetitors(Series series)
        {
            return series.Competitors
                .OrderBy(c => series.Results.Results[c].Rank)
                .Select(c =>
                    new FlatCompetitor
                    {
                        Id = c.Id,
                        Name = c.Name,
                        SailNumber = c.SailNumber,
                        AlternativeSailNumber = c.AlternativeSailNumber,
                        BoatName = c.BoatName
                    });
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
            if(series.Season == null)
            {
                throw new InvalidOperationException("Series must have a season assigned.");
            }

            var season = _dbContext.Seasons.SingleOrDefault(s => s.Id == series.Season.Id);

            if(_dbContext.Series.Any( s =>
                s.Id == series.Id
                || (s.ClubId == series.ClubId
                    && s.Name == series.Name
                    && s.Season.Id == series.Season.Id)))
            {
                throw new InvalidOperationException("Cannot create series. A series with this name in this season already exists.");
            }
            
            Database.Entities.Series dbSeries = _mapper.Map<dbObj.Series>(series);
            if(season != null)
            {
                dbSeries.Season = season;
            } else if(series.Season.Id != Guid.Empty && series.Season.Start != default(DateTime))
            {
                season = _mapper.Map<dbObj.Season>(season);
                _dbContext.Seasons.Add(season);
                dbSeries.Season = season;
            } else
            {
                dbSeries.Season = null;
            }
            _dbContext.Series.Add(dbSeries);
            await _dbContext.SaveChangesAsync();
        }

        private async Task<dbObj.Series> BuildDbSeriesAsync(Model.Series ssSeries, Model.Club club)
        {
            var retObj = _mapper.Map<dbObj.Series>(ssSeries);
            if(retObj.RaceSeries == null) 
            {
                retObj.RaceSeries = new List<dbObj.SeriesRace>();
            }
            foreach(var race in ssSeries.Races)
            {
                var dbRace = await BuildDbRaceObj(club, race);
                retObj.RaceSeries.Add(new dbObj.SeriesRace
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

        public async Task Update(Series model)
        {
            var existingSeries = await _dbContext.Series
                .Include(f => f.RaceSeries)
                .SingleAsync(c => c.Id == model.Id);

            existingSeries.Name = model.Name;
            existingSeries.Description = model.Description;

            if(model.Season != null
                && model.Season.Id != Guid.Empty
                && existingSeries.Season?.Id != model.Season?.Id)
            {
                existingSeries.Season = _dbContext.Seasons.Single(s => s.Id == model.Season.Id);
            }

            var racesToRemove = new List<dbObj.SeriesRace>();

            if (model.Races != null)
            {
                racesToRemove =
                    existingSeries.RaceSeries
                    .Where(f => !(model.Races.Any(c => c.Id == f.RaceId)))
                    .ToList();
            }
            var racesToAdd =
                model.Races != null ?
                model.Races
                .Where(c =>
                    !(existingSeries.RaceSeries.Any(f => c.Id == f.RaceId)))
                    .Select(c => new dbObj.SeriesRace { RaceId = c.Id, SeriesId = existingSeries.Id })
                : new List<dbObj.SeriesRace>();

            foreach (var removingClass in racesToRemove)
            {
                existingSeries.RaceSeries.Remove(removingClass);
            }
            foreach (var addClass in racesToAdd)
            {
                existingSeries.RaceSeries.Add(addClass);
            }

            await _dbContext.SaveChangesAsync();
        }

        public async Task Delete(Guid fleetId)
        {
            var dbSeries = await _dbContext.Series
                .Include(f => f.RaceSeries)
                .SingleAsync(c => c.Id == fleetId);
            foreach (var link in dbSeries.RaceSeries.ToList())
            {
                dbSeries.RaceSeries.Remove(link);
            }
            _dbContext.Series.Remove(dbSeries);

            await _dbContext.SaveChangesAsync();
        }
    }
}
