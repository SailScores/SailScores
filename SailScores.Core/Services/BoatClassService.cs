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
            var dbClass = await _dbContext.BoatClasses.SingleAsync(c => c.Id == boatClassId);
            _dbContext.BoatClasses.Remove(dbClass);
            await _dbContext.SaveChangesAsync();
        }

        public async Task SaveNew(BoatClass boatClass)
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
            await _dbContext.SaveChangesAsync();

        }

        public async Task Update(BoatClass boatClass)
        {
            var existingClass = await _dbContext.BoatClasses.SingleAsync(c => c.Id == boatClass.Id);

            existingClass.Name = boatClass.Name;
            existingClass.Description = boatClass.Description;
            await _dbContext.SaveChangesAsync();
        }
    }
}
