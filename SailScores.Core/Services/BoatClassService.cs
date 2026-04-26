using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SailScores.Database;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using SailScores.Core.Model;
using Db = SailScores.Database.Entities;

namespace SailScores.Core.Services
{
    public class BoatClassService : IBoatClassService
    {
        private readonly ISailScoresContext _dbContext;
        private readonly IMapper _mapper;

        public BoatClassService(
            ISailScoresContext dbContext,
            IMapper mapper)
        {
            _dbContext = dbContext;
            _mapper = mapper;
        }

        public async Task Delete(Guid boatClassId)
        {
            var dbClass = await _dbContext.BoatClasses.SingleAsync(c => c.Id == boatClassId)
                .ConfigureAwait(false);
            var fleets = await _dbContext.Fleets
                .Where(f =>
                    f.FleetType == Api.Enumerations.FleetType.SelectedClasses
                    && f.FleetBoatClasses.Any(fbc =>
                        fbc.BoatClassId == dbClass.Id))
                .ToListAsync().ConfigureAwait(false);

            // if fleet has other classes, just remove this one.
            // otherwise delete the fleet as well.

            var fleetIds = fleets.Select(f => f.Id).ToList();

            var fleetBoatClasses = await _dbContext.FleetBoatClasses
                .Where(fbc => fleetIds.Contains(fbc.FleetId))
                .ToListAsync().ConfigureAwait(false);

            var fleetClassCounts = fleetBoatClasses
                .GroupBy(fbc => fbc.FleetId)
                .ToDictionary(g => g.Key, g => g.Count());

            var fleetsToDelete = fleets
                .Where(f => fleetClassCounts.TryGetValue(f.Id, out var classCount) && classCount == 1)
                .ToList();

            var fleetsToKeep = fleets
                .Where(f => fleetClassCounts.TryGetValue(f.Id, out var classCount) && classCount > 1)
                .ToList();

            var fleetsToDeleteIds = fleetsToDelete.Select(f => f.Id).ToList();
            var fleetsToKeepIds = fleetsToKeep.Select(f => f.Id).ToList();

            var fleetBoatClassesToRemove = fleetBoatClasses
                .Where(fbc =>
                    fleetsToDeleteIds.Contains(fbc.FleetId)
                    || (fleetsToKeepIds.Contains(fbc.FleetId) && fbc.BoatClassId == dbClass.Id))
                .ToList();

            if (fleetsToDeleteIds.Any())
            {
                var allBoatsFleet = await _dbContext.Fleets
                    .Where(f =>
                        f.ClubId == dbClass.ClubId
                        && f.FleetType == Api.Enumerations.FleetType.AllBoatsInClub)
                    .SingleOrDefaultAsync().ConfigureAwait(false);

                var races = await _dbContext.Races
                    .Where(r => fleetsToDeleteIds.Contains(r.Fleet.Id))
                    .ToListAsync().ConfigureAwait(false);

                if (races.Any())
                {
                    if (allBoatsFleet == null)
                    {
                        throw new InvalidOperationException(
                            $"Cannot delete boat class '{dbClass.Name}' because races are using a fleet " +
                            $"associated with this class, and no 'All Boats in Club' fleet exists to reassign them to.");
                    }

                    foreach (var race in races)
                    {
                        race.Fleet = allBoatsFleet;
                    }
                }

                var competitorFleets = await _dbContext.CompetitorFleets
                    .Where(cf => fleetsToDeleteIds.Contains(cf.FleetId))
                    .ToListAsync().ConfigureAwait(false);

                _dbContext.CompetitorFleets.RemoveRange(competitorFleets);
                _dbContext.Fleets.RemoveRange(fleetsToDelete);
            }

            _dbContext.FleetBoatClasses.RemoveRange(fleetBoatClassesToRemove);
            _dbContext.BoatClasses.Remove(dbClass);
            await _dbContext.SaveChangesAsync().ConfigureAwait(false);
        }

        public async Task SaveNew(BoatClass boatClass)
        {
            var dbBoatClass = _mapper.Map<Db.BoatClass>(boatClass);
            dbBoatClass.Id = Guid.NewGuid();
            _dbContext.BoatClasses.Add(dbBoatClass);
            var defaultShortName = new string(
                boatClass.Name
                .Where(c => !Char.IsWhiteSpace(c))
                .ToArray());

            var fleetId = Guid.NewGuid();

            var fleetName = boatClass.Name.EndsWith('s') ? boatClass.Name : $"{boatClass.Name}s";
            var dbFleet = new Db.Fleet
            {
                Id = fleetId,
                ClubId = boatClass.ClubId,
                FleetType = Api.Enumerations.FleetType.SelectedClasses,
                IsHidden = false,
                ShortName = defaultShortName,
                Name = fleetName,
                FleetBoatClasses = new List<Db.FleetBoatClass>
                {
                    new Db.FleetBoatClass
                    {
                        BoatClassId = dbBoatClass.Id,
                        FleetId = Guid.NewGuid()
                    }
                }
            };
            _dbContext.Fleets.Add(dbFleet);
            await _dbContext.SaveChangesAsync().ConfigureAwait(false);

        }

        public async Task Update(BoatClass boatClass)
        {
            var existingClass = await _dbContext.BoatClasses
                .SingleAsync(c => c.Id == boatClass.Id)
                .ConfigureAwait(false);

            existingClass.Name = boatClass.Name;
            existingClass.Description = boatClass.Description;
            await _dbContext.SaveChangesAsync().ConfigureAwait(false);
        }

        public async Task<BoatClass> GetClass(Guid boatClassId)
        {
            var dbClass = await _dbContext.BoatClasses.SingleAsync(c => c.Id == boatClassId)
                .ConfigureAwait(false);

            return _mapper.Map<BoatClass>(dbClass);
        }

        public async Task<DeletableInfo> GetDeletableInfo(Guid id)
        {
            var boatCount = await _dbContext.Competitors.CountAsync(c => c.BoatClassId == id);

            return new DeletableInfo
            {
                IsDeletable = boatCount == 0,
                Reason = boatCount != 0 ? $"{boatCount} boats in this class" : ""
            };
        }
    }
}
