using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SailScores.Core.Model;
using SailScores.Core.Model.Summary;
using SailScores.Core.Utility;
using SailScores.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Db = SailScores.Database.Entities;

namespace SailScores.Core.Services
{
    public class ClubService : IClubService
    {
        private readonly ISailScoresContext _dbContext;
        private readonly IMemoryCache _cache;
        private readonly IScoringService _scoringService;
        private readonly IMapper _mapper;

        //used for copying club
        private Dictionary<Guid, Guid> guidMapper;

        public ClubService(
            ISailScoresContext dbContext,
            IMemoryCache cache,
            IScoringService scoringService,
            IMapper mapper)
        {
            _dbContext = dbContext;
            _cache = cache;
            _scoringService = scoringService;
            _mapper = mapper;
        }

        public async Task<IList<Fleet>> GetAllFleets(Guid clubId)
        {
            var dbFleets = await _dbContext
                .Fleets
                .Include(f => f.FleetBoatClasses)
                    .ThenInclude(fbc => fbc.BoatClass)
                .Include(f => f.CompetitorFleets)
                    .ThenInclude(cf => cf.Competitor)
                .Where(f => f.ClubId == clubId)
                .AsSplitQuery()
                .ToListAsync()
                .ConfigureAwait(false);

            var bizObj = _mapper.Map<IList<Fleet>>(dbFleets);

            var bizById = bizObj.ToDictionary(x => x.Id);

            // ignored in mapper to avoid loops.
            foreach (var fleet in dbFleets)
            {
                var target = bizById[fleet.Id];
                var boatClasses = fleet.FleetBoatClasses.Select(fbc => fbc.BoatClass);
                target.BoatClasses
                    = _mapper.Map<IList<BoatClass>>(boatClasses);

                var competitors = fleet.CompetitorFleets.Select(cf => cf.Competitor);
                target.Competitors
                    = _mapper.Map<IList<Competitor>>(competitors);
            }

            return bizObj;
        }

        public async Task<IList<Fleet>> GetActiveFleets(Guid clubId)
        {
            var dbFleets = await _dbContext
                .Fleets
                .Include(f => f.FleetBoatClasses)
                    .ThenInclude(fbc => fbc.BoatClass)
                .Include(f => f.CompetitorFleets)
                    .ThenInclude(cf => cf.Competitor)
                .Where(f => f.ClubId == clubId && (f.IsActive ?? true))
                .AsSplitQuery()
                .ToListAsync()
                .ConfigureAwait(false);

            var bizObj = _mapper.Map<IList<Fleet>>(dbFleets);

            // ignored in mapper to avoid loops.
            foreach (var fleet in dbFleets)
            {
                var boatClasses = fleet.FleetBoatClasses.Select(fbc => fbc.BoatClass);
                bizObj.First(bo => bo.Id == fleet.Id).BoatClasses
                    = _mapper.Map<IList<BoatClass>>(boatClasses);

                var competitors = fleet.CompetitorFleets.Select(cf => cf.Competitor);
                bizObj.First(bo => bo.Id == fleet.Id).Competitors
                    = _mapper.Map<IList<Competitor>>(competitors);
            }
            return bizObj;
        }


        public async Task<IList<Fleet>> GetMinimalForSelectedBoatsFleets(Guid clubId)
        {
            var dbFleets = await _dbContext
                .Fleets
                .Where(f => f.ClubId == clubId
                    && !f.IsHidden
                    && f.FleetType == Api.Enumerations.FleetType.SelectedBoats)
                .ToListAsync()
                .ConfigureAwait(false);

            var bizObj = _mapper.Map<IList<Fleet>>(dbFleets);
            return bizObj;
        }


        public async Task<IEnumerable<BoatClass>> GetAllBoatClasses(Guid clubId)
        {
            var dbClasses = await _dbContext
                .BoatClasses
                .Where(bc => bc.ClubId == clubId)
                .ToListAsync()
                .ConfigureAwait(false);

            var bizObj = _mapper.Map<IList<BoatClass>>(dbClasses);

            return bizObj;
        }

        public async Task<IEnumerable<ClubSummary>> GetClubs(bool includeHidden)
        {
            var dbObjects = _dbContext
                .Clubs
                .Where(c => includeHidden || !c.IsHidden);
            return _mapper.ProjectTo<ClubSummary>(dbObjects);
        }

