using AutoMapper;
using SailScores.Core.Services;
using SailScores.Database;
using SailScores.Test.Unit.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SailScores.Core.Model;
using SailScores.Web.Models.SailScores;
using Xunit;

namespace SailScores.Test.Unit.Core.Services
{
    public class FleetServiceTests
    {
        FleetService _service;

        private readonly ISailScoresContext _context;
        private readonly IMapper _mapper;

        public FleetServiceTests()
        {
            _context = InMemoryContextBuilder.GetContext();
            _mapper = MapperBuilder.GetSailScoresMapper();
            _service = new FleetService(
                _context,
                _mapper);
        }

        [Fact]
        public async Task SaveNew_Always_SavesToDb()
        {
            var startingFleetCount = _context.Fleets.Count();

            var newFleet = new Fleet
            {
                Name = "myFleet"
            };

            await _service.SaveNew(newFleet);

            Assert.NotEmpty(_context.Fleets
                .Where(f => f.Name == newFleet.Name));
            Assert.Equal(startingFleetCount + 1,
                _context.Fleets.Count());
        }

        [Fact]
        public async Task Delete_Fleet_RemovesFromDb()
        {
            // Arrange
            var boatClass = await _context.BoatClasses.FirstAsync();
            var newFleet = new Fleet
            {
                Name = "myFleet",
                BoatClasses = new List<BoatClass>
                {
                    new BoatClass
                    {
                        Id = boatClass.Id,
                        ClubId = boatClass.ClubId,
                        Name = boatClass.Name
                    }
                }

            };

            await _service.SaveNew(newFleet);

            Assert.NotEmpty(_context.Fleets
                .Where(f => f.Name == newFleet.Name).SelectMany(
                f => f.FleetBoatClasses));

            var newFleetId = _context.Fleets
                .Where(f => f.Name == newFleet.Name).First().Id;

            //Act 
            await _service.Delete(newFleetId);

            // Assert
            Assert.Empty(_context.Fleets
                .Where(f => f.Name == newFleet.Name));

        }


        [Fact]
        public async Task Get_Fleet_ReturnsFromDb()
        {
            // Arrange
            var boatClass = await _context.BoatClasses.FirstAsync();
            var newFleet = new Fleet
            {
                Name = "myFleet",
                BoatClasses = new List<BoatClass>
                {
                    new BoatClass
                    {
                        Id = boatClass.Id,
                        ClubId = boatClass.ClubId,
                        Name = boatClass.Name
                    }
                }

            };

            await _service.SaveNew(newFleet);

            Assert.NotEmpty(_context.Fleets
                .Where(f => f.Name == newFleet.Name).SelectMany(
                f => f.FleetBoatClasses));

            var newFleetId = _context.Fleets
                .Where(f => f.Name == newFleet.Name).First().Id;

            //Act 
            var testresult = await _service.Get(newFleetId);

            // Assert
            Assert.Equal(newFleet.Name, testresult.Name);

        }
    }
}
