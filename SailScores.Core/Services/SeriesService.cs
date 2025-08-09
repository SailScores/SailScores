using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Identity.Client;
using SailScores.Api.Enumerations;
using SailScores.Core.FlatModel;
using SailScores.Core.Model;
using SailScores.Core.Scoring;
using SailScores.Core.Utility;
using SailScores.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using dbObj = SailScores.Database.Entities;

namespace SailScores.Core.Services
{
    public class SeriesService : ISeriesService
    {
        private readonly IScoringCalculatorFactory _scoringCalculatorFactory;
        private readonly IScoringService _scoringService;
        private readonly IConversionService _converter;
        private readonly IForwarderService _forwarderService;
        private readonly IDbObjectBuilder _dbObjectBuilder;
        private readonly ISailScoresContext _dbContext;
        private readonly IMemoryCache _cache;
        private readonly IMapper _mapper;

        public SeriesService(
            IScoringCalculatorFactory scoringCalculatorFactory,
            IScoringService scoringService,
            IForwarderService forwarderService,
            IConversionService converter,
            IDbObjectBuilder dbObjBuilder,
            ISailScoresContext dbContext,
            IMemoryCache cache,
            IMapper mapper)
        {
            _scoringCalculatorFactory = scoringCalculatorFactory;
            _scoringService = scoringService;
            _forwarderService = forwarderService;
            _converter = converter;
            _dbObjectBuilder = dbObjBuilder;
            _dbContext = dbContext;
            _cache = cache;
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
                    (s.Season.Start <= date && s.Season.End >= date))
                .Include(s => s.Season)
                .Include(s => s.RaceSeries)
                    .ThenInclude(rs => rs.Race)
                    .ThenInclude(r => r.Fleet)
                 .Include(s => s.ChildLinks)
                .Where(s => includeRegattaSeries || !regattaSeriesId.Contains(s.Id))
                .OrderBy(s => s.Name)
                .AsSplitQuery()
                .ToListAsync().ConfigureAwait(false);

            var returnObj = _mapper.Map<List<Series>>(series);
            return returnObj;
        }

        public async Task<Series> GetOneSeriesAsync(Guid seriesId)
        {
            var seriesDb = await _dbContext
                .Series
                .Include(s => s.Season)
                .Include(s => s.ChildLinks)
                .AsSingleQuery()
                .FirstOrDefaultAsync(c => c.Id == seriesId)
                .ConfigureAwait(false);

            var fullSeries = _mapper.Map<Series>(seriesDb);
            if (fullSeries != null)
            {
                var flatResults = await GetHistoricalResults(fullSeries)
                    .ConfigureAwait(false);
                if (flatResults == null)
                {
                    await UpdateSeriesResults(seriesDb.Id, seriesDb.UpdatedBy)
                        .ConfigureAwait(false);
                    flatResults = await GetHistoricalResults(fullSeries)
                        .ConfigureAwait(false);
                }
                fullSeries.FlatResults = flatResults;

                if (flatResults.NumberOfSailedRaces == 0)
                {
                    flatResults.NumberOfSailedRaces = flatResults.Races
                        .Count(r => (r.State ?? RaceState.Raced) == RaceState.Raced
                                     || r.State == RaceState.Preliminary);
                }

                if (await IsPartOfRegatta(seriesDb.Id))
                {
                    fullSeries.PreferAlternativeSailNumbers = await DoesRegattaPrefersAltSailNumbers(seriesDb.Id);
                    fullSeries.ShowCompetitorClub = true;
                }
            }
            return fullSeries;
        }

        private async Task<bool> IsPartOfRegatta(Guid seriesId)
        {
            return await _dbContext.Regattas
                .SelectMany(r => r.RegattaSeries)
                .AnyAsync(rs => rs.SeriesId == seriesId);
        }

        private async Task<bool> DoesRegattaPrefersAltSailNumbers(Guid seriesId)
        {
            return await _dbContext.Regattas
                .Where(r => r.RegattaSeries.Any( rs => rs.SeriesId == seriesId))
                .Select(r => r.PreferAlternateSailNumbers)
                .FirstOrDefaultAsync() ?? false;
        }

