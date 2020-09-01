using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Moq;
using SailScores.Core.Mapping;
using SailScores.Core.Scoring;
using SailScores.Core.Services;
using SailScores.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SailScores.Database.Entities;
using Xunit;

namespace SailScores.Test.Unit.Core.Services
{
    public class RegattaServiceTests
    {
        private readonly Regatta _regatta;
        private readonly DbObjectBuilder _dbObjectBuilder;
        private readonly RegattaService _service;
        private readonly IMapper _mapper;
        private readonly ISailScoresContext _context;
        private readonly Guid _clubId;
        private readonly Mock<ISeriesService> _mockSeriesService;
        private readonly string _clubInitials;

        public RegattaServiceTests()
        {

            _context = Utilities.InMemoryContextBuilder.GetContext();
            _clubInitials = _context.Clubs.First().Initials;
            _clubId = _context.Clubs.First().Id;
            _regatta = _context.Regattas.First();


            _mockSeriesService = new Mock<ISeriesService>();

            var config = new MapperConfiguration(opts =>
            {
                opts.AddProfile(new DbToModelMappingProfile());
            });

            _mapper = config.CreateMapper();

            //yep, this means we are testing the real DbObjectBuilder as well:
            _dbObjectBuilder = new DbObjectBuilder(
                _context,
                _mapper
                );
            _service = new RegattaService(
                _mockSeriesService.Object,
                _context,
                _dbObjectBuilder,
                _mapper
                );


        }

        [Fact]
        public async Task GetAllRegattas_Always_CallsDb()
        {
            var result = await _service.GetAllRegattasAsync(_clubId);

            Assert.Equal(1, result.Count);
        }


        [Fact]
        public async Task GetRegattasDuringSpan_NoneInSpan_Returns0()
        {
            var result = await _service.GetRegattasDuringSpanAsync(DateTime.Today.AddDays(1), DateTime.Today.AddYears(3));

            Assert.Equal(0, result.Count);
        }

        [Fact]
        public async Task SaveNewRegattaAsync_Null_Throws()
        {
            Exception ex = await Assert.ThrowsAsync<ArgumentNullException>(() => _service.SaveNewRegattaAsync(null));

            Assert.NotNull(ex);
        }

        [Fact]
        public async Task SaveNewRegattaAsync_NotNull_SaveToDb()
        {
            var newRegatta = new Regatta
            {
                Name = "New Regatta"
            };
            var result = await _service.SaveNewRegattaAsync(
                _mapper.Map<SailScores.Core.Model.Regatta>(newRegatta));

            Assert.Equal(2, _context.Regattas.Count());
        }

        [Fact]
        public async Task UpdateAsync_Null_Throws()
        {
            Exception ex = await Assert.ThrowsAsync<ArgumentNullException>(() => _service.UpdateAsync(null));

            Assert.NotNull(ex);
        }

        [Fact]
        public async Task AddRaceToRegatta_Null_Throws()
        {
            Exception ex = await Assert.ThrowsAsync<ArgumentNullException>(() => _service.AddRaceToRegattaAsync(null, Guid.NewGuid()));

            Assert.NotNull(ex);
        }

        [Fact]
        public async Task GetRegattaAsync_ReturnsRegatta()
        {
            var result = await _service.GetRegattaAsync(
                _clubInitials,
                _regatta.Season.UrlName,
                _regatta.UrlName);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task AddRaceToRegattaAsync_NotNull_SavesToDb()
        {
            var race = new Race
            {
                Id = Guid.NewGuid(),
                Fleet = _mapper.Map<Fleet>(_context.Fleets.First())
            };
        
            await _service.AddRaceToRegattaAsync(
                _mapper.Map<SailScores.Core.Model.Race>(race),
                _regatta.Id);

            Assert.True(
                _context.Regattas.First().RegattaSeries.Any(rs =>
                    rs.Series.RaceSeries != null
                    && rs.Series.RaceSeries.Any(r => r.RaceId == race.Id)));
        }

        [Fact]
        public async Task AddFleetTorRegattaAsync_AddsToDb()
        {
            var fleet = _context.Fleets.ToList()
                    .Where(f => !_regatta.RegattaFleet.Any(rf => rf.FleetId == f.Id))
                    .First();

            await _service.AddFleetToRegattaAsync(fleet.Id, _regatta.Id);

            Assert.True(
                _context.Regattas.First().RegattaFleet.Any(rf =>
                    rf.FleetId == fleet.Id));
        }

    }
}
