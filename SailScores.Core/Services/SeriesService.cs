using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
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
            bool includeRegatta,
            bool includeSummary)
        {

            IQueryable<Guid> regattaSeriesIds = Enumerable.Empty<Guid>().AsQueryable();
            
            if (!includeRegatta)
            {
                regattaSeriesIds = _dbContext.Regattas.SelectMany(r =>
                    r.RegattaSeries).Select(rs => rs.SeriesId);
            }

            IQueryable<dbObj.Series> seriesQuery =  _dbContext
                .Clubs
                .Where(c => c.Id == clubId)
                .SelectMany(c => c.Series)
                .Where(s => date == null ||
                    (s.Season.Start <= date && s.Season.End >= date))
                .Include(s => s.Season)
                .Include(s => s.RaceSeries)
                    .ThenInclude(rs => rs.Race)
                    .ThenInclude(r => r.Fleet)
                 .Include(s => s.ChildLinks);
            
            // Apply date restriction filtering
            if (date.HasValue)
            {
                var dateOnly = DateOnly.FromDateTime(date.Value);
                seriesQuery = seriesQuery.Where(s =>
                    // Either not date restricted
                    (s.DateRestricted != true) ||
                    // Or date restricted and date falls within enforced range
                    (s.DateRestricted == true && 
                     s.EnforcedStartDate != null && 
                     s.EnforcedEndDate != null &&
                     s.EnforcedStartDate <= dateOnly && 
                     s.EnforcedEndDate >= dateOnly));
            }

            if (!includeRegatta)
            {
                seriesQuery = seriesQuery.Where(s => !regattaSeriesIds.Contains(s.Id));

            }
            if (!includeSummary) {
                seriesQuery = seriesQuery.Where(s => s.Type != dbObj.SeriesType.Summary);
            }
                
            var series = await seriesQuery
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
                // populate parentSeriesIds
                fullSeries.ParentSeriesIds = await _dbContext.Series
                    .Where(s => s.ChildLinks.Any(l => l.ChildSeriesId == seriesDb.Id))
                    .Select(s => s.Id)
                    .ToListAsync()
                    .ConfigureAwait(false);

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
            String updatedBy,
            bool calculateParents = true)
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

            if(calculateParents)
            {
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
        }

        public async Task UpdateParentSeriesResults(
            Guid seriesId,
            String updatedBy)
        {

            var parentSeries = await _dbContext.Series
                .Include(s => s.ChildLinks)
                .Where(s => s.ChildLinks.Any(l => l.ChildSeriesId == seriesId))
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
            if (dbSeries.Type == dbObj.SeriesType.Summary)
            {
                await PopulateSummaryForSummarySeries(dbSeries, level).ConfigureAwait(false);
            }
            else
            {
                await PopulateSummaryForRegularSeries(dbSeries).ConfigureAwait(false);
            }
        }

        private async Task PopulateSummaryForSummarySeries(dbObj.Series dbSeries, int level)
        {
            if (dbSeries.ChildLinks == null || !dbSeries.ChildLinks.Any())
            {
                dbSeries.RaceCount = null;
                dbSeries.StartDate = null;
                dbSeries.EndDate = null;
                return;
            }

            var childIds = dbSeries.ChildLinks.Select(l => l.ChildSeriesId).ToList();
            var allChildSeries = await _dbContext.Series
                .Include(s => s.RaceSeries)
                .ThenInclude(rs => rs.Race)
                .Where(s => childIds.Contains(s.Id))
                .ToListAsync()
                .ConfigureAwait(false);

            foreach (var childSeries in allChildSeries)
            {
                await PopulateChildSummaryValues(childSeries, level).ConfigureAwait(false);
            }

            dbSeries.RaceCount = allChildSeries.Sum(s => s.RaceCount);
            dbSeries.StartDate = allChildSeries.Min(s => s.StartDate);
            dbSeries.EndDate = allChildSeries.Max(s => s.EndDate);
        }

        private async Task PopulateChildSummaryValues(dbObj.Series childSeries, int level)
        {
            if (childSeries.Type == dbObj.SeriesType.Summary && level < 10)
            {
                await PopulateSummaryValues(childSeries, level + 1).ConfigureAwait(false);
            }
            if (childSeries.Type != dbObj.SeriesType.Summary &&
                (childSeries.RaceCount == null || childSeries.StartDate == null || childSeries.EndDate == null))
            {
                childSeries.RaceCount = childSeries.RaceSeries.Count();
                var minDate = childSeries.RaceSeries.Select(rs => rs.Race).Min(r => r.Date);
                var maxDate = childSeries.RaceSeries.Select(rs => rs.Race).Max(r => r.Date);
                childSeries.StartDate = GetDateOnlyFromNullableDate(minDate);
                childSeries.EndDate = GetDateOnlyFromNullableDate(maxDate);

                // If the series is date restricted, prefer the enforced dates
                if (childSeries.DateRestricted == true)
                {
                    if (childSeries.EnforcedStartDate.HasValue)
                    {
                        childSeries.StartDate = childSeries.EnforcedStartDate;
                    }
                    if (childSeries.EnforcedEndDate.HasValue)
                    {
                        childSeries.EndDate = childSeries.EnforcedEndDate;
                    }
                }
            }
        }

        private DateOnly? GetDateOnlyFromNullableDate(DateTime? nullableDateTime)
        {
            return nullableDateTime.HasValue ?
                DateOnly.FromDateTime(nullableDateTime.Value) :
                (DateOnly?)null;
        }

        private async Task PopulateSummaryForRegularSeries(dbObj.Series dbSeries)
        {
            var raceIds = dbSeries.RaceSeries.Select(rs => rs.RaceId);
            var races = _dbContext.Races.Where(r => raceIds.Any(r2 => r2 == r.Id));
            dbSeries.RaceCount = await races.CountAsync().ConfigureAwait(false);
            dbSeries.StartDate = await races.MinAsync(r => r.Date.HasValue ? DateOnly.FromDateTime(r.Date.Value) : (DateOnly?)null).ConfigureAwait(false);
            dbSeries.EndDate = await races.MaxAsync(r => r.Date.HasValue ? DateOnly.FromDateTime(r.Date.Value) : (DateOnly?)null).ConfigureAwait(false);

            // If the series has date restrictions, use the enforced dates instead of calculated ones
            if (dbSeries.DateRestricted == true)
            {
                if (dbSeries.EnforcedStartDate.HasValue)
                {
                    dbSeries.StartDate = dbSeries.EnforcedStartDate;
                }
                if (dbSeries.EnforcedEndDate.HasValue)
                {
                    dbSeries.EndDate = dbSeries.EnforcedEndDate;
                }
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

            var discardsToUse = discards > 0 ? discards : 0;
            // upper bound on discards for safety.
            discardsToUse = Math.Min(discardsToUse, 100);

            // if down to one race, then no discards.
            var sb = new System.Text.StringBuilder();
            sb.Append("0,");
            for (int i = 1; i <= discardsToUse; i++)
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
                     settings.WindSpeedUnits)?.ToString("N0");
                race.WindGust =
                     _converter.Convert(
                         race.WindSpeedMeterPerSecond,
                         _converter.MeterPerSecond,
                         settings.WindSpeedUnits)?.ToString("N0");
                race.WindSpeedUnits = settings.WindSpeedUnits;
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
                LowerScoreWins = series.Results.LowerScoreWins,
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
                    Average = kvp.Value.Average,
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

        private List<FlatRace> FlattenRaces(Series series)
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
                        WeatherIcon = r.Weather?.Icon,
                        WindSpeedMeterPerSecond = r.Weather?.WindSpeedMeterPerSecond,
                        WindDirectionDegrees = r.Weather?.WindDirectionDegrees,
                        WindGustMeterPerSecond = r.Weather?.WindGustMeterPerSecond
                    };
                if (r is SeriesAsRace seriesAsRace)
                {
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
                .Select(s => s.CompetitorId)
                .Distinct();

            if (!_cache.TryGetValue($"SeriesCompetitors_{series.Id}", out List<dbObj.Competitor> dbCompetitors))
            {
                dbCompetitors = await _dbContext.Competitors
                    .Where(c => compIds.Contains(c.Id)).ToListAsync()
                    .ConfigureAwait(false);

                _cache.Set($"SeriesCompetitors_{series.Id}", dbCompetitors, TimeSpan.FromSeconds(30));
            }
            else
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

            if (await _dbContext.Series.AnyAsync(s =>
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

            // Create child series links if specified (used by Summary series)
            if (series.ChildrenSeriesIds != null && series.ChildrenSeriesIds.Any())
            {
                var distinctChildIds = series.ChildrenSeriesIds.Distinct().ToList();
                foreach (var childSeriesId in distinctChildIds)
                {
                    dbSeries.ChildLinks ??= new List<dbObj.SeriesToSeriesLink>();

                    if (!dbSeries.ChildLinks.Any(l => l.ChildSeriesId == childSeriesId))
                    {
                        dbSeries.ChildLinks.Add(new dbObj.SeriesToSeriesLink
                        {
                            ParentSeriesId = dbSeries.Id,
                            ChildSeriesId = childSeriesId
                        });
                    }
                }
                await _dbContext.SaveChangesAsync().ConfigureAwait(false);
            }

            // Create parent series links if specified
            if (series.ParentSeriesIds != null && series.ParentSeriesIds.Any())
            {
                foreach (var parentSeriesId in series.ParentSeriesIds)
                {
                    var parentSeries = await _dbContext.Series
                        .Include(s => s.ChildLinks)
                        .FirstOrDefaultAsync(s => s.Id == parentSeriesId)
                        .ConfigureAwait(false);

                    if (parentSeries != null)
                    {
                        parentSeries.ChildLinks ??= new List<dbObj.SeriesToSeriesLink>();
                        
                        // Only add if not already present
                        if (!parentSeries.ChildLinks.Any(l => l.ChildSeriesId == dbSeries.Id))
                        {
                            parentSeries.ChildLinks.Add(new dbObj.SeriesToSeriesLink
                            {
                                ParentSeriesId = parentSeriesId,
                                ChildSeriesId = dbSeries.Id
                            });
                        }
                    }
                }
                await _dbContext.SaveChangesAsync()
                    .ConfigureAwait(false);
            }

            await UpdateSeriesResults(dbSeries.Id, series.UpdatedBy)
                .ConfigureAwait(false);

            // Update parent series results if parent series were specified
            if (series.ParentSeriesIds != null && series.ParentSeriesIds.Any())
            {
                foreach (var parentSeriesId in series.ParentSeriesIds)
                {
                    await UpdateSeriesResults(parentSeriesId, series.UpdatedBy, false)
                        .ConfigureAwait(false);
                }
            }

            return dbSeries.Id;
        }


        public async Task Update(Series model)
        {
            var existingSeries = await _dbContext.Series
                .Include(f => f.RaceSeries)
                .Include(f => f.ChildLinks)
                .Include(f => f.Season)
                .AsSplitQuery()
                .SingleAsync(c => c.Id == model.Id && c.ClubId == model.ClubId)
                .ConfigureAwait(false);

            await EnsureUniqueSeriesName(model, existingSeries).ConfigureAwait(false);

            if (!DoIdentifiersMatch(model, existingSeries))
            {
                await _forwarderService.CreateSeriesForwarder(model, existingSeries);
            }

            UpdateSeriesProperties(model, existingSeries);
            await PopulateSummaryValues(existingSeries);

            UpdateSeriesRaces(model, existingSeries);

            if (existingSeries.Type == dbObj.SeriesType.Summary)
            {
                UpdateSeriesLinks(model, existingSeries);
            }

            await _dbContext.SaveChangesAsync().ConfigureAwait(false);

            if (!(existingSeries.ResultsLocked ?? false))
            {
                await UpdateSeriesResults(existingSeries.Id, existingSeries.UpdatedBy).ConfigureAwait(false);
            }
        }

        private async Task EnsureUniqueSeriesName(Series model, dbObj.Series existingSeries)
        {
            if (await _dbContext.Series.AnyAsync(s =>
                s.Id != model.Id
                && s.ClubId == model.ClubId
                && s.Name == model.Name
                && s.Season.Id == existingSeries.Season.Id))
            {
                throw new InvalidOperationException(
                    "Cannot update series. A series with this name in this season already exists.");
            }
        }

        private static void UpdateSeriesProperties(Series model, dbObj.Series existingSeries)
        {
            existingSeries.Name = model.Name;
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
            existingSeries.DateRestricted = model.DateRestricted;
            existingSeries.EnforcedStartDate = model.EnforcedStartDate;
            existingSeries.EnforcedEndDate = model.EnforcedEndDate;
        }

        private void UpdateSeriesRaces(Series model, dbObj.Series existingSeries)
        {
            var racesToRemove = new List<dbObj.SeriesRace>();
            if (model.Races != null)
            {
                racesToRemove = existingSeries.RaceSeries
                    .Where(f => !(model.Races.Any(c => c.Id == f.RaceId)))
                    .ToList();
            }
            var racesToAdd = model.Races != null
                ? model.Races
                    .Where(c => !(existingSeries.RaceSeries.Any(f => c.Id == f.RaceId)))
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
        }

        private void UpdateSeriesLinks(Series model, dbObj.Series existingSeries)
        {
            model.ChildrenSeriesIds ??= new List<Guid>();
            existingSeries.ChildLinks ??= new List<dbObj.SeriesToSeriesLink>();

            if (existingSeries.ChildLinks != null && existingSeries.ChildLinks.Any())
            {
                var seriesLinksToRemove = existingSeries.ChildLinks
                    .Where(l => !model.ChildrenSeriesIds.Any(c => c == l.ChildSeriesId))
                    .ToList();
                foreach (var removingLink in seriesLinksToRemove)
                {
                    existingSeries.ChildLinks.Remove(removingLink);
                }
            }
            var seriesLinksToAdd = model.ChildrenSeriesIds
                .Where(c => !(existingSeries.ChildLinks.Any(l => c == l.ChildSeriesId)))
                .Select(c => new dbObj.SeriesToSeriesLink
                {
                    ChildSeriesId = c,
                    ParentSeriesId = existingSeries.Id
                }).ToList();

            foreach (var addLink in seriesLinksToAdd)
            {
                existingSeries.ChildLinks.Add(addLink);
            }
        }

        private static bool DoIdentifiersMatch(Series model, dbObj.Series existingSeries)
        {
            // no longer checking season, as it cannot be changed.
            return model.Name == existingSeries.Name
                   && model.ClubId == existingSeries.ClubId;
        }

        public async Task Delete(Guid seriesId)
        {
            var dbSeries = await _dbContext.Series
                .Include(f => f.RaceSeries)
                .Include(f => f.ChildLinks)
                .Include(f => f.ParentLinks)
                .AsSplitQuery()
                .SingleAsync(c => c.Id == seriesId)
                .ConfigureAwait(false);
            foreach (var link in dbSeries.RaceSeries.ToList())
            {
                dbSeries.RaceSeries.Remove(link);
            }
            foreach (var link in dbSeries.ChildLinks.ToList())
            {
                dbSeries.ChildLinks.Remove(link);
            }

            foreach (var link in dbSeries.ParentLinks.ToList())
            {
                dbSeries.ParentLinks.Remove(link);
            }

            _dbContext.Series.Remove(dbSeries);

            await _dbContext.SaveChangesAsync()
                .ConfigureAwait(false);
        }

        private async Task SaveChartData(Series fullSeries)
        {
            FlatChartData chartData = await CalculateChartData(fullSeries)
                .ConfigureAwait(false);

            // used to have .ExecuteDeleteAsync, but that isn't supported in in-memory provider. :-(
            var oldCharts = await _dbContext.SeriesChartResults
                .Where(r => r.SeriesId == fullSeries.Id).ToListAsync()
                .ConfigureAwait(false);
            _dbContext.SeriesChartResults.RemoveRange(oldCharts);


            if (chartData != null)
            {
                // order the races:
                chartData.Races = chartData.Races.OrderBy(r => r.Date).ThenBy(r => r.Order);

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

            var isLowPoint = fullSeries.Results.LowerScoreWins ?? !(fullSeries.Results.IsPercentSystem);

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
