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

            _dbContext.Fleets.RemoveRange(fleets);
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
