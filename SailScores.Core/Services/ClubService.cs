using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SailScores.Database;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using SailScores.Core.Model;
using Db = SailScores.Database.Entities;
using SailScores.Core.Model.Summary;
using Microsoft.Extensions.Caching.Memory;

namespace SailScores.Core.Services
{
    public class ClubService : IClubService
    {
        private readonly ISailScoresContext _dbContext;
        private readonly IMemoryCache _cache;
        private readonly IMapper _mapper;

        //used for copying club
        private Dictionary<Guid, Guid> guidMapper;

        public ClubService(
            ISailScoresContext dbContext,
            IMemoryCache cache,
            IMapper mapper)
        {
            _dbContext = dbContext;
            _cache = cache;
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

        public async Task<IList<Fleet>> GetActiveFleets(Guid clubId)
        {
            var dbFleets = await _dbContext
                .Fleets
                .Include(f => f.FleetBoatClasses)
                    .ThenInclude(fbc => fbc.BoatClass)
                .Include(f => f.CompetitorFleets)
                    .ThenInclude(cf => cf.Competitor)
                .Where(f => f.ClubId == clubId && (f.IsActive ?? true))
                .AsSingleQuery()
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
    }
}