        public async Task<IEnumerable<ClubActivitySummary>> GetClubsWithRecentActivity(int daysBack)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-daysBack);
            
            // Optimized query: Aggregate race counts and most recent date per club in a single database query
            var clubsWithRecentRaces = await _dbContext
                .Races
                .Where(r => r.Date.HasValue && r.Date.Value >= cutoffDate)
                .Where(r => r.State == Api.Enumerations.RaceState.Raced ||
                    r.State == Api.Enumerations.RaceState.Preliminary)
                .GroupBy(r => r.ClubId)
                .Select(g => new { 
                    ClubId = g.Key, 
                    RaceCount = g.Count(),
                    MostRecentRace = g.Max(r => r.Date.Value)
                })
                .ToListAsync()
                .ConfigureAwait(false);

            // Optimized query: Aggregate series counts and most recent update date per club
            var clubsWithRecentSeries = await _dbContext
                .Series
                .Where(s => s.UpdatedDate.HasValue && s.UpdatedDate.Value >= cutoffDate
                    && (s.StartDate.HasValue && s.StartDate.Value <= DateOnly.FromDateTime(DateTime.UtcNow) 
                        || s.EnforcedStartDate.HasValue && s.EnforcedStartDate.Value <= DateOnly.FromDateTime(DateTime.UtcNow))
                    && (s.EndDate.HasValue && s.EndDate.Value > DateOnly.FromDateTime(cutoffDate) 
                        || s.EnforcedEndDate.HasValue && s.EnforcedEndDate.Value > DateOnly.FromDateTime(cutoffDate)))
                .GroupBy(s => s.ClubId)
                .Select(g => new { 
                    ClubId = g.Key, 
                    SeriesCount = g.Count(),
                    MostRecentSeries = g.Max(s => s.UpdatedDate.Value)
                })
                .ToListAsync()
                .ConfigureAwait(false);

            // Combine race and series activity in memory (already minimal data loaded)
            var allActivityByClub = clubsWithRecentRaces
                .Select(r => new { 
                    r.ClubId, 
                    RaceCount = r.RaceCount,
                    SeriesCount = 0,
                    MostRecentActivity = r.MostRecentRace 
                })
                .Concat(clubsWithRecentSeries.Select(s => new { 
                    s.ClubId, 
                    RaceCount = 0,
                    SeriesCount = s.SeriesCount,
                    MostRecentActivity = s.MostRecentSeries 
                }))
                .GroupBy(a => a.ClubId)
                .Select(g => new { 
                    ClubId = g.Key, 
                    RaceCount = g.Sum(x => x.RaceCount),
                    SeriesCount = g.Sum(x => x.SeriesCount),
                    MostRecentActivity = g.Max(x => x.MostRecentActivity)
                })
                .OrderByDescending(a => a.MostRecentActivity)
                .ToList();

            var clubIds = allActivityByClub.Select(a => a.ClubId).ToList();

            // Fetch only clubs with activity, filtered by visibility
            var clubs = await _dbContext
                .Clubs
                .Where(c => !c.IsHidden && clubIds.Contains(c.Id))
                .ToListAsync()
                .ConfigureAwait(false);

