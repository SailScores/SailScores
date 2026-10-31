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
using SailScores.Api.Enumerations;
using SailScores.Core.Model;
using Microsoft.EntityFrameworkCore;

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
        public async Task GetRaceSummariesAsync_RaceInRange_ReturnsScoresAndSeries()
        {
            var result = await GetSummariesAsync();

            var race = Assert.Single(result);
            Assert.Equal(1, Assert.Single(race.Scores).Place);
            Assert.Equal("SeriesOne", Assert.Single(race.Series).UrlName);
        }

        [Fact]
        public async Task GetRaceSummariesAsync_RaceOutOfRange_IsExcluded()
        {
            var result = await _service.GetRaceSummariesAsync(
                _clubId,
                DateTime.Today.AddYears(-3),
                DateTime.Today.AddYears(-2));

            Assert.Empty(result);
        }

        [Theory]
        [InlineData(null)]
        [InlineData(RaceState.Raced)]
        [InlineData(RaceState.Preliminary)]
        [InlineData(RaceState.Scheduled)]
        [InlineData(RaceState.Abandoned)]
        public async Task GetRaceSummariesAsync_IncludingAllStates_ListsRace(RaceState? state)
        {
            await SetRaceStateAsync(state);

            var result = await GetSummariesAsync();

            Assert.Single(result);
        }

        [Fact]
        public async Task GetRaceSummariesAsync_ScheduledNotIncluded_ExcludesRace()
        {
            await SetRaceStateAsync(RaceState.Scheduled);

            var result = await GetSummariesAsync(includeScheduled: false);

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetRaceSummariesAsync_AbandonedNotIncluded_ExcludesRace()
        {
            await SetRaceStateAsync(RaceState.Abandoned);

            var result = await GetSummariesAsync(includeAbandoned: false);

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetRaceSummariesAsync_FleetId_ReturnsOnlyThatFleet()
        {
            var fleet = await _context.Fleets.FirstAsync(f => f.ClubId == _clubId);
            await AddRaceAsync(fleet, DateTime.Today);

            var result = await _service.GetRaceSummariesAsync(
                _clubId,
                DateTime.Today.AddDays(-1),
                DateTime.Today.AddDays(2),
                fleetId: fleet.Id);

            Assert.Equal(fleet.ShortName, Assert.Single(result).FleetShortName);
        }

        [Fact]
        public async Task GetRaceFacetsAsync_RacesInMonthsAndFleets_CountsEachGroup()
        {
            var fleet = await _context.Fleets.FirstAsync(f => f.ClubId == _clubId);
            await AddRaceAsync(fleet, new DateTime(2000, 6, 1));
            await AddRaceAsync(fleet, new DateTime(2000, 6, 20));
            await AddRaceAsync(fleet, new DateTime(2000, 7, 4));

            var result = await _service.GetRaceFacetsAsync(
                _clubId,
                new DateTime(2000, 1, 1),
                new DateTime(2000, 12, 31));

            Assert.Equal(2, result.Single(f => f.Month == 6).Count);
            Assert.Equal(1, result.Single(f => f.Month == 7).Count);
            Assert.All(result, f => Assert.Equal(fleet.Id, f.FleetId));
        }

        [Fact]
        public async Task GetRaceFacetsAsync_ScheduledNotIncluded_LeavesRaceOut()
        {
            await SetRaceStateAsync(RaceState.Scheduled);

            var result = await _service.GetRaceFacetsAsync(
                _clubId,
                DateTime.Today.AddDays(-1),
                DateTime.Today.AddDays(2),
                includeScheduled: false);

            Assert.Empty(result);
        }

        private async Task AddRaceAsync(SailScores.Database.Entities.Fleet fleet, DateTime date)
        {
            _context.Races.Add(new SailScores.Database.Entities.Race
            {
                Id = Guid.NewGuid(),
                ClubId = _clubId,
                Date = date,
                Fleet = fleet
            });
            await _context.SaveChangesAsync();
        }

        private Task<IList<RaceSummary>> GetSummariesAsync(
            bool includeScheduled = true,
            bool includeAbandoned = true)
        {
            return _service.GetRaceSummariesAsync(
                _clubId,
                DateTime.Today.AddDays(-1),
                DateTime.Today.AddDays(2),
                includeScheduled,
                includeAbandoned);
        }

        private async Task SetRaceStateAsync(RaceState? state)
        {
            var race = await _context.Races.FirstAsync();
            race.State = state;
            await _context.SaveChangesAsync();
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

        [Fact]
        public async Task GetRaceHandicapSystemsAsync_WithSingleEffectiveSystem_ReturnsOne()
        {
            var club = await _context.Clubs.FirstAsync(TestContext.Current.CancellationToken);
            var fleet = await _context.Fleets.FirstAsync(f => f.ClubId == club.Id, TestContext.Current.CancellationToken);
            var season = await _context.Seasons.FirstAsync(s => s.ClubId == club.Id, TestContext.Current.CancellationToken);

            var baseSystem = new SailScores.Database.Entities.HandicapSystem
            {
                Id = Guid.NewGuid(),
                Name = "Base Portsmouth",
                ClubId = null,
                SystemType = SailScores.Database.Entities.HandicapSystemType.Portsmouth
            };
            _context.HandicapSystems.Add(baseSystem);
            club.DefaultHandicapSystemId = baseSystem.Id;

            var race = new SailScores.Database.Entities.Race
            {
                Id = Guid.NewGuid(),
                ClubId = club.Id,
                Date = DateTime.Today,
                Fleet = fleet
            };

            var series = new SailScores.Database.Entities.Series
            {
                Id = Guid.NewGuid(),
                ClubId = club.Id,
                Name = "Series A",
                Season = season
            };

            _context.Races.Add(race);
            _context.Series.Add(series);
            _context.SeriesRaces.Add(new SailScores.Database.Entities.SeriesRace
            {
                RaceId = race.Id,
                SeriesId = series.Id
            });
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var result = await _service.GetRaceHandicapSystemsAsync(race.Id);

            Assert.Single(result);
            Assert.Equal(baseSystem.Id, result[0].Id);
        }

        [Fact]
        public async Task GetRaceHandicapSystemsAsync_WithMultipleDistinctSystems_ReturnsDistinctSet()
        {
            var club = await _context.Clubs.FirstAsync(TestContext.Current.CancellationToken);
            var fleet = await _context.Fleets.FirstAsync(f => f.ClubId == club.Id, TestContext.Current.CancellationToken);
            var season = await _context.Seasons.FirstAsync(s => s.ClubId == club.Id, TestContext.Current.CancellationToken);

            var systemA = new SailScores.Database.Entities.HandicapSystem
            {
                Id = Guid.NewGuid(),
                Name = "Base ToD",
                ClubId = null,
                SystemType = SailScores.Database.Entities.HandicapSystemType.PhrfToD
            };
            var systemB = new SailScores.Database.Entities.HandicapSystem
            {
                Id = Guid.NewGuid(),
                Name = "Base ToT",
                ClubId = null,
                SystemType = SailScores.Database.Entities.HandicapSystemType.PhrfToT
            };

            _context.HandicapSystems.Add(systemA);
            _context.HandicapSystems.Add(systemB);

            var race = new SailScores.Database.Entities.Race
            {
                Id = Guid.NewGuid(),
                ClubId = club.Id,
                Date = DateTime.Today,
                Fleet = fleet
            };

            var seriesA = new SailScores.Database.Entities.Series
            {
                Id = Guid.NewGuid(),
                ClubId = club.Id,
                Name = "Series A",
                Season = season,
                HandicapSystemId = systemA.Id
            };
            var seriesB = new SailScores.Database.Entities.Series
            {
                Id = Guid.NewGuid(),
                ClubId = club.Id,
                Name = "Series B",
                Season = season,
                HandicapSystemId = systemB.Id
            };

            _context.Races.Add(race);
            _context.Series.Add(seriesA);
            _context.Series.Add(seriesB);
            _context.SeriesRaces.Add(new SailScores.Database.Entities.SeriesRace
            {
                RaceId = race.Id,
                SeriesId = seriesA.Id
            });
            _context.SeriesRaces.Add(new SailScores.Database.Entities.SeriesRace
            {
                RaceId = race.Id,
                SeriesId = seriesB.Id
            });
            await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

            var result = await _service.GetRaceHandicapSystemsAsync(race.Id);

            Assert.Equal(2, result.Count);
            Assert.Contains(result, h => h.Id == systemA.Id);
            Assert.Contains(result, h => h.Id == systemB.Id);
        }

    }
}
