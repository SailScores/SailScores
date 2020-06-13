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
        private readonly IConversionService _converter;
        private readonly IDbObjectBuilder _dbObjectBuilder;
        private readonly ISailScoresContext _dbContext;
        private readonly IMapper _mapper;

        public SeriesService(
            IScoringCalculatorFactory scoringCalculatorFactory,
            IScoringService scoringService,
            IConversionService converter,
            IDbObjectBuilder dbObjBuilder,
            ISailScoresContext dbContext,
            IMapper mapper)
        {
            _scoringCalculatorFactory = scoringCalculatorFactory;
            _scoringService = scoringService;
            _converter = converter;
            _dbObjectBuilder = dbObjBuilder;
            _dbContext = dbContext;
            _mapper = mapper;
        }

        public async Task<IList<Series>> GetAllSeriesAsync(
            Guid clubId,
            DateTime? date,
            bool includeRegattaSeries)
        {

            var regattaSeriesId = _dbContext.Regattas.SelectMany(r =>
            r.RegattaSeries).Select(rs => rs.SeriesId);

            var series = await _dbContext
                .Clubs
                .Where(c => c.Id == clubId)
                .SelectMany(c => c.Series)
                .Where(s => date == null ||
                    (s.Season.Start <= date && s.Season.End > date))
                .Include(s => s.Season)
                .Include(s => s.RaceSeries)
                    .ThenInclude(rs => rs.Race)
                .Where(s => includeRegattaSeries || !regattaSeriesId.Contains(s.Id))
                .OrderBy(s => s.Name).ToListAsync();

            var returnObj = _mapper.Map<List<Series>>(series);
            return returnObj;
        }

        public async Task<Series> GetOneSeriesAsync(Guid seriesId)
        {
            var seriesDb = await _dbContext
                .Series
                .Include(s => s.Season)
                .FirstAsync(c => c.Id == seriesId);

            var fullSeries = _mapper.Map<Series>(seriesDb);
            if (fullSeries != null)
            {
                var flatResults = await GetHistoricalResults(fullSeries);
                if (flatResults == null)
                {
                    await UpdateSeriesResults(seriesDb.Id);
                    flatResults = await GetHistoricalResults(fullSeries);
                }
                fullSeries.FlatResults = flatResults;
            }
            return fullSeries;
        }

        public async Task UpdateSeriesResults(
            Guid seriesId)
        {
            var dbSeries = await _dbContext
                .Series
                .Include(s => s.RaceSeries)
                    .ThenInclude(rs => rs.Race)
                        .ThenInclude(r => r.Weather)
                .Include(s => s.RaceSeries)
                    .ThenInclude(rs => rs.Race)
                        .ThenInclude(r => r.Scores)
                    .Include(s => s.Season)
                .SingleAsync(s => s.Id == seriesId);

            if (dbSeries.ResultsLocked ?? false)
            {
                return;
            }

            var fullSeries = _mapper.Map<Series>(dbSeries);
            await CalculateScoresAsync(fullSeries);
            dbSeries.UpdatedDate = DateTime.UtcNow;
            await SaveHistoricalResults(fullSeries);

            await SaveChartData(fullSeries);
        }

        private async Task CalculateScoresAsync(Series fullSeries)
        {
            var dbScoringSystem = await _scoringService.GetScoringSystemAsync(
                fullSeries);

            fullSeries.ScoringSystem = _mapper.Map<ScoringSystem>(dbScoringSystem);
            var calculator = await _scoringCalculatorFactory
                .CreateScoringCalculatorAsync(fullSeries.ScoringSystem);

            fullSeries.Races = fullSeries.Races.Where(r => r != null).ToList();
            await PopulateCompetitorsAsync(fullSeries);

            var results = calculator.CalculateResults(fullSeries);
            fullSeries.Results = results;
        }

        public async Task<Series> GetSeriesDetailsAsync(
            string clubInitials,
            string seasonName,
            string seriesUrlName)
        {
            var club = await _dbContext.Clubs
                .Where(c =>
                   c.Initials == clubInitials
                ).SingleAsync();
            var clubId = club.Id;
            var seriesDb = await _dbContext
                .Series
                .Where(s =>
                    s.ClubId == clubId)
                .SingleOrDefaultAsync(s => s.UrlName == seriesUrlName
                                  && s.Season.UrlName == seasonName);

            var fullSeries = _mapper.Map<Series>(seriesDb);
            fullSeries.ShowCompetitorClub = club.ShowClubInResults;
            if (fullSeries != null)
            {
                var flatResults = await GetHistoricalResults(fullSeries);
                if (flatResults == null)
                {
                    await UpdateSeriesResults(seriesDb.Id);
                    flatResults = await GetHistoricalResults(fullSeries);
                }
                fullSeries.FlatResults = flatResults;
            }
            // get the current version of the competitors, so we can get current sail number.
            foreach(var comp in fullSeries.FlatResults.Competitors)
            {
                comp.CurrentSailNumber = (await _dbContext.Competitors.FirstOrDefaultAsync(c => c.Id == comp.Id)).SailNumber;
            }
            return fullSeries;
        }

        private async Task LocalizeFlatResults(FlatResults flatResults, Guid clubId)
        {
            var settings = await _dbContext.Clubs
                .Where(c =>
                   c.Id == clubId
                ).Select(c => c.WeatherSettings).SingleOrDefaultAsync();
            if(settings == null)
            {
                return;
            }
            foreach(var race in flatResults.Races)
            {
                race.WindSpeed = 
                    _converter.Convert(
                     race.WindSpeedMeterPerSecond,
                     _converter.MeterPerSecond,
                     settings?.WindSpeedUnits)?.ToString("N0");
                race.WindGust =
                     _converter.Convert(
                         race.WindSpeedMeterPerSecond,
                         _converter.MeterPerSecond,
                         settings?.WindSpeedUnits)?.ToString("N0");
            }
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

        private FlatResults FlattenResults(Series series)
        {
            series.Competitors =
                series.Competitors.OrderBy(c => series.Results.Results.Keys.Contains(c) ?
                    (series.Results.Results[c].Rank ?? int.MaxValue)
                    : (int.MaxValue))
                .ToList();
            var races = FlattenRaces(series);
            var flatResults = new FlatResults
            {
                SeriesId = series.Id,
                Competitors = FlattenCompetitors(series),
                Races = races,
                CalculatedScores = FlattenSeriesScores(series),
                NumberOfDiscards = series.Results.NumberOfDiscards,
                NumberOfSailedRaces = series.Results.SailedRaces.Count(),
                IsPercentSystem = series.Results.IsPercentSystem,
                PercentRequired = series.Results.PercentRequired,
                ScoringSystemName = series.ScoringSystem?.Name
            };
            return flatResults;
        }

        private IEnumerable<FlatSeriesScore> FlattenSeriesScores(Series series)
        {
            if (series?.Results?.Results == null)
            {
                return new List<FlatSeriesScore>();
            }
            return series.Results.Results.Select(
                kvp => new FlatSeriesScore
                {
                    CompetitorId = kvp.Key.Id,
                    Rank = kvp.Value.Rank,
                    TotalScore = kvp.Value.TotalScore,
                    PointsEarned = kvp.Value.PointsEarned,
                    PointsPossible = kvp.Value.PointsPossible,
                    Scores = FlattenScores(kvp.Value),
                    Trend = kvp.Value.Trend
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
            var flatRaces = series.Races
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
                        State = r.State,
                        WeatherIcon = (r.Weather != null ? r.Weather.Icon : null),
                        WindSpeedMeterPerSecond = (r.Weather != null ? r.Weather.WindSpeedMeterPerSecond : null),
                        WindDirectionDegrees = (r.Weather != null ? r.Weather.WindDirectionDegrees : null),
                        WindGustMeterPerSecond = (r.Weather != null ? r.Weather.WindGustMeterPerSecond : null)
                    });

            return flatRaces;
        }

        private IEnumerable<FlatCompetitor> FlattenCompetitors(Series series)
        {
            return series.Competitors
                .Select(c =>
                    new FlatCompetitor
                    {
                        Id = c.Id,
                        Name = c.Name,
                        SailNumber = c.SailNumber,
                        AlternativeSailNumber = c.AlternativeSailNumber,
                        BoatName = c.BoatName,
                        HomeClubName = c.HomeClubName
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
            Database.Entities.Series dbSeries = await 
                _dbObjectBuilder.BuildDbSeriesAsync(series);
            dbSeries.UrlName = UrlUtility.GetUrlName(series.Name);
            dbSeries.UpdatedDate = DateTime.UtcNow;
            if (dbSeries.Season == null && series.Season.Id != Guid.Empty && series.Season.Start != default)
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
                .SingleAsync(c => c.Id == model.Id && c.ClubId == model.ClubId);

            existingSeries.Name = model.Name;
            // Don't update UrlName here: keep links to this series unchanged.
            existingSeries.Description = model.Description;
            existingSeries.IsImportantSeries = model.IsImportantSeries;
            existingSeries.ResultsLocked = model.ResultsLocked;
            existingSeries.ScoringSystemId = model.ScoringSystemId;
            existingSeries.TrendOption = model.TrendOption;
            existingSeries.ExcludeFromCompetitorStats = model.ExcludeFromCompetitorStats;

            if (model.Season != null
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

            foreach (var removingRace in racesToRemove)
            {
                existingSeries.RaceSeries.Remove(removingRace);
            }
            foreach (var addRace in racesToAdd)
            {
                existingSeries.RaceSeries.Add(addRace);
            }

            await _dbContext.SaveChangesAsync();
            if (!(existingSeries.ResultsLocked ?? false))
            {
                await UpdateSeriesResults(existingSeries.Id);
            }
        }

        public async Task Delete(Guid seriesId)
        {
            var dbSeries = await _dbContext.Series
                .Include(f => f.RaceSeries)
                .SingleAsync(c => c.Id == seriesId);
            foreach (var link in dbSeries.RaceSeries.ToList())
            {
                dbSeries.RaceSeries.Remove(link);
            }
            _dbContext.Series.Remove(dbSeries);

            await _dbContext.SaveChangesAsync();
        }

        private async Task SaveChartData(Series fullSeries)
        {
            FlatChartData chartData = await CalculateChartData(fullSeries);
            var oldCharts = await _dbContext
                .SeriesChartResults
                .Where(r => r.SeriesId == fullSeries.Id).ToListAsync();
            
            foreach (var chart in oldCharts.ToList()) {
                _dbContext.SeriesChartResults.Remove(chart);
            }

            var chartResults = new Database.Entities.SeriesChartResults
            {
                Id = Guid.NewGuid(),
                SeriesId = fullSeries.Id,
                IsCurrent = true,
                Results = Newtonsoft.Json.JsonConvert.SerializeObject(chartData),
                Created = DateTime.Now
            };
            _dbContext.SeriesChartResults.Add(chartResults);
            await _dbContext.SaveChangesAsync();

        }

        private async Task<FlatChartData> CalculateChartData(Series fullSeries)
        {
            var entries = new List<FlatChartPoint>();
            var lastRaceId = fullSeries.Races.OrderByDescending(r => r.Date).ThenByDescending(r => r.Order).FirstOrDefault()?.Id;
            entries.AddRange(GetChartDataPoints(fullSeries));
            fullSeries.Races = fullSeries.Races
                .Where(r => r.State == null || r.State == Api.Enumerations.RaceState.Raced
                || r.Id == lastRaceId)
                .ToList();
            var copyOfRaces = fullSeries.Races.ToList();
            var copyOfCompetitors = fullSeries.Competitors.ToList();

            var racesToRemove = fullSeries.Races.OrderByDescending(r => r.Date).ThenByDescending(r => r.Order)
                .ToList();
            foreach (var race in racesToRemove)
            {
                if (race != racesToRemove.Last())
                {
                    fullSeries.Races.Remove(race);
                    if (fullSeries.Races.Count > 0)
                    {
                        await CalculateScoresAsync(fullSeries);
                        entries.AddRange(GetChartDataPoints(fullSeries));
                    }
                }
            }
            fullSeries.Races = copyOfRaces;
            fullSeries.Competitors = copyOfCompetitors;
            return new FlatChartData
            {
                Races = FlattenRaces(fullSeries),
                Competitors = FlattenCompetitors(fullSeries),
                IsLowPoints = true, //todo: figure out if it's really low points
                Entries = entries
            };
        }

        private IEnumerable<FlatChartPoint> GetChartDataPoints(Series fullSeries)
        {
            var lastRaceId = fullSeries.Races.OrderByDescending(r => r.Date).ThenByDescending(r => r.Order).FirstOrDefault()?.Id;
            if(lastRaceId == null)
            {
                yield break;
            }
            foreach (var comp in fullSeries.Competitors)
            {
                var calcScore = fullSeries.Results.Results.FirstOrDefault(r => r.Key == comp).Value;
                var raceScore = calcScore.CalculatedScores.FirstOrDefault(s => s.Key.Id == lastRaceId).Value;
                yield return new FlatChartPoint
                {
                    RaceId = lastRaceId.Value,
                    CompetitorId = comp.Id,
                    RacePlace = raceScore?.ScoreValue,
                    SeriesRank = calcScore?.Rank,
                    SeriesPoints = calcScore?.TotalScore
                };
            }
        }

        public async Task<FlatResults> GetHistoricalResults(Series series)
        {
            var dbRow = await _dbContext.HistoricalResults
                .SingleOrDefaultAsync(r =>
                    r.SeriesId == series.Id
                    && r.IsCurrent);

            if (String.IsNullOrWhiteSpace(dbRow?.Results))
            {
                return null;
            }

            var flatResults = Newtonsoft.Json.JsonConvert.DeserializeObject<FlatResults>(dbRow.Results);
            await LocalizeFlatResults(flatResults, series.ClubId);
            return flatResults;
        }

        public async Task<FlatChartData> GetChartData(Guid seriesId)
        {
            var dbRow = await _dbContext.SeriesChartResults
                .SingleOrDefaultAsync(r =>
                    r.SeriesId == seriesId
                    && r.IsCurrent);

            if (String.IsNullOrWhiteSpace(dbRow?.Results))
            {
                return null;
            }
            return Newtonsoft.Json.JsonConvert.DeserializeObject<FlatChartData>(dbRow.Results);
        }
    }
}
