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
using SailScores.Api.Dtos;

namespace SailScores.Core.Services
{
    public class ClubService : IClubService
    {
        private readonly ISailScoresContext _dbContext;
        private readonly IMapper _mapper;
        
        //used for copying club
        private Dictionary<Guid, Guid> guidMapper;

        public ClubService(
            ISailScoresContext dbContext,
            IMapper mapper)
        {
            _dbContext = dbContext;
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
                .ToListAsync();

            var bizObj = _mapper.Map<IList<Fleet>>(dbFleets);

            // ignored in mapper to avoid loops.
            foreach(var fleet in dbFleets)
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

        public async Task<IList<Model.Club>> GetClubs(bool includeHidden)
        {
            var dbObjects = await _dbContext
                .Clubs
                .Where(c => includeHidden || !c.IsHidden)
                .ToListAsync();
            return _mapper.Map<List<Model.Club>>(dbObjects);
        }

        public async Task<Guid> GetClubId(string initials)
        {
            Guid clubGuid;
            if (!Guid.TryParse(initials, out clubGuid))
            {
                clubGuid = (await _dbContext.Clubs.SingleAsync(c => c.Initials == initials)).Id;
            }

            return clubGuid;
        }

        public async Task<Model.Club> GetFullClub(string id)
        {
            var guid = await GetClubId(id);

            return await GetFullClub(guid);
        }

        public async Task<Model.Club> GetFullClub(Guid id)
        {
            IQueryable<Db.Club> clubQuery =
                            _dbContext.Clubs.Where(c => c.Id == id);

            var club = await clubQuery
                .Include(c => c.Races)
                    .ThenInclude(r => r.Scores)
                .Include(c => c.Races)
                    .ThenInclude(r => r.Fleet)
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
                .FirstOrDefaultAsync();

            var retClub = _mapper.Map<Model.Club>(club);

            retClub.Seasons = retClub.Seasons.OrderByDescending(s => s.Start).ToList();
            retClub.Series = retClub.Series
                .OrderByDescending(s => s.Season.Start)
                .ThenBy(s => s.Name)
                .ToList();
            return retClub;
        }

        public async Task<Model.Club> GetMinimalClub(Guid id)
        {

            var dbClub = await _dbContext.Clubs.FirstAsync(c => c.Id == id);

            var retClub = _mapper.Map<Model.Club>(dbClub);

            return retClub;

        }

        public async Task<Guid> SaveNewClub(Club club)
        {
            if(_dbContext.Clubs.Any(c => c.Initials == club.Initials))
            {
                throw new InvalidOperationException("Cannot create club." +
                    " A club with those initials already exists.");
            }
            club.Id = Guid.NewGuid();
            var dbClub = _mapper.Map<Db.Club>(club);
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
            await _dbContext.SaveChangesAsync();
            return club.Id;
        }

        public async Task SaveNewFleet(Fleet fleet)
        {
            var dbFleet = _mapper.Map<Db.Fleet>(fleet);
            _dbContext.Fleets.Add(dbFleet);
            await _dbContext.SaveChangesAsync();
        }

        public async Task SaveNewSeason(Season season)
        {
            var dbSeason = _mapper.Map<Db.Season>(season);
            _dbContext.Seasons.Add(dbSeason);
            await _dbContext.SaveChangesAsync();
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
            var dbClub = _dbContext.Clubs.Single(c => c.Id == club.Id);
            dbClub.Name = club.Name;
            dbClub.IsHidden = club.IsHidden;
            dbClub.Url = club.Url;
            dbClub.Description = club.Description;
            dbClub.DefaultScoringSystemId = club.DefaultScoringSystemId;
            await _dbContext.SaveChangesAsync();
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
                .FirstAsync();

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
                foreach(var fleetBoatClass in fleet.FleetBoatClasses)
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
            foreach(var season in dbClub.Seasons)
            {
                season.ClubId = dbClub.Id;
                season.Id = GetNewGuid(season.Id);
            }

            dbClub.Initials = targetClub.Initials;
            dbClub.Name = targetClub.Name;
            dbClub.Url = targetClub.Url;
            dbClub.IsHidden = targetClub.IsHidden;

            Guid? oldDefaultScoringSystemId = null;
            if(!(GetNewGuidIfSet(dbClub.DefaultScoringSystemId) == oldDefaultScoringSystemId))
            {
                oldDefaultScoringSystemId = dbClub.DefaultScoringSystemId;
                dbClub.DefaultScoringSystemId = null;
            }

            _dbContext.Clubs.Add(dbClub);
            await _dbContext.SaveChangesAsync();
            if (oldDefaultScoringSystemId != null)
            {
                dbClub.DefaultScoringSystemId = GetNewGuidIfSet(oldDefaultScoringSystemId);
            }
            await _dbContext.SaveChangesAsync();
            return dbClub.Id;
        }

        private Guid? GetNewGuid(Guid? oldGuid)
        {
            if (!oldGuid.HasValue)
            {
                return null;
            }
            return GetNewGuid(oldGuid.Value);
        }

        private Guid GetNewGuid(Guid oldGuid)
        {
            if(guidMapper == null)
            {
                guidMapper = new Dictionary<Guid, Guid>();
            }
            if (!guidMapper.ContainsKey(oldGuid))
            {
                guidMapper.Add(oldGuid, Guid.NewGuid());
            }
            return guidMapper[oldGuid];
        }

        private Guid? GetNewGuidIfSet(Guid? oldGuid)
        {
            if (!oldGuid.HasValue)
            {
                return null;
            }
            if (guidMapper == null)
            {
                guidMapper = new Dictionary<Guid, Guid>();
            }
            if (guidMapper.ContainsKey(oldGuid.Value))
            {
                return guidMapper[oldGuid.Value];
            }
            return oldGuid;
        }
    }
}
