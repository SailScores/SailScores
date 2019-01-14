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

namespace SailScores.Core.Services
{
    public class ClubService : IClubService
    {
        private readonly ISailScoresContext _dbContext;
        private readonly IMapper _mapper;

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
                .Include(f => f.BoatClasses)
                .Include(f => f.CompetitorFleets)
                .Where(f => f.ClubId == clubId)
                .ToListAsync();

            var bizObj = _mapper.Map<IList<Fleet>>(dbFleets);

            // ignored in mapper to avoid loops.
            foreach(var fleet in dbFleets)
            {
                var boatClasses = fleet.BoatClasses;
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
            //    .ProjectTo<Model.Club>(_mapper.ConfigurationProvider)
            //    .ToListAsync();
            //return returnList;
        }

        public async Task<Model.Club> GetFullClub(string id)
        {
            Guid potentialClubId;
            IQueryable<Db.Club> clubQuery =
                Guid.TryParse(id, out potentialClubId) ?
                _dbContext.Clubs.Where(c => c.Id == potentialClubId) :
                _dbContext.Clubs.Where(c => c.Initials == id);

            var club = await clubQuery
                .Include(c => c.Races)
                    .ThenInclude(r => r.Scores)
                .Include(c => c.Races)
                    .ThenInclude(r => r.Fleet)
                .Include(c => c.ScoreCodes)
                .Include(c => c.Seasons)
                .Include(c => c.Series)
                    .ThenInclude(s => s.RaceSeries)
                .Include(c => c.Competitors)
                .Include(c => c.BoatClasses)
                .Include(c => c.Fleets)
                    .FirstOrDefaultAsync();

            return _mapper.Map<Model.Club>(club);
        }

        public async Task<Model.Club> GetFullClub(Guid id)
        {
            // not the best use of resources: to string, but then cast it back.
            return await GetFullClub(id.ToString());
        }

        public async Task SaveNewBoatClass(BoatClass boatClass)
        {
            var dbBoatClass =_mapper.Map<Db.BoatClass>(boatClass);
            dbBoatClass.Id = Guid.NewGuid();
            _dbContext.BoatClasses.Add(dbBoatClass);
            var defaultShortName = boatClass.Name.Split(' ')[0];
            var fleetId = Guid.NewGuid();
            var dbFleet = new Db.Fleet
            {
                Id = fleetId,
                ClubId = boatClass.ClubId,
                FleetType = Api.Enumerations.FleetType.SelectedClasses,
                IsHidden = false,
                ShortName = defaultShortName,
                Name = $"All {boatClass.Name}s",
                BoatClasses = new List<Db.FleetBoatClass>
                {
                    new Db.FleetBoatClass
                    {
                        BoatClassId = dbBoatClass.Id,
                        FleetId = fleetId
                    }
                }
            };
            _dbContext.Fleets.Add(dbFleet);
            await _dbContext.SaveChangesAsync();

        }

        public async Task SaveNewClub(Club club)
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
                Club = dbClub,
                FleetType = Api.Enumerations.FleetType.AllBoatsInClub,
                IsHidden = false,
                ShortName = "All",
                Name = "All Boats in Club"
            };
            _dbContext.Fleets.Add(dbFleet);
            await _dbContext.SaveChangesAsync();
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
    }
}
