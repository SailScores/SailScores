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
            var dbClass = await _dbContext.Fleets.SingleAsync(c => c.Id == fleetId);
            _dbContext.Fleets.Remove(dbClass);
            await _dbContext.SaveChangesAsync();
        }

        public async Task SaveNew(Fleet fleet)
        {
            var dbFleet =_mapper.Map<Db.Fleet>(fleet);
            dbFleet.Id = Guid.NewGuid();
            _dbContext.Fleets.Add(dbFleet);
            await _dbContext.SaveChangesAsync();

        }

        public async Task Update(Fleet fleet)
        {
            var existingFleet = await _dbContext.Fleets
                .SingleAsync(c => c.Id == fleet.Id);

            existingFleet.Name = fleet.Name;
            existingFleet.Description = fleet.Description;
            await _dbContext.SaveChangesAsync();
        }
    }
}
