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
    public class FleetService : IFleetService
    {
        private readonly ISailScoresContext _dbContext;
        private readonly IMapper _mapper;

        public FleetService(
            ISailScoresContext dbContext,
            IMapper mapper)
        {
            _dbContext = dbContext;
            _mapper = mapper;
        }

        public async Task Delete(Guid fleetId)
        {
            var dbClass = await _dbContext.Fleets
                .Include(f => f.FleetBoatClasses)
                .SingleAsync(c => c.Id == fleetId);
            foreach(var link in dbClass.FleetBoatClasses.ToList())
            {
                dbClass.FleetBoatClasses.Remove(link);
            }
            _dbContext.Fleets.Remove(dbClass);
            
            await _dbContext.SaveChangesAsync();
        }

        public async Task<Guid> SaveNew(Fleet fleet)
        {

            if (_dbContext.Fleets.Any(f =>
                f.ClubId == fleet.ClubId
                && (f.ShortName == fleet.ShortName
                ||f.Name == fleet.Name)))
            {
                throw new InvalidOperationException("Cannot create fleet. A fleet with this name or short name already exists.");
            }
            var dbFleet =_mapper.Map<Db.Fleet>(fleet);
            dbFleet.Id = Guid.NewGuid();
            dbFleet.FleetBoatClasses = new List<Db.FleetBoatClass>();
            if((fleet.BoatClasses?.Count() ?? 0) != 0)
            {
                foreach(var newClass in fleet.BoatClasses)
                {
                    dbFleet.FleetBoatClasses.Add(
                        new Db.FleetBoatClass
                        {
                            BoatClassId = newClass.Id,
                            FleetId = dbFleet.Id
                        });
                }
            }
            dbFleet.CompetitorFleets = new List<Db.CompetitorFleet>();
            if ((fleet.Competitors?.Count() ?? 0) != 0)
            {
                foreach (var newComp in fleet.Competitors)
                {
                    dbFleet.CompetitorFleets.Add(
                        new Db.CompetitorFleet
                        {
                            CompetitorId = newComp.Id,
                            FleetId = dbFleet.Id
                        });
                }
            }
            _dbContext.Fleets.Add(dbFleet);

            //todo: save classes.
            await _dbContext.SaveChangesAsync();
            return dbFleet.Id;

        }

        public async Task Update(Fleet fleet)
        {
            if (_dbContext.Fleets.Any(f =>
                f.Id != fleet.Id
                && f.ClubId == fleet.ClubId
                && (f.ShortName == fleet.ShortName
                || f.Name == fleet.Name)))
            {
                throw new InvalidOperationException("Cannot update fleet. A fleet with this name or short name already exists.");
            }
            var existingFleet = await _dbContext.Fleets
                .Include(f => f.FleetBoatClasses)
                .Include(f => f.CompetitorFleets)
                .SingleAsync(c => c.Id == fleet.Id);

            existingFleet.ShortName = fleet.ShortName;
            existingFleet.Name = fleet.Name;
            existingFleet.NickName = fleet.NickName;
            existingFleet.Description = fleet.Description;
            existingFleet.FleetType = fleet.FleetType;

            CleanUpClasses(fleet, existingFleet);
            CleanUpCompetitors(fleet, existingFleet);

            await _dbContext.SaveChangesAsync();
        }

        private static void CleanUpClasses(Fleet fleet, Db.Fleet existingFleet)
        {
            var classesToRemove = existingFleet.FleetBoatClasses.ToList();

            if (existingFleet.FleetType == Api.Enumerations.FleetType.SelectedClasses
                && fleet.BoatClasses != null)
            {
                classesToRemove =
                existingFleet.FleetBoatClasses
                .Where(f => !(fleet.BoatClasses.Any(c => c.Id == f.BoatClassId)))
                .ToList();
            }
            var classesToAdd =
                fleet.BoatClasses != null ?
                fleet.BoatClasses
                .Where(c =>
                    !(existingFleet.FleetBoatClasses.Any(f => c.Id == f.BoatClassId)))
                    .Select(c => new Db.FleetBoatClass { BoatClassId = c.Id, FleetId = existingFleet.Id })
                : new List<Db.FleetBoatClass>();

            foreach (var removingClass in classesToRemove)
            {
                existingFleet.FleetBoatClasses.Remove(removingClass);
            }
            foreach (var addClass in classesToAdd)
            {
                existingFleet.FleetBoatClasses.Add(addClass);
            }
        }

        private static void CleanUpCompetitors(Fleet fleet, Db.Fleet existingFleet)
        {
            var competitorsToRemove = existingFleet.CompetitorFleets.ToList();
            var competitorsToAdd =
                fleet.Competitors != null ?
                fleet.Competitors
                .Where(c =>
                    !(existingFleet.CompetitorFleets.Any(f => c.Id == f.CompetitorId)))
                    .Select(c => new Db.CompetitorFleet { CompetitorId = c.Id, FleetId = existingFleet.Id })
                : new List<Db.CompetitorFleet>();

            if (existingFleet.FleetType == Api.Enumerations.FleetType.SelectedBoats
                && fleet.Competitors != null)
            {
                competitorsToRemove =
                existingFleet.CompetitorFleets
                .Where(c => !(fleet.Competitors.Any(cp => cp.Id == c.CompetitorId)))
                .ToList();
            }


            foreach (var removingCompetitor in competitorsToRemove)
            {
                existingFleet.CompetitorFleets.Remove(removingCompetitor);
            }
            foreach (var addCompetitor in competitorsToAdd)
            {
                existingFleet.CompetitorFleets.Add(addCompetitor);
            }
        }

        public async Task<Fleet> Get(Guid fleetId)
        {
            var dbClass = await _dbContext.Fleets
                .Include(f => f.FleetBoatClasses)
                .SingleAsync(c => c.Id == fleetId);
            return _mapper.Map<Fleet>(dbClass);

        }
    }
}