            // Build final result with activity stats, maintaining activity-date order
            var result = clubs
                .Select(c => {
                    var activity = allActivityByClub.First(a => a.ClubId == c.Id);
                    return new ClubActivitySummary
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Initials = c.Initials,
                        Description = c.Description,
                        LogoFileId = c.LogoFileId,
                        IsHidden = c.IsHidden,
                        RecentRaceCount = activity.RaceCount,
                        RecentSeriesCount = activity.SeriesCount,
                        MostRecentActivity = activity.MostRecentActivity
                    };
                })
                .OrderByDescending(c => c.MostRecentActivity)
                .ToList();

            return result;
        }

        public async Task<Guid> GetClubId(string initials)
        {
            if (!Guid.TryParse(initials, out Guid clubGuid))
            {
                if (_cache.TryGetValue($"ClubId_{initials}", out clubGuid))
                {
                    return clubGuid;
                }
                // Is the check to see if initials are a guid necessary?
                if (!Guid.TryParse(initials, out clubGuid))
                {
                    clubGuid = await _dbContext.Clubs
                        .Where(c => c.Initials == initials)
                        .Select(c => c.Id)
                        .SingleAsync()
                        .ConfigureAwait(false);
                }
                _cache.Set($"ClubId_{initials}", clubGuid);
            }
            return clubGuid;
        }

        public async Task<Model.Club> GetClubForAdmin(string initials)
        {
            var guid = await GetClubId(initials)
                .ConfigureAwait(false);

            return await GetClubForAdmin(guid)
                .ConfigureAwait(false);
        }


        public async Task<Model.Club> GetClubForAdmin(Guid id)
        {

            IQueryable<Db.Club> clubQuery =
                _dbContext.Clubs.Where(c => c.Id == id)
                    .Include(c => c.Seasons);
            var club = await clubQuery.FirstAsync()
                .ConfigureAwait(false);

            await clubQuery
                .Include(c => c.Seasons)
                .Include(c => c.Series)
                .ThenInclude(s => s.RaceSeries)
                .Include(c => c.Series)
                .ThenInclude(s => s.Season)
                .Include(c => c.BoatClasses)
                .Include(c => c.DefaultScoringSystem)
                .Include(c => c.ScoringSystems)
                .Include(c => c.Fleets)
                .ThenInclude(f => f.CompetitorFleets)
                .Include(c => c.Fleets)
                .ThenInclude(f => f.FleetBoatClasses)
                .Include(c => c.Regattas)
                .ThenInclude(r => r.RegattaSeries)
                .Include(c => c.Regattas)
                .ThenInclude(r => r.RegattaFleet)
                .Include(c => c.WeatherSettings)
                .AsSplitQuery()
                .LoadAsync().ConfigureAwait(false);

            var retClub = _mapper.Map<Model.Club>(club);

            retClub.Seasons = retClub.Seasons.OrderByDescending(s => s.Start).ToList();
            retClub.Series = retClub.Series
                .OrderByDescending(s => s.Season.Start)
                .ThenBy(s => s.Name)
                .ToList();
            retClub.BoatClasses = retClub.BoatClasses
                .OrderBy(c => c.Name).ToList();
            retClub.Regattas = retClub.Regattas
                .OrderByDescending(r => r.StartDate ?? DateTime.MinValue).ThenBy(r => r.Name).ToList();
            return retClub;
        }

        public async Task<Model.Club> GetFullClubExceptScores(string id)
        {
            var guid = await GetClubId(id)
                .ConfigureAwait(false);

            return await GetFullClubExceptScores(guid)
                .ConfigureAwait(false);
        }

        public async Task<Model.Club> GetFullClubExceptScores(Guid id)
        {
            return await GetFullClub(id, false)
                .ConfigureAwait(false);
        }

        private async Task<Club> GetFullClub(Guid id,
            bool includeScores)
        {
            IQueryable<Db.Club> clubQuery =
                _dbContext.Clubs.Where(c => c.Id == id)
                    .Include(c => c.Seasons);
            var club = await clubQuery.FirstAsync()
                .ConfigureAwait(false);

            if (includeScores)
            {
                clubQuery
                    .Include(c => c.Races)
                    .ThenInclude(r => r.Scores)
                    .Include(c => c.Races)
                    .ThenInclude(r => r.Fleet);
            }

            await clubQuery
                .Include(c => c.Seasons)
                .Include(c => c.Series)
                .ThenInclude(s => s.RaceSeries)
                .Include(c => c.Series)
                .ThenInclude(s => s.Season)
                .Include(c => c.Competitors)
                .Include(c => c.BoatClasses)
                .Include(c => c.DefaultScoringSystem)
                .Include(c => c.ScoringSystems)
                .Include(c => c.Fleets)
                .ThenInclude(f => f.CompetitorFleets)
                .Include(c => c.Fleets)
                .ThenInclude(f => f.FleetBoatClasses)
                .Include(c => c.Regattas)
                .ThenInclude(r => r.RegattaSeries)
                .Include(c => c.Regattas)
                .ThenInclude(r => r.RegattaFleet)
                .Include(c => c.WeatherSettings)
                .AsSplitQuery()
                .LoadAsync().ConfigureAwait(false);

            var retClub = _mapper.Map<Model.Club>(club);

            retClub.Seasons = retClub.Seasons.OrderByDescending(s => s.Start).ToList();
            retClub.Series = retClub.Series
                .OrderByDescending(s => s.Season.Start)
                .ThenBy(s => s.Name)
                .ToList();
            retClub.BoatClasses = retClub.BoatClasses
                .OrderBy(c => c.Name).ToList();
            return retClub;
        }

        public async Task<Model.Club> GetMinimalClub(Guid id)
        {

            var dbClub = await _dbContext.Clubs
                .Include(c => c.WeatherSettings)
                .FirstAsync(c => c.Id == id)
                .ConfigureAwait(false);

            var retClub = _mapper.Map<Model.Club>(dbClub);

            return retClub;

        }
        public async Task<Model.Club> GetMinimalClub(string clubInitials)
        {

            var dbClub = await _dbContext.Clubs
                .Include(c => c.WeatherSettings)
                .FirstAsync(c => c.Initials == clubInitials)
                .ConfigureAwait(false);

            var retClub = _mapper.Map<Model.Club>(dbClub);

            return retClub;
        }

        public async Task<string> GetClubName(string clubInitials)
        {
            return await _dbContext.Clubs
                .Where(
                c => c.Initials == clubInitials)
                .Select(c => c.Name)
                .FirstOrDefaultAsync();
        }

        public async Task<Guid> SaveNewClub(Club club)
        {
            if (_dbContext.Clubs.Any(c => c.Initials == club.Initials))
            {
                throw new InvalidOperationException("Cannot create club." +
                    " A club with those initials already exists.");
            }
            club.Id = Guid.NewGuid();


            var defaultSystem = club.DefaultScoringSystem;
            if (defaultSystem.Id == default)
            {
                defaultSystem.Id = Guid.NewGuid();
            }
            if (defaultSystem != null)
            {
                club.ScoringSystems ??= new List<ScoringSystem>();
                if (!club.ScoringSystems.Any(ss => ss == defaultSystem))
                {
                    club.ScoringSystems.Add(defaultSystem);
                }
            }
            if ((club.ScoringSystems?.Count ?? 0) > 0)
            {
                foreach (var system in club.ScoringSystems)
                {
                    if (system.Id != defaultSystem.Id)
                    {
                        system.ClubId = club.Id;
                    }
                }
            }

            var dbClub = _mapper.Map<Db.Club>(club);
            dbClub.DefaultScoringSystem = null;
            dbClub.DefaultScoringSystemId = null;
            dbClub.DefaultRaceDateOffset = 0;
            _dbContext.Clubs.Add(dbClub);
            var dbFleet = new Db.Fleet
            {
                Id = Guid.NewGuid(),
                ClubId = dbClub.Id,
                FleetType = Api.Enumerations.FleetType.AllBoatsInClub,
                IsHidden = false,
                ShortName = "All",
                Name = "All Boats in Club"
            };
            _dbContext.Fleets.Add(dbFleet);
            await _dbContext.SaveChangesAsync()
                .ConfigureAwait(false);
            // save twice here to avoid circular reference.
            dbClub.DefaultScoringSystemId = defaultSystem.Id;
            await _dbContext.SaveChangesAsync()
                .ConfigureAwait(false);
            return club.Id;
        }

        public async Task SaveNewFleet(Fleet fleet)
        {
            var dbFleet = _mapper.Map<Db.Fleet>(fleet);
            _dbContext.Fleets.Add(dbFleet);
            await _dbContext.SaveChangesAsync()
                .ConfigureAwait(false);
        }

        public async Task SaveNewSeason(Season season)
        {
            var dbSeason = _mapper.Map<Db.Season>(season);
            if(String.IsNullOrWhiteSpace(dbSeason.UrlName))
            {
                dbSeason.UrlName = UrlUtility.GetUrlName(dbSeason.Name);
            }
            _dbContext.Seasons.Add(dbSeason);
            await _dbContext.SaveChangesAsync()
                .ConfigureAwait(false);
        }

        public async Task UpdateClub(Club club)
        {
            if (_dbContext.Clubs.Any(s =>
                s.Id != club.Id
                && s.Initials == club.Initials))
            {
                throw new InvalidOperationException("Cannot update club. A club with these initials already exists.");
            }

            // don't update initials or id!
            var dbClub = _dbContext.Clubs
                .Include(c => c.WeatherSettings)
                .Single(c => c.Id == club.Id);
            dbClub.Name = club.Name;
            dbClub.IsHidden = club.IsHidden;
            dbClub.Url = club.Url;
            dbClub.Description = club.Description;
            dbClub.DefaultScoringSystemId = club.DefaultScoringSystemId;
            dbClub.ShowClubInResults = club.ShowClubInResults;
            dbClub.Locale = club.Locale;
            dbClub.DefaultRaceDateOffset = club.DefaultRaceDateOffset;
            dbClub.LogoFileId = club.LogoFileId;
            dbClub.HomePageDescription = club.HomePageDescription;
            dbClub.ShowCalendarInNav = club.ShowCalendarInNav;

            dbClub.WeatherSettings ??= new Database.Entities.WeatherSettings();
            dbClub.WeatherSettings.Latitude = club.WeatherSettings.Latitude;
            dbClub.WeatherSettings.Longitude = club.WeatherSettings.Longitude;
            dbClub.WeatherSettings.TemperatureUnits = club.WeatherSettings.TemperatureUnits;
            dbClub.WeatherSettings.WindSpeedUnits = club.WeatherSettings.WindSpeedUnits;

            await _dbContext.SaveChangesAsync()
                .ConfigureAwait(false);
        }

        public async Task<Guid> CopyClubAsync(Guid copyFromClubId, Club targetClub)
        {

            var dbClub = await _dbContext.Clubs.Where(c => c.Id == copyFromClubId)
                .AsNoTracking()
                .Include(c => c.ScoringSystems)
                .ThenInclude(s => s.ScoreCodes)
                .Include(c => c.Competitors)
                .ThenInclude(c => c.CompetitorFleets)
                .Include(c => c.Fleets)
                .ThenInclude(f => f.FleetBoatClasses)
                .Include(c => c.BoatClasses)
                .Include(c => c.Seasons)
                .FirstAsync()
                .ConfigureAwait(false);

            dbClub.Id = GetNewGuid(dbClub.Id);

            dbClub.DefaultScoringSystem = null;

            foreach (var scoringSystem in dbClub.ScoringSystems)
            {
                scoringSystem.Id = GetNewGuid(scoringSystem.Id);
                scoringSystem.ClubId = dbClub.Id;
                foreach (var scoreCode in scoringSystem.ScoreCodes)
                {
                    scoreCode.Id = GetNewGuid(scoreCode.Id);
                    scoreCode.ScoringSystemId = GetNewGuid(scoreCode.ScoringSystemId);
                }
            }
            // cycle through again, now that we've set all new Ids.
            foreach (var scoringSystem in dbClub.ScoringSystems)
            {
                scoringSystem.ParentSystem = null;
                // This allows that some parent systems might not be owned by this club.
                scoringSystem.ParentSystemId = GetNewGuidIfSet(scoringSystem.ParentSystemId);
            }

            foreach (var boatClass in dbClub.BoatClasses)
            {
                boatClass.Id = GetNewGuid(boatClass.Id);
                boatClass.ClubId = dbClub.Id;
            }

            foreach (var comp in dbClub.Competitors)
            {
                comp.Id = GetNewGuid(comp.Id);
                comp.ClubId = dbClub.Id;
                comp.BoatClass = null;
                comp.BoatClassId = GetNewGuid(comp.BoatClassId);
            }

            foreach (var fleet in dbClub.Fleets)
            {
                fleet.Id = GetNewGuid(fleet.Id);
                fleet.ClubId = dbClub.Id;
                foreach (var fleetBoatClass in fleet.FleetBoatClasses)
                {
                    fleetBoatClass.FleetId = fleet.Id;
                    fleetBoatClass.Fleet = null;
                    fleetBoatClass.BoatClassId = GetNewGuid(fleetBoatClass.BoatClassId);
                    fleetBoatClass.BoatClass = null;
                }
            }

            // cycle through competitors a second time.
            foreach (var comp in dbClub.Competitors)
            {
                foreach (var compFleet in comp.CompetitorFleets)
                {
                    compFleet.CompetitorId = comp.Id;
                    compFleet.Competitor = null;
                    compFleet.FleetId = GetNewGuid(compFleet.FleetId);
                    compFleet.Fleet = null;
                }
            }
            foreach (var season in dbClub.Seasons)
            {
                season.ClubId = dbClub.Id;
                season.Id = GetNewGuid(season.Id);
            }

            dbClub.Initials = targetClub.Initials;
            dbClub.Name = targetClub.Name;
            dbClub.Url = targetClub.Url;
            dbClub.IsHidden = targetClub.IsHidden;

            Guid? oldDefaultScoringSystemId = dbClub.DefaultScoringSystemId;
            dbClub.DefaultScoringSystemId = null;

            _dbContext.Clubs.Add(dbClub);
            await _dbContext.SaveChangesAsync()
                .ConfigureAwait(false);

            var newScoringSystemId = GetNewGuidIfSet(oldDefaultScoringSystemId);
            dbClub.DefaultScoringSystemId =
                dbClub.ScoringSystems.Any(s => s.Id == newScoringSystemId)
                    ? newScoringSystemId
                    : oldDefaultScoringSystemId;

            await _dbContext.SaveChangesAsync()
                .ConfigureAwait(false);
            return dbClub.Id;
        }

        public async Task<bool> DoesClubHaveCompetitors(Guid clubId)
        {
            return await _dbContext.Competitors.AnyAsync(c => c.ClubId == clubId)
                .ConfigureAwait(false);
        }

        public async Task<IList<Db.ClubSeasonStats>> GetClubStats(string clubInitials)
        {
            return await _dbContext.GetClubStats(clubInitials).ConfigureAwait(false);
        }

        public async Task<IList<Db.SiteStats>> GetAllStats()
        {
            return await _dbContext.GetSiteStats().ConfigureAwait(false);
        }

        public async Task UpdateStatsDescription(Guid clubId, string statisticsDescription)
        {
            var dbClub = await _dbContext.Clubs
                .SingleAsync(c => c.Id == clubId).ConfigureAwait(false);
            dbClub.StatisticsDescription = statisticsDescription;
            await _dbContext.SaveChangesAsync().ConfigureAwait(false);
        }

        public async Task SetUseAdvancedFeaturesAsync(Guid clubId, bool enabled)
        {
            var dbClub = await _dbContext.Clubs.SingleAsync(c => c.Id == clubId).ConfigureAwait(false);
            
            // If turning advanced features on (from null or false to true), set the enabled date
            if (enabled && !(dbClub.UseAdvancedFeatures ?? false))
            {
                dbClub.AdvancedFeaturesEnabledDate = DateTime.UtcNow;
            }
            
            dbClub.UseAdvancedFeatures = enabled;
            await _dbContext.SaveChangesAsync().ConfigureAwait(false);
        }

        public async Task SetSubscriptionTypeAsync(Guid clubId, string subscriptionType)
        {
            var dbClub = await _dbContext.Clubs.SingleAsync(c => c.Id == clubId).ConfigureAwait(false);
            dbClub.SubscriptionType = subscriptionType;
            await _dbContext.SaveChangesAsync().ConfigureAwait(false);
        }

        private Guid GetNewGuid(Guid oldGuid)
        {
            guidMapper ??= new Dictionary<Guid, Guid>();
            if (!guidMapper.TryGetValue(oldGuid, out Guid value))
            {
                value = Guid.NewGuid();
                guidMapper.Add(oldGuid, value);
            }
            return value;
        }

        private Guid? GetNewGuidIfSet(Guid? oldGuid)
        {
            if (!oldGuid.HasValue)
            {
                return null;
            }
            guidMapper ??= new Dictionary<Guid, Guid>();
            if (guidMapper.TryGetValue(oldGuid.Value, out Guid value))
            {
                return value;
            }
            return oldGuid;
        }

        public async Task<bool> HasCompetitorsAsync(Guid id)
        {
            return await _dbContext.Competitors
                .AnyAsync(r => r.ClubId == id)
                .ConfigureAwait(false);
        }

        public async Task SaveFileAsync(Db.File file)
        {
            await _dbContext.Files.AddAsync(file);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<Db.File> GetFileAsync(Guid id)
        {
            return await _dbContext.Files.FirstOrDefaultAsync(f => f.Id == id);
        }

        public async Task ResetClubAsync(Guid clubId, Model.ResetLevel resetLevel)
        {
            // Verify club exists
            var club = await _dbContext.Clubs
                .FirstOrDefaultAsync(c => c.Id == clubId)
                .ConfigureAwait(false);
            
            if (club == null)
            {
                throw new InvalidOperationException("Club not found.");
            }

            // Level 1, 2, and 3: Clear scores first (foreign key to races and competitors)
            var scores = await _dbContext.Scores
                .Where(s => s.Race.ClubId == clubId)
                .ToListAsync()
                .ConfigureAwait(false);
            _dbContext.Scores.RemoveRange(scores);

            // Clear race-series relationships
            var raceSeries = await _dbContext.Series
                .Where(s => s.ClubId == clubId)
                .SelectMany(s => s.RaceSeries)
                .ToListAsync()
                .ConfigureAwait(false);

            foreach (var rs in raceSeries)
            {
                rs.Race = null;
            }

            // Clear races
            var races = await _dbContext.Races
                .Where(r => r.ClubId == clubId)
                .ToListAsync()
                .ConfigureAwait(false);
            _dbContext.Races.RemoveRange(races);

            // Clear series chart results (via Series navigation)
            var chartResults = await _dbContext.SeriesChartResults
                .Where(scr => scr.Series.ClubId == clubId)
                .ToListAsync()
                .ConfigureAwait(false);
            _dbContext.SeriesChartResults.RemoveRange(chartResults);

            // Clear historical results (via Series navigation)
            var historicalResults = await _dbContext.HistoricalResults
                .Where(hr => hr.Series.ClubId == clubId)
                .ToListAsync()
                .ConfigureAwait(false);
            _dbContext.HistoricalResults.RemoveRange(historicalResults);

            // Get series IDs for this club
            var seriesIds = await _dbContext.Series
                .Where(s => s.ClubId == clubId)
                .Select(s => s.Id)
                .ToListAsync()
                .ConfigureAwait(false);

            // Clear series-to-series links (parent or child relationships)
            var seriesToSeriesLinks = await _dbContext.SeriesToSeriesLinks
                .Where(ssl => seriesIds.Contains(ssl.ParentSeriesId) || seriesIds.Contains(ssl.ChildSeriesId))
                .ToListAsync()
                .ConfigureAwait(false);
            _dbContext.SeriesToSeriesLinks.RemoveRange(seriesToSeriesLinks);

            // Clear series forwarders (use NewSeriesId)
            var seriesForwarders = await _dbContext.SeriesForwarders
                .Where(sf => seriesIds.Contains(sf.NewSeriesId))
                .ToListAsync()
                .ConfigureAwait(false);
            _dbContext.SeriesForwarders.RemoveRange(seriesForwarders);

            // Clear series
            var series = await _dbContext.Series
                .Where(s => s.ClubId == clubId)
                .ToListAsync()
                .ConfigureAwait(false);
            _dbContext.Series.RemoveRange(series);

            // Get regatta IDs for this club (needed for announcements that reference regattas)
            var regattaIds = await _dbContext.Regattas
                .Where(r => r.ClubId == clubId)
                .Select(r => r.Id)
                .ToListAsync()
                .ConfigureAwait(false);

            // Clear announcements (both by ClubId and those referencing the regattas being deleted)
            // Must be done before deleting regattas due to foreign key constraint
            // Use IgnoreQueryFilters to include soft-deleted announcements
            var announcements = await _dbContext.Announcements
                .IgnoreQueryFilters()
                .Where(a => a.ClubId == clubId || (a.RegattaId.HasValue && regattaIds.Contains(a.RegattaId.Value)))
                .ToListAsync()
                .ConfigureAwait(false);
            _dbContext.Announcements.RemoveRange(announcements);
            await _dbContext.SaveChangesAsync().ConfigureAwait(false);

            // Clear regatta forwarders (use NewRegattaId)
            var regattaForwarders = await _dbContext.RegattaForwarders
                .Where(rf => regattaIds.Contains(rf.NewRegattaId))
                .ToListAsync()
                .ConfigureAwait(false);
            _dbContext.RegattaForwarders.RemoveRange(regattaForwarders);
            await _dbContext.SaveChangesAsync().ConfigureAwait(false);

            // Clear regattas with their series and fleet relationships
            var regattas = await _dbContext.Regattas
                .Include(r => r.RegattaSeries)
                .Include(r => r.RegattaFleet)
                .Where(r => r.ClubId == clubId)
                .AsSplitQuery()
                .ToListAsync()
                .ConfigureAwait(false);
            
            foreach (var regatta in regattas)
            {
                regatta.RegattaSeries.Clear();
                regatta.RegattaFleet.Clear();
            }

            _dbContext.Regattas.RemoveRange(regattas);
            await _dbContext.SaveChangesAsync().ConfigureAwait(false);

            // Level 2 and 3: Clear competitors
            if (resetLevel >= Model.ResetLevel.RacesSeriesAndCompetitors)
            {
                // Get competitor IDs for this club
                var competitorIds = await _dbContext.Competitors
                    .Where(c => c.ClubId == clubId)
                    .Select(c => c.Id)
                    .ToListAsync()
                    .ConfigureAwait(false);

                // Clear competitor changes (use CompetitorId)
                var competitorChanges = await _dbContext.CompetitorChanges
                    .Where(cc => competitorIds.Contains(cc.CompetitorId))
                    .ToListAsync()
                    .ConfigureAwait(false);
                _dbContext.CompetitorChanges.RemoveRange(competitorChanges);

                // Clear competitor forwarders (use CompetitorId)
                var competitorForwarders = await _dbContext.CompetitorForwarders
                    .Where(cf => competitorIds.Contains(cf.CompetitorId))
                    .ToListAsync()
                    .ConfigureAwait(false);
                _dbContext.CompetitorForwarders.RemoveRange(competitorForwarders);

                // Clear competitor-fleet relationships
                var competitors = await _dbContext.Competitors
                    .Include(c => c.CompetitorFleets)
                    .Where(c => c.ClubId == clubId)
                    .ToListAsync()
                    .ConfigureAwait(false);
                
                foreach (var comp in competitors)
                {
                    comp.CompetitorFleets.Clear();
                }

                _dbContext.Competitors.RemoveRange(competitors);
            }

            // Level 3: Full reset - clear fleets, boat classes, seasons, scoring systems
            if (resetLevel == Model.ResetLevel.FullReset)
            {
                // Clear fleet-boat class relationships and fleets
                var fleets = await _dbContext.Fleets
                    .Include(f => f.FleetBoatClasses)
                    .Include(f => f.CompetitorFleets)
                    .Where(f => f.ClubId == clubId)
                    .AsSplitQuery()
                    .ToListAsync()
                    .ConfigureAwait(false);
                
                foreach (var fleet in fleets)
                {
                    fleet.FleetBoatClasses.Clear();
                    fleet.CompetitorFleets.Clear();
                }
                _dbContext.Fleets.RemoveRange(fleets);

                // Clear boat classes
                var boatClasses = await _dbContext.BoatClasses
                    .Where(bc => bc.ClubId == clubId)
                    .ToListAsync()
                    .ConfigureAwait(false);
                _dbContext.BoatClasses.RemoveRange(boatClasses);

                // Clear seasons
                var seasons = await _dbContext.Seasons
                    .Where(s => s.ClubId == clubId)
                    .ToListAsync()
                    .ConfigureAwait(false);
                _dbContext.Seasons.RemoveRange(seasons);

                // Clear documents
                var documents = await _dbContext.Documents
                    .Where(d => d.ClubId == clubId)
                    .ToListAsync()
                    .ConfigureAwait(false);
                _dbContext.Documents.RemoveRange(documents);

                // Clear club scoring systems (not site-wide ones)
                // First, clear the default scoring system reference
                club.DefaultScoringSystemId = null;
                await _dbContext.SaveChangesAsync().ConfigureAwait(false);

                // Get scoring system IDs for this club
                var scoringSystemIds = await _dbContext.ScoringSystems
                    .Where(ss => ss.ClubId == clubId)
                    .Select(ss => ss.Id)
                    .ToListAsync()
                    .ConfigureAwait(false);

                // Clear score codes for club scoring systems (use ScoringSystemId)
                var scoreCodes = await _dbContext.ScoreCodes
                    .Where(sc => scoringSystemIds.Contains(sc.ScoringSystemId))
                    .ToListAsync()
                    .ConfigureAwait(false);
                _dbContext.ScoreCodes.RemoveRange(scoreCodes);

                // Clear club scoring systems
                var scoringSystems = await _dbContext.ScoringSystems
                    .Where(ss => ss.ClubId == clubId)
                    .ToListAsync()
                    .ConfigureAwait(false);
                _dbContext.ScoringSystems.RemoveRange(scoringSystems);

                await _dbContext.SaveChangesAsync().ConfigureAwait(false);

                // Create default scoring systems using the consolidated method
                var createdSystems = await _scoringService.CreateDefaultScoringSystemsAsync(clubId, club.Initials)
                    .ConfigureAwait(false);

                if (createdSystems.Count > 0)
                {
                    // Set the first system (series default) as the club's default
                    club.DefaultScoringSystemId = createdSystems[0].Id;
                }
            }

            await _dbContext.SaveChangesAsync().ConfigureAwait(false);

            // Clear cache for this club
            _cache.Remove($"ClubId_{club.Initials}");
        }
    }
}