        public async Task UpdateSeriesResults(
            Guid seriesId,
            String updatedBy)
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
                    .AsSplitQuery()
                .SingleAsync(s => s.Id == seriesId)
                .ConfigureAwait(false);

            if (dbSeries.ResultsLocked ?? false)
            {
                return;
            }

            await PopulateSummaryValues(dbSeries);
            var fullSeries = _mapper.Map<Series>(dbSeries);
            fullSeries.UpdatedBy = updatedBy;
            await CalculateScoresAsync(fullSeries)
                .ConfigureAwait(false);
            dbSeries.UpdatedDate = DateTime.UtcNow;


            await SaveHistoricalResults(fullSeries)
                .ConfigureAwait(false);

            await SaveChartData(fullSeries)
                .ConfigureAwait(false);

            //todo: check for parent series to be updated
            var parentSeries = await _dbContext.Series
                .Include(s => s.ChildLinks)
                .Where(s => s.ChildLinks.Any(l => l.ChildSeriesId == dbSeries.Id))
                .ToListAsync()
                .ConfigureAwait(false);
            if (parentSeries != null)
            {
                foreach (var parent in parentSeries)
                {
                    await UpdateSeriesResults(parent.Id, updatedBy)
                        .ConfigureAwait(false);
                }
            }
        }
        private async Task PopulateSummaryValues(
            dbObj.Series dbSeries,
            int level = 0)
        {
            // three things to populate:
            // - RaceCount
            // - StartDate
            // - EndDate

            //if it's a summary series, we need to calculate these by reviewing child series.
            if (dbSeries.Type == dbObj.SeriesType.Summary)
            {
                if (dbSeries.ChildLinks == null || dbSeries.ChildLinks.Count() == 0)
                {
                    dbSeries.RaceCount = null;
                    dbSeries.StartDate = null;
                    dbSeries.EndDate = null;
                    return;
                }
                var childIds = dbSeries.ChildLinks.Select(l => l.ChildSeriesId).ToList();
                var allChildSeries = await _dbContext.Series
                    .Include(s => s.RaceSeries)
                    .ThenInclude(rs => rs.Race).Where(s =>
                    childIds.Contains(s.Id)).ToListAsync();

                foreach ( var childSeries in allChildSeries)
                {
                    // limit how deep we look to roll up.
                    if (childSeries.Type == dbObj.SeriesType.Summary && level <10)
                    {
                        await PopulateSummaryValues(childSeries, level + 1);
                    }
                    if(childSeries.Type != dbObj.SeriesType.Summary && childSeries.RaceCount == null ||
                        childSeries.StartDate == null || childSeries.EndDate == null)
                    {
                        childSeries.RaceCount = childSeries.RaceSeries.Count();
                        var minDate = childSeries.RaceSeries.Select(rs => rs.Race).Min(r => r.Date);
                        var maxDate = childSeries.RaceSeries.Select(rs => rs.Race).Max(r => r.Date);
                        childSeries.StartDate = minDate.HasValue ? DateOnly.FromDateTime(minDate.Value) : (DateOnly?)null;
                        childSeries.EndDate = maxDate.HasValue ? DateOnly.FromDateTime(maxDate.Value) : (DateOnly?)null;
                    }
                }

                dbSeries.RaceCount = allChildSeries.Sum(s => s.RaceCount);
                dbSeries.StartDate = allChildSeries.Min(s => s.StartDate);
                dbSeries.EndDate = allChildSeries.Max(s => s.EndDate);
            } else
            {
                // if it's not a summary series, just set the values.
                var raceIds = dbSeries.RaceSeries.Select(rs => rs.RaceId);
                var races = _dbContext.Races
                    .Where(r => raceIds.Any(r2 => r2 == r.Id));
                dbSeries.RaceCount = races.Count();
                dbSeries.StartDate = races.Min(r => r.Date.HasValue ? DateOnly.FromDateTime(r.Date.Value) : (DateOnly?)null);
                dbSeries.EndDate = races.Max(r => r.Date.HasValue ? DateOnly.FromDateTime(r.Date.Value) : (DateOnly?)null);
            }
        }

        private async Task CalculateScoresAsync(Series fullSeries)
        {
            await CalculateScoresAsync(fullSeries, true)
                .ConfigureAwait(false);
        }

        private async Task CalculateScoresAsync(Series fullSeries,
            bool initial)
        {
            var dbScoringSystem = await _scoringService.GetScoringSystemFromCacheAsync(
                fullSeries)
                .ConfigureAwait(false);

            fullSeries.ScoringSystem = _mapper.Map<ScoringSystem>(dbScoringSystem);
            var calculator = await _scoringCalculatorFactory
                .CreateScoringCalculatorAsync(fullSeries.ScoringSystem)
                .ConfigureAwait(false);

            if (initial)
            {

                if (fullSeries.Type == SeriesType.Summary)
                {
                    if (fullSeries.ChildrenSeriesAsSingleRace)
                    {
                        await AddChildSeriesAsRaces(fullSeries).ConfigureAwait(false);
                    }
                    else
                    {
                        await AddChildSeriesAsSeries(fullSeries).ConfigureAwait(false);
                    }
                }
                else  // non-summary series.
                {
                    fullSeries.Races = fullSeries.Races.Where(r => r != null).ToList();
                }
            }
            await PopulateCompetitorsAsync(fullSeries)
                .ConfigureAwait(false);

            var results = calculator.CalculateResults(fullSeries);
            fullSeries.Results = results;

            // saw one instance when results weren't sorted. Having trouble reproducing that.
        }

        private async Task AddChildSeriesAsSeries(Series fullSeries)
        {
            // Get all the races in any child series
            var tempRaceList = new List<Race>();
            foreach (var childLink in fullSeries.ChildrenSeriesIds)
            {
                var childSeries = await _dbContext
                    .Series
                    .Include(s => s.RaceSeries)
                        .ThenInclude(rs => rs.Race)
                            .ThenInclude(r => r.Scores)
                    .AsSplitQuery()
                    .SingleAsync(s => s.Id == childLink)
                    .ConfigureAwait(false);

                if (childSeries != null)
                {
                    tempRaceList.AddRange(
                        _mapper.Map<List<Race>>(
                            childSeries.RaceSeries.Select(rs => rs.Race)));
                }
            }

            fullSeries.Races = tempRaceList;
        }

        private async Task AddChildSeriesAsRaces(Series fullSeries)
        {
            // Get the results of each child series as a race.
            var tempRaceList = new List<Race>();
            foreach (var childLink in fullSeries.ChildrenSeriesIds)
            {

                var childSeries = await _dbContext
                    .Series
                    .Include(s => s.RaceSeries)
                        .ThenInclude(rs => rs.Race)
                            .ThenInclude(r => r.Scores)
                    .AsSplitQuery()
                    .SingleAsync(s => s.Id == childLink)
                    .ConfigureAwait(false);

                if (childSeries != null)
                {
                    var results = await GetHistoricalResults(fullSeries.ClubId, childSeries.Id)
                        .ConfigureAwait(false);
                    var domainObject = _mapper.Map<Series>(childSeries);
                    tempRaceList.Add(new SeriesAsRace(domainObject, results));
                }
            }

            fullSeries.Races = tempRaceList;
        }

        public async Task<Series> CalculateWhatIfScoresAsync(
            Guid seriesId,
            Guid scoringSystemId,
            int discards,
            Decimal? participationPercent)
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
                    .AsSplitQuery()
                .SingleAsync(s => s.Id == seriesId)

                .ConfigureAwait(false);

            var fullSeries = _mapper.Map<Series>(dbSeries);
            // if a non-default scoring system is specified, use it.
            fullSeries.ScoringSystemId = scoringSystemId != default ? scoringSystemId : fullSeries.ScoringSystemId;
            var dbScoringSystem = await _scoringService.GetScoringSystemAsync(
                fullSeries.ScoringSystemId.Value)
                .ConfigureAwait(false);

            fullSeries.ScoringSystem = _mapper.Map<ScoringSystem>(dbScoringSystem);
            var sb = new System.Text.StringBuilder();
            for(int i = 0; i <= discards; i++)
            {
                sb.Append(i);
                sb.Append(",");
            }
            fullSeries.ScoringSystem.DiscardPattern = sb.ToString();
            fullSeries.ScoringSystem.ParticipationPercent = participationPercent ??
                fullSeries.ScoringSystem.ParticipationPercent;
            var calculator = await _scoringCalculatorFactory
                .CreateScoringCalculatorAsync(fullSeries.ScoringSystem)
                .ConfigureAwait(false);

            fullSeries.Races = fullSeries.Races.Where(r => r != null).ToList();
            await PopulateCompetitorsAsync(fullSeries)
                .ConfigureAwait(false);

            var results = calculator.CalculateResults(fullSeries);
            fullSeries.Results = results;
            fullSeries.FlatResults = FlattenResults(fullSeries);
            return fullSeries;
        }

        public async Task<Series> GetSeriesDetailsAsync(
            string clubInitials,
            string seasonName,
            string seriesUrlName)
        {
            var club = await _dbContext.Clubs
                .Where(c =>
                   c.Initials == clubInitials
                ).SingleAsync()
                .ConfigureAwait(false);
            var clubId = club.Id;
            var seriesId = await _dbContext
                .Series
                .Where(s =>
                    s.ClubId == clubId
                        && s.UrlName == seriesUrlName
                        && s.Season.UrlName == seasonName)
                .Select(s => s.Id)
                .FirstOrDefaultAsync()
                .ConfigureAwait(false);
            if(seriesId == default)
            {
                return null;
            }

            var fullSeries = await GetOneSeriesAsync(seriesId)
                .ConfigureAwait(false);

            fullSeries.ShowCompetitorClub = club.ShowClubInResults;

            // get the current version of the competitors, so we can get current sail number.
            var competitorUrlNamesById = await _dbContext.Competitors
                .Where(c => c.ClubId == clubId
                    && c.UrlName != null)
                .ToDictionaryAsync(c => c.Id, c => c.UrlName)
                .ConfigureAwait(false);
            foreach (var comp in fullSeries.FlatResults.Competitors)
            {
                comp.UrlName = competitorUrlNamesById.ContainsKey(comp.Id) 
                    ? competitorUrlNamesById[comp.Id]
                    : UrlUtility.GetUrlName(comp.SailNumber);
            }
            return fullSeries;
        }

        private async Task LocalizeFlatResults(FlatResults flatResults, Guid clubId)
        {
            var settings = await _dbContext.Clubs
                .Where(c =>
                   c.Id == clubId
                ).Select(c => c.WeatherSettings).SingleOrDefaultAsync()
                .ConfigureAwait(false);
            if (settings == null)
            {
                return;
            }
            foreach (var race in flatResults.Races)
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
                race.WindSpeedUnits = settings?.WindSpeedUnits;
            }
        }

        private async Task SaveHistoricalResults(Series series)
        {
            DateTime currentDate = DateTime.Today;
            FlatModel.FlatResults results = FlattenResults(series);

            var oldResults = await _dbContext
                .HistoricalResults
                .Where(r => r.SeriesId == series.Id).ToListAsync()
                .ConfigureAwait(false);
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

            await _dbContext.SaveChangesAsync()
                .ConfigureAwait(false);
        }

        private FlatResults FlattenResults(Series series)
        {
            if (series?.Results?.Results != null)
            {
                series.Competitors =
                    series.Competitors.OrderBy(c => series.Results.Results.Keys.Contains(c) ?
                        (series.Results.Results[c].Rank ?? int.MaxValue)
                        : (int.MaxValue))
                    .ThenByDescending(c => series.Results.Results.Keys.Contains(c) ?
                                           (series.Results.Results[c].ParticipationPercent ?? 0m)
                                                                  : 0m)
                    .ToList();

            }
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
                ScoringSystemName = series.ScoringSystem?.Name,
                ScoreCodesUsed = series.Results.ScoreCodesUsed,
                IsPreliminary = series.Races.Any(r => r.State == RaceState.Preliminary),
                UpdatedBy =series.UpdatedBy
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
                    ParticipationPercent = kvp.Value.ParticipationPercent,
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
                    PerfectScoreValue = s.Value.PerfectScoreValue,
                    Discard = s.Value.Discard
                });
        }

        private IEnumerable<FlatRace> FlattenRaces(Series series)
        {
            var flatRaces = new List<FlatRace>();
            foreach (var r in series.Races
                .OrderBy(r => r.Date)
                .ThenBy(r => r.Order))
            {
                var flatRace = 
                    new FlatRace
                    {
                        Id = r.Id,
                        Name = r.Name,
                        Date = r.Date,
                        Order = r.Order,
                        Description = r.Description,
                        IsSeries = r.IsSeriesSummary,
                        State = r.State,
                        WeatherIcon = (r.Weather != null ? r.Weather.Icon : null),
                        WindSpeedMeterPerSecond = (r.Weather != null ? r.Weather.WindSpeedMeterPerSecond : null),
                        WindDirectionDegrees = (r.Weather != null ? r.Weather.WindDirectionDegrees : null),
                        WindGustMeterPerSecond = (r.Weather != null ? r.Weather.WindGustMeterPerSecond : null)
                    };
                if (r is SeriesAsRace)
                {
                    var seriesAsRace = (SeriesAsRace)r;
                    flatRace.IsSeries = true;
                    flatRace.StartDate = seriesAsRace.StartDate?.ToDateOnly();
                    flatRace.EndDate = seriesAsRace.EndDate?.ToDateOnly();
                    flatRace.TotalChildRaceCount = seriesAsRace.TotalChildRaceCount;
                    flatRace.seriesUrlName = seriesAsRace.SeriesUrl;
                }
                flatRaces.Add(flatRace);
            }

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

            List<dbObj.Competitor> dbCompetitors;
            if (!_cache.TryGetValue($"SeriesCompetitors_{series.Id}", out dbCompetitors))
            {
                dbCompetitors = await _dbContext.Competitors
                    .Where(c => compIds.Contains(c.Id)).ToListAsync()
                    .ConfigureAwait(false);

                _cache.Set($"SeriesCompetitors_{series.Id}", dbCompetitors, TimeSpan.FromSeconds(30));
            } else
            {
                // this method is called with series that may be missing races (creating
                // historical results for charts)
                // so we need to refilter the competitors.
                dbCompetitors = dbCompetitors.Where(c => compIds.Contains(c.Id)).ToList();
            }

            series.Competitors = _mapper.Map<IList<Competitor>>(dbCompetitors);

            foreach (var score in series.Races
                .Where(r => r != null).SelectMany(r => r.Scores))
            {
                var competitor = series.Competitors.FirstOrDefault(c => c.Id == score.CompetitorId);
                if(competitor == null)
                {
                    dbCompetitors = await _dbContext.Competitors
                    .Where(c => compIds.Contains(c.Id)).ToListAsync()
                    .ConfigureAwait(false);

                    _cache.Set($"SeriesCompetitors_{series.Id}", dbCompetitors, TimeSpan.FromSeconds(30));
                    series.Competitors = _mapper.Map<IList<Competitor>>(dbCompetitors);
                    competitor = series.Competitors.First(c => c.Id == score.CompetitorId);
                }
                score.Competitor = competitor;
            }
        }

        public async Task<Guid> SaveNewSeries(Series series, Club club)
        {
            series.ClubId = club.Id;
            var seriesId = await SaveNewSeries(series)
                .ConfigureAwait(false);
            return seriesId;
        }

        public async Task<Guid> SaveNewSeries(Series series)
        {
            Database.Entities.Series dbSeries = await
                _dbObjectBuilder.BuildDbSeriesAsync(series)
                .ConfigureAwait(false);
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
            await _dbContext.SaveChangesAsync()
                .ConfigureAwait(false);

            await UpdateSeriesResults(dbSeries.Id, series.UpdatedBy)
                .ConfigureAwait(false);
            return dbSeries.Id;
        }


        public async Task Update(Series model)
        {
            if (_dbContext.Series.Any(s =>
                s.Id != model.Id
                && s.ClubId == model.ClubId
                && s.Name == model.Name
                && s.Season.Id == model.Season.Id))
            {
                throw new InvalidOperationException(
                    "Cannot update series. A series with this name in this season already exists.");
            }
            var existingSeries = await _dbContext.Series
                .Include(f => f.RaceSeries)
                .Include(f => f.ChildLinks)
                .AsSplitQuery()
                .SingleAsync(c => c.Id == model.Id && c.ClubId == model.ClubId)
                .ConfigureAwait(false);

            if (!DoIdentifiersMatch(model, existingSeries))
            {
                await _forwarderService.CreateSeriesForwarder(model, existingSeries);
            }

            existingSeries.Name = model.Name;
            // Now that forwarders are in place, we can change the url name.
            existingSeries.UrlName = UrlUtility.GetUrlName(model.Name);
            existingSeries.Description = model.Description;
            existingSeries.IsImportantSeries = model.IsImportantSeries;
            existingSeries.ResultsLocked = model.ResultsLocked;
            existingSeries.ScoringSystemId = model.ScoringSystemId;
            existingSeries.TrendOption = model.TrendOption;
            existingSeries.ExcludeFromCompetitorStats = model.ExcludeFromCompetitorStats;
            existingSeries.HideDncDiscards = model.HideDncDiscards;
            existingSeries.UpdatedBy = model.UpdatedBy;
            existingSeries.ChildrenSeriesAsSingleRace = model.ChildrenSeriesAsSingleRace;
            await PopulateSummaryValues(existingSeries);

            // no longer allow season to be changed on a series. Create new if needed.

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

            if(existingSeries.Type == dbObj.SeriesType.Summary)
            {
                if(model.ChildrenSeriesIds == null )
                {
                    model.ChildrenSeriesIds = new List<Guid>();
                }
                if(existingSeries.ChildLinks == null)
                {
                    existingSeries.ChildLinks = new List<dbObj.SeriesToSeriesLink>();
                }
                var seriesLinksToRemove = new List<dbObj.SeriesToSeriesLink>();
                var seriesLinksToAdd = new List<dbObj.SeriesToSeriesLink>();

                if (existingSeries.ChildLinks != null && existingSeries.ChildLinks.Count() > 0)
                {
                    seriesLinksToRemove = 
                    existingSeries.ChildLinks
                        .Where(l => !model.ChildrenSeriesIds.Any(c => c == l.ChildSeriesId))
                        .ToList();
                    foreach (var removingLink in seriesLinksToRemove)
                    {
                        existingSeries.ChildLinks.Remove(removingLink);
                    }
                }
                seriesLinksToAdd =
                    model.ChildrenSeriesIds
                    .Where(c =>
                        !(existingSeries.ChildLinks.Any(l => c == l.ChildSeriesId)))
                    .Select(c => new dbObj.SeriesToSeriesLink
                    {
                        ChildSeriesId = c,
                        ParentSeriesId = existingSeries.Id
                    }).ToList();

                foreach(var addLink in seriesLinksToAdd)
                {
                    existingSeries.ChildLinks.Add(addLink);
                }
            }

            await _dbContext.SaveChangesAsync()
                .ConfigureAwait(false);
            if (!(existingSeries.ResultsLocked ?? false))
            {
                await UpdateSeriesResults(existingSeries.Id, existingSeries.UpdatedBy)
                    .ConfigureAwait(false);
            }
        }

        private bool DoIdentifiersMatch(Series model, dbObj.Series existingSeries)
        {
            return model.Name == existingSeries.Name
                   && model.ClubId == existingSeries.ClubId
                   && model.Season.Id == existingSeries.Season.Id;
        }

        public async Task Delete(Guid seriesId)
        {
            var dbSeries = await _dbContext.Series
                .Include(f => f.RaceSeries)
                .SingleAsync(c => c.Id == seriesId)
                .ConfigureAwait(false);
            foreach (var link in dbSeries.RaceSeries.ToList())
            {
                dbSeries.RaceSeries.Remove(link);
            }
            _dbContext.Series.Remove(dbSeries);

            await _dbContext.SaveChangesAsync()
                .ConfigureAwait(false);
        }

        private async Task SaveChartData(Series fullSeries)
        {
            FlatChartData chartData = await CalculateChartData(fullSeries)
                .ConfigureAwait(false);
            var oldCharts = await _dbContext
                .SeriesChartResults
                .Where(r => r.SeriesId == fullSeries.Id).ExecuteDeleteAsync()
                .ConfigureAwait(false);

            if (chartData != null)
            {
                // order the races:
                chartData.Races = chartData.Races.OrderBy(r => r.Date).ThenBy(r => r.Order);
                foreach (var item in chartData.Races)
                {
                    
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
            }

            await _dbContext.SaveChangesAsync()
                .ConfigureAwait(false);

        }

        private async Task<FlatChartData> CalculateChartData(Series fullSeries)
        {
            var entries = new List<FlatChartPoint>();
            var lastRaceId = fullSeries.Races.OrderByDescending(r => r.Date).ThenByDescending(r => r.Order).FirstOrDefault()?.Id;
            entries.AddRange(GetChartDataPoints(fullSeries));
            fullSeries.Races = fullSeries.Races
                .Where(r => r.State == null || r.State == Api.Enumerations.RaceState.Raced
                || r.State == RaceState.Preliminary
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
                        await CalculateScoresAsync(fullSeries, false)
                            .ConfigureAwait(false);
                        entries.AddRange(GetChartDataPoints(fullSeries));
                    }
                }
            }
            fullSeries.Races = copyOfRaces;
            fullSeries.Competitors = copyOfCompetitors;

            var scoringSystem= fullSeries.ScoringSystem;

            var isLowPoint = !(fullSeries?.Results.IsPercentSystem ?? false);

            return new FlatChartData
            {
                Races = FlattenRaces(fullSeries),
                Competitors = FlattenCompetitors(fullSeries),
                IsLowPoints = isLowPoint,
                Entries = entries
            };
        }

        private IEnumerable<FlatChartPoint> GetChartDataPoints(Series fullSeries)
        {
            var lastRaceId = fullSeries.Races.OrderByDescending(r => r.Date).ThenByDescending(r => r.Order).FirstOrDefault()?.Id;
            if (lastRaceId == null)
            {
                yield break;
            }
            foreach (var comp in fullSeries.Competitors)
            {
                var calcScore = fullSeries.Results?.Results?.FirstOrDefault(r => r.Key == comp).Value;
                var raceScore = calcScore?.CalculatedScores?.FirstOrDefault(s => s.Key.Id == lastRaceId).Value;
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
            return await GetHistoricalResults(series.ClubId, series.Id)
                .ConfigureAwait(false);
        }

        public async Task<FlatResults> GetHistoricalResults(
            Guid clubId,
            Guid seriesId)
        {
            var dbRow = await _dbContext.HistoricalResults
                .SingleOrDefaultAsync(r =>
                    r.SeriesId == seriesId
                    && r.IsCurrent)
                .ConfigureAwait(false);

            if (String.IsNullOrWhiteSpace(dbRow?.Results))
            {
                return null;
            }

            var flatResults = Newtonsoft.Json.JsonConvert.DeserializeObject<FlatResults>(dbRow.Results);
            await LocalizeFlatResults(flatResults, clubId)
                .ConfigureAwait(false);
            return flatResults;
        }

        public async Task<FlatChartData> GetChartData(Guid seriesId)
        {
            var dbRow = await _dbContext.SeriesChartResults
                .SingleOrDefaultAsync(r =>
                    r.SeriesId == seriesId
                    && r.IsCurrent)
                .ConfigureAwait(false);

            if (String.IsNullOrWhiteSpace(dbRow?.Results))
            {
                return null;
            }
            return Newtonsoft.Json.JsonConvert.DeserializeObject<FlatChartData>(dbRow.Results);
        }
    }
}
