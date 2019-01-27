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

        public async Task SaveNew(Fleet fleet)
        {
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
            _dbContext.Fleets.Add(dbFleet);

            //todo: save classes.
            await _dbContext.SaveChangesAsync();

        }

        public async Task Update(Fleet fleet)
        {
            var existingFleet = await _dbContext.Fleets
                .Include(f => f.FleetBoatClasses)
                .SingleAsync(c => c.Id == fleet.Id);

            existingFleet.Name = fleet.Name;
            existingFleet.Description = fleet.Description;
            existingFleet.FleetType = fleet.FleetType;

            var classesToRemove = existingFleet.FleetBoatClasses.ToList();

            if (existingFleet.FleetType == Api.Enumerations.FleetType.SelectedClasses
                && fleet.BoatClasses != null) {
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
                :new List<Db.FleetBoatClass>();

            foreach (var removingClass in classesToRemove)
            {
                existingFleet.FleetBoatClasses.Remove(removingClass);
            }
            foreach (var addClass in classesToAdd)
            {
                existingFleet.FleetBoatClasses.Add(addClass);
            }

            await _dbContext.SaveChangesAsync();
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
