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
        private readonly IScoringCalculatorFactory _scoringCalculatorFactory;
        private readonly IScoringService _scoringService;
        private readonly ISailScoresContext _dbContext;
        private readonly IMapper _mapper;

        public SeriesService(
            IScoringCalculatorFactory scoringCalculatorFactory,
            IScoringService scoringService,
            ISailScoresContext dbContext,
            IMapper mapper)
        {
            _scoringCalculatorFactory = scoringCalculatorFactory;
            _scoringService = scoringService;
            _dbContext = dbContext;
            _mapper = mapper;
        }

        public async Task<IList<Series>> GetAllSeriesAsync(Guid clubId,
            DateTime? date)
        {
            var seriesDb = await _dbContext
                .Clubs
                .Where(c => c.Id == clubId)
                .SelectMany(c => c.Series)
                .Where(s => date == null ||
                    (s.Season.Start <= date && s.Season.End > date))
                .Include(s => s.Season)
                .Include(s => s.RaceSeries)
                    .ThenInclude(rs => rs.Race)
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
                   c.Initials == clubInitials
                )).Id;
            var seriesDb = await _dbContext
                .Series
                .SingleAsync(s => s.ClubId == clubId
                    && s.Name == seriesName
                  && s.Season.Name == seasonName);

            await UpdateSeriesResults(seriesDb.Id);
        }

        public async Task UpdateSeriesResults(
            Guid seriesId)
        {
            var dbSeries = await _dbContext
                .Series
                .Include(s => s.RaceSeries)
                    .ThenInclude(rs => rs.Race)
                        .ThenInclude(r => r.Scores)
                    .Include(s => s.Season)
                .SingleAsync(s => s.Id == seriesId);

            var fullSeries = _mapper.Map<Series>(dbSeries);
            var dbScoringSystem = await _scoringService.GetScoringSystemAsync(
                fullSeries); 
            
            var calculator = _scoringCalculatorFactory
                .CreateScoringCalculator(_mapper.Map<ScoringSystem>(dbScoringSystem));
            
            fullSeries.Races = fullSeries.Races.Where(r => r != null).ToList();
            await PopulateCompetitorsAsync(fullSeries);

            var results = calculator.CalculateResults(fullSeries);
            fullSeries.Results = results;
            dbSeries.UpdatedDate = DateTime.UtcNow;
            await SaveHistoricalResults(fullSeries);
        }

        public async Task<Series> GetSeriesDetailsAsync(
            string clubInitials,
            string seasonName,
            string seriesName)
        {
            var clubId = (await _dbContext.Clubs
                .SingleAsync( c =>
                    c.Initials == clubInitials
                )).Id;
            var seriesDb = await _dbContext
                .Series
                .Where(s =>
                    s.ClubId == clubId)
                .SingleAsync(s => s.Name == seriesName
                                  && s.Season.Name == seasonName);

            var fullSeries = _mapper.Map<Series>(seriesDb);
            
            var flatResults = await GetHistoricalResults(fullSeries);
            if(flatResults == null)
            {
                await UpdateSeriesResults(seriesDb.Id);
                flatResults = await GetHistoricalResults(fullSeries);
            }
            fullSeries.FlatResults = flatResults;

            return fullSeries;
        }

        private async Task SaveHistoricalResults(Series series)
        {
            DateTime currentDate = DateTime.Today;
            FlatModel.FlatResults results = FlattenResults(series);

            var oldResults = await _dbContext
                .HistoricalResults
                .Where(r => r.SeriesId == series.Id).ToListAsync();
            oldResults.ForEach(r => r.IsCurrent = false);

            var todayPrevious = oldResults
                .Where(r => r.Created >= currentDate)
                .ToList();
            todayPrevious.ForEach(r => _dbContext.HistoricalResults.Remove(r));

            var older = oldResults
                .Where(r => r.Created < currentDate)
                .OrderByDescending(r => r.Created)
                .Skip(1)
                .ToList();
            older.ForEach(r => _dbContext.HistoricalResults.Remove(r));

            _dbContext.HistoricalResults.Add(new dbObj.HistoricalResults
            {
                SeriesId = series.Id,
                Results = Newtonsoft.Json.JsonConvert.SerializeObject(results),
                IsCurrent = true,
                Created = DateTime.Now
            });

            await _dbContext.SaveChangesAsync();
        }

        private async Task<FlatResults> GetHistoricalResults(Series series)
        {
            var dbRow = await _dbContext.HistoricalResults
                .SingleOrDefaultAsync(r =>
                    r.SeriesId == series.Id
                    && r.IsCurrent);

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
                NumberOfDiscards = series.Results.NumberOfDiscards,
                NumberOfSailedRaces = series.Results.SailedRaces.Count()
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
                        Description = r.Description,
                        State = r.State
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
            var compIds = series.Races
                .Where(r => r != null)
                .SelectMany(r => r.Scores)
                .Select(s => s.CompetitorId);
            var dbCompetitors = await _dbContext.Competitors.Where(c => compIds.Contains(c.Id)).ToListAsync();

            series.Competitors = _mapper.Map<IList<Competitor>>(dbCompetitors);

            foreach(var score in series.Races
                .Where(r => r != null).SelectMany(r => r.Scores))
            {
                score.Competitor = series.Competitors.First(c => c.Id == score.CompetitorId);
            }
        }

        public async Task SaveNewSeries(Series series, Club club)
        {
            series.ClubId = club.Id;
            await SaveNewSeries(series);
        }

        public async Task SaveNewSeries(Series series)
        {
            Database.Entities.Series dbSeries = await BuildDbSeriesAsync(series);
            dbSeries.Name = RemoveDisallowedCharacters(series.Name);
            dbSeries.UpdatedDate = DateTime.UtcNow;
            if (dbSeries.Season == null && series.Season.Id != Guid.Empty && series.Season.Start != default(DateTime))
            {
                var season = _mapper.Map<dbObj.Season>(series.Season);
                _dbContext.Seasons.Add(season);
                dbSeries.Season = season;
            }
            if (dbSeries.Season == null)
            {
                throw new InvalidOperationException("Could not find or create season for new series.");
            }

            if (_dbContext.Series.Any(s =>
                s.Id == series.Id
                || (s.ClubId == series.ClubId
                    && s.Name == series.Name
                    && s.Season.Id == series.Season.Id)))
            {
                throw new InvalidOperationException("Cannot create series. A series with this name in this season already exists.");
            }

            _dbContext.Series.Add(dbSeries);
            await _dbContext.SaveChangesAsync();

            await UpdateSeriesResults(dbSeries.Id);
        }

        private async Task<dbObj.Series> BuildDbSeriesAsync(Model.Series series)
        {
            var retObj = _mapper.Map<dbObj.Series>(series);
            if(retObj.RaceSeries == null) 
            {
                retObj.RaceSeries = new List<dbObj.SeriesRace>();
            }
            if (series.Races != null)
            {
                foreach (var race in series.Races)
                {
                    var dbRace = await BuildDbRaceObj(series.ClubId, race);
                    retObj.RaceSeries.Add(new dbObj.SeriesRace
                    {
                        Series = retObj,
                        Race = dbRace
                    });
                }
            }

            var dbSeason = await GetSeasonAsync(series.ClubId, series);

            retObj.Season = dbSeason;

            return retObj;
        }


        private string RemoveDisallowedCharacters(string str)
        {
            var charsToRemove = new string[] { ":", "/", "?", "#", "[", "]", "@", "!", "$", "&", "'", "(", ")", "*", "+", ",", ";", "=" };
            foreach (var c in charsToRemove)
            {
                str = str.Replace(c, string.Empty);
            }
            return str;
        }

        private async Task<dbObj.Race> BuildDbRaceObj(Guid clubId, Race race)
        {
            var dbRace = _mapper.Map<dbObj.Race>(race);
            dbRace.ClubId = clubId;
            dbRace.Scores = new List<dbObj.Score>();
            // add scores
            foreach(var score in race.Scores)
            {
                var dbScore = _mapper.Map<dbObj.Score>(score);
                if (!String.IsNullOrWhiteSpace(dbScore.Code))
                {
                    dbScore.Place = null;
                }
                dbScore.Competitor = await FindOrBuildCompetitorAsync(clubId, score.Competitor);
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
            Guid clubId,
            Competitor competitor)
        {
            var existingCompetitors = _dbContext.Competitors.Local
                .Where(c => c.ClubId == clubId);
            foreach(var currentDbComp in existingCompetitors)
            {
                if(AreCompetitorsMatch(competitor, currentDbComp))
                {
                    return currentDbComp;
                }
            }

            var dbComp = _mapper.Map<dbObj.Competitor>(competitor);
            dbComp.ClubId = clubId;
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

        private async Task<dbObj.Season> GetSeasonAsync(Guid clubId, Series series)
        {
            dbObj.Season retSeason = null;
            if (series.Season != null)
            {
                retSeason = await _dbContext.Seasons
                    .FirstOrDefaultAsync(s =>
                        s.ClubId == clubId
                        && ( s.Id == series.Season.Id
                            || s.Start == series.Season.Start));
            }
            if (retSeason == null)
            {
                DateTime? firstDate = series.Races?.Min(r => r.Date);
                DateTime? lastDate = series.Races?.Max(r => r.Date);
                retSeason = await GetSeason(clubId, firstDate, lastDate, true);
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
                && s.End > maxDateToUse);
            if(retObj == null && createNew)
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

        public async Task Update(Series model)
        {
            if (_dbContext.Series.Any(s =>
                s.Id != model.Id
                && s.ClubId == model.ClubId
                && s.Name == model.Name
                && s.Season.Id == model.Season.Id))
            {
                throw new InvalidOperationException("Cannot update series. A series with this name in this season already exists.");
            }
            var existingSeries = await _dbContext.Series
                .Include(f => f.RaceSeries)
                .SingleAsync(c => c.Id == model.Id);

            existingSeries.Name = RemoveDisallowedCharacters(model.Name);
            existingSeries.Description = model.Description;
            existingSeries.IsImportantSeries = model.IsImportantSeries;

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

            await UpdateSeriesResults(existingSeries.Id);
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
