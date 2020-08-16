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

            _service.SaveNew(newFleet);

            Assert.NotEmpty(_context.Fleets
                .Where(f => f.Name == newFleet.Name));
            Assert.Equal(startingFleetCount + 1,
                _context.Fleets.Count());
        }
    }
}
