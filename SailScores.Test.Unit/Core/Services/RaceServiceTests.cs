using AutoMapper;
using SailScores.Core.Services;
using SailScores.Database;
using SailScores.Test.Unit.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Moq;
using SailScores.Core.JobQueue;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using SailScores.Api.Dtos;

namespace SailScores.Test.Unit.Core.Services
{
    public class RaceServiceTests
    {
        RaceService _service;
        private Guid _clubId;
        private readonly ISailScoresContext _context;
        private readonly Mock<ISeriesService> _mockSeriesService;
        private readonly Mock<IBackgroundTaskQueue> _mockBackgroundTaskQueue;
        private readonly Mock<ILogger<IRaceService>> _mockLogger;
        private readonly Mock<IServiceScopeFactory> _mockServiceScopeFactory;

        private readonly IMapper _mapper;

        public RaceServiceTests()
        {
            _context = InMemoryContextBuilder.GetContext();
            _mockSeriesService = new Mock<ISeriesService>();
            _mockBackgroundTaskQueue = new Mock<IBackgroundTaskQueue>();
            _mockLogger = new Mock<ILogger<IRaceService>>();
            _mockServiceScopeFactory = new Mock<IServiceScopeFactory>();
            _mapper = MapperBuilder.GetSailScoresMapper();
            _service = new RaceService(
                _context,
                _mockSeriesService.Object,
                _mockBackgroundTaskQueue.Object,
                _mockLogger.Object,
                _mockServiceScopeFactory.Object,
                _mapper);

            _clubId = _context.Clubs.First().Id;
        }

        [Fact]
        public async Task GetFullRacesAsync_ReturnsAllRaces()
        {
            // Arrange

            // Act
            var result = await _service.GetFullRacesAsync(
                _clubId,
                "NoSeasonName"
                );

            // fail for now
            Assert.True(result.Count() > 0);
        }

        
        [Fact]
        public async Task Save_Null_Throws()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(async() => await _service.SaveAsync(null));

        }

        [Fact]
        public async Task Save_Existing_DoesNotChangeCount()
        {
            var racesBefore = await _service.GetRacesAsync(
                _clubId
            );

            var dto = _mapper.Map<RaceDto>(racesBefore.First());
            await _service.SaveAsync(dto);

            var racesAfter = await _service.GetRacesAsync(
                _clubId
            );
            Assert.Equal(racesBefore.Count(), racesAfter.Count());

        }

        [Fact]
        public async Task Save_New_IncreasesCount()
        {
            var racesBefore = await _service.GetRacesAsync(
                _clubId
            );

            var dto = new RaceDto
            {
                ClubId = _clubId,
                Date = DateTime.Today,
                Order = 1,
                SeriesIds = new List<Guid>()

            };
            await _service.SaveAsync(dto);

            var racesAfter = await _service.GetRacesAsync(
                _clubId
            );
            Assert.Equal(racesBefore.Count() + 1, racesAfter.Count());

        }

        [Fact]
        public async Task Save_NewNoSeries_IncreasesCount()
        {
            var racesBefore = await _service.GetRacesAsync(
                _clubId
            );

            var dto = new RaceDto
            {
                ClubId = _clubId,
                Date = DateTime.Today,
                Order = 1

            };
            await _service.SaveAsync(dto);

            var racesAfter = await _service.GetRacesAsync(
                _clubId
            );
            Assert.Equal(racesBefore.Count() + 1, racesAfter.Count());

        }

    }
}
