using AutoMapper;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using SailScores.Core.Model;
using SailScores.Core.Services;
using SailScores.Database;
using SailScores.Test.Unit.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace SailScores.Test.Unit.Core.Services
{
    public class ClubServiceTests
    {
        private readonly ClubService _service;
        private readonly IMapper _mapper;
        private readonly ISailScoresContext _context;
        private readonly Guid _clubId;
        private readonly string _clubInitials;
        private readonly MemoryCache _realCache;
        private readonly Mock<IScoringService> _mockScoringService;

        public ClubServiceTests()
        {
            _context = Utilities.InMemoryContextBuilder.GetContext();
            _clubId = _context.Clubs.First().Id;
            _clubInitials = _context.Clubs.First().Initials;
            _realCache = new MemoryCache(new MemoryCacheOptions());
            _mapper = MapperBuilder.GetSailScoresMapper();
            
            _mockScoringService = new Mock<IScoringService>();
            _mockScoringService
                .Setup(s => s.CreateDefaultScoringSystemsAsync(It.IsAny<Guid>(), It.IsAny<string>()))
                .ReturnsAsync((Guid clubId, string initials) => new List<ScoringSystem>
                {
                    new ScoringSystem { Id = Guid.NewGuid(), ClubId = clubId, Name = $"{initials} scoring based on App. A Rule 5.3" }
                });

            _service = new SailScores.Core.Services.ClubService(
                _context,
                _realCache,
                _mockScoringService.Object,
                _mapper
                );
        }

        [Fact]
        public async Task GetAllFleets_ReturnsFleets()
        {
            var result = await _service.GetAllFleets(_clubId);
            Assert.NotEmpty(result);
        }

        [Fact]
        public async Task GetActiveFleets_ReturnsFleets()
        {
            var result = await _service.GetActiveFleets(_clubId);
            Assert.NotEmpty(result);
        }


        [Fact]
        public async Task GetMinimalForSelectedBoatsFleets_OnlyReturnsSelectedBoatFleets()
        {
            var result = await _service.GetMinimalForSelectedBoatsFleets(_clubId);
            Assert.DoesNotContain(result, f => f.FleetType != Api.Enumerations.FleetType.SelectedBoats);
            Assert.Contains(result, f => f.FleetType == Api.Enumerations.FleetType.SelectedBoats);

        }

        [Fact]
        public async Task GetAllBoatClasses_ReturnsSomeClasses()
        {
            var result = await _service.GetAllBoatClasses(_clubId);
            Assert.NotEmpty(result);
        }


        [Fact]
        public async Task GetClubs_ReturnsOnlyVisible()
        {
            var result = await _service.GetClubs(false);
            Assert.NotEmpty(result);
            Assert.DoesNotContain(result, c => c.IsHidden);
        }

        [Fact]
        public async Task GetClubId_ReturnsCorrectId()
        {
            var result = await _service.GetClubId(_clubInitials);
            Assert.Equal(_clubId, result);
        }

        [Fact]
        public async Task GetFullClubExceptScores_Initials_ReturnsClub()
        {
            var result = await _service.GetClubForAdmin(_clubInitials);
            Assert.Equal(_clubId, result.Id);
        }

        [Fact]
        public async Task GetMinimalClub_Initials_ReturnsClub()
        {
            var result = await _service.GetMinimalClub(_clubInitials);
            Assert.Equal(_clubId, result.Id);
        }

        [Fact]
        public async Task GetMinimalClub_ById_ReturnsClub()
        {
            var result = await _service.GetMinimalClub(_clubId);
            Assert.Equal(_clubId, result.Id);
        }

        [Fact]
        public async Task DoesClubHaveCompetitors_competitors_ReturnsTrue()
        {
            // arrange
            // act
            var result = await _service.DoesClubHaveCompetitors(_clubId);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ResetClub_RacesAndSeries_ClearsRacesSeriesRegattas()
        {
            // Arrange - verify data exists
            Assert.True(_context.Races.Any(r => r.ClubId == _clubId));
            Assert.True(_context.Series.Any(s => s.ClubId == _clubId));
            Assert.True(_context.Regattas.Any(r => r.ClubId == _clubId));
            Assert.True(_context.Competitors.Any(c => c.ClubId == _clubId));

            // Act
            await _service.ResetClubAsync(_clubId, SailScores.Core.Model.ResetLevel.RacesAndSeries);

            // Assert - races, series, regattas should be cleared
            Assert.False(_context.Races.Any(r => r.ClubId == _clubId));
            Assert.False(_context.Series.Any(s => s.ClubId == _clubId));
            Assert.False(_context.Regattas.Any(r => r.ClubId == _clubId));
            // Competitors should still exist
            Assert.True(_context.Competitors.Any(c => c.ClubId == _clubId));
            // Fleets should still exist
            Assert.True(_context.Fleets.Any(f => f.ClubId == _clubId));
        }

        [Fact]
        public async Task ResetClub_RacesSeriesAndCompetitors_ClearsCompetitors()
        {
            // Arrange - verify data exists
            Assert.True(_context.Competitors.Any(c => c.ClubId == _clubId));
            Assert.True(_context.Fleets.Any(f => f.ClubId == _clubId));

            // Act
            await _service.ResetClubAsync(_clubId, SailScores.Core.Model.ResetLevel.RacesSeriesAndCompetitors);

            // Assert - competitors should be cleared
            Assert.False(_context.Competitors.Any(c => c.ClubId == _clubId));
            // Fleets should still exist
            Assert.True(_context.Fleets.Any(f => f.ClubId == _clubId));
            // Boat classes should still exist
            Assert.True(_context.BoatClasses.Any(bc => bc.ClubId == _clubId));
        }

        [Fact]
        public async Task ResetClub_FullReset_ClearsAllDataExceptClubIdentity()
        {
            // Arrange - store original club info
            var club = _context.Clubs.First(c => c.Id == _clubId);
            var originalName = club.Name;
            var originalInitials = club.Initials;

            // Act
            await _service.ResetClubAsync(_clubId, SailScores.Core.Model.ResetLevel.FullReset);

            // Assert - all data should be cleared
            Assert.False(_context.Races.Any(r => r.ClubId == _clubId));
            Assert.False(_context.Series.Any(s => s.ClubId == _clubId));
            Assert.False(_context.Regattas.Any(r => r.ClubId == _clubId));
            Assert.False(_context.Competitors.Any(c => c.ClubId == _clubId));
            Assert.False(_context.Fleets.Any(f => f.ClubId == _clubId));
            Assert.False(_context.BoatClasses.Any(bc => bc.ClubId == _clubId));
            Assert.False(_context.Seasons.Any(s => s.ClubId == _clubId));
            
            // Club identity should be preserved
            var updatedClub = _context.Clubs.First(c => c.Id == _clubId);
            Assert.Equal(originalName, updatedClub.Name);
            Assert.Equal(originalInitials, updatedClub.Initials);
            
            // Verify that CreateDefaultScoringSystemsAsync was called
            _mockScoringService.Verify(
                s => s.CreateDefaultScoringSystemsAsync(_clubId, originalInitials), 
                Times.Once);
            
            // Default scoring system should be set (from mock return value)
            Assert.NotNull(updatedClub.DefaultScoringSystemId);
        }

        [Fact]
        public async Task ResetClub_InvalidClubId_ThrowsException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.ResetClubAsync(Guid.NewGuid(), SailScores.Core.Model.ResetLevel.RacesAndSeries));
        }

        [Fact]
        public async Task SaveNewClub_WithoutDefaultScoringSystem_CreatesClubWithDefaultFleet()
        {
            // Arrange
            var newClub = new Club
            {
                Name = "New Test Club",
                Initials = "NTC"
            };

            // Act
            var clubId = await _service.SaveNewClub(newClub);

            // Assert
            Assert.NotEqual(Guid.Empty, clubId);
            var savedClub = _context.Clubs.FirstOrDefault(c => c.Id == clubId);
            Assert.NotNull(savedClub);
            Assert.Equal("New Test Club", savedClub.Name);
            Assert.Equal("NTC", savedClub.Initials);
            Assert.Equal(clubId, newClub.Id);

            // Verify default "All Boats in Club" fleet was created
            var defaultFleet = _context.Fleets.FirstOrDefault(f => f.ClubId == clubId);
            Assert.NotNull(defaultFleet);
            Assert.Equal("All Boats in Club", defaultFleet.Name);
            Assert.Equal("All", defaultFleet.ShortName);
            Assert.Equal(Api.Enumerations.FleetType.AllBoatsInClub, defaultFleet.FleetType);
        }

        [Fact]
        public async Task SaveNewClub_WithDefaultScoringSystem_CreatesClubWithScoringSystem()
        {
            // Arrange
            var scoringSystem = new ScoringSystem
            {
                Name = "Test Scoring System",
                DiscardPattern = "1"
            };

            var newClub = new Club
            {
                Name = "Club With Scoring",
                Initials = "CWS",
                DefaultScoringSystem = scoringSystem,
                ScoringSystems = new List<ScoringSystem> { scoringSystem }
            };

            // Act
            var clubId = await _service.SaveNewClub(newClub);

            // Assert
            var savedClub = _context.Clubs.FirstOrDefault(c => c.Id == clubId);
            Assert.NotNull(savedClub);
            Assert.NotNull(savedClub.DefaultScoringSystemId);

            var savedScoringSystem = _context.ScoringSystems.FirstOrDefault(ss => ss.Id == savedClub.DefaultScoringSystemId);
            Assert.NotNull(savedScoringSystem);
            Assert.Equal("Test Scoring System", savedScoringSystem.Name);
            Assert.Equal(clubId, savedScoringSystem.ClubId);
        }

        [Fact]
        public async Task SaveNewClub_WithMultipleScoringSystemsAndDefault_CreatesAllSystems()
        {
            // Arrange
            var defaultSystem = new ScoringSystem
            {
                Id = Guid.NewGuid(),
                Name = "Default Scoring System",
                DiscardPattern = "2"
            };

            var additionalSystem = new ScoringSystem
            {
                Name = "Additional Scoring System",
                DiscardPattern = "1"
            };

            var newClub = new Club
            {
                Name = "Club With Multiple Systems",
                Initials = "CWMS",
                DefaultScoringSystem = defaultSystem,
                ScoringSystems = new List<ScoringSystem> { defaultSystem, additionalSystem }
            };

            // Act
            var clubId = await _service.SaveNewClub(newClub);

            // Assert
            var savedClub = _context.Clubs.FirstOrDefault(c => c.Id == clubId);
            Assert.NotNull(savedClub);
            Assert.NotNull(savedClub.DefaultScoringSystemId);

            var allSystems = _context.ScoringSystems.Where(ss => ss.ClubId == clubId).ToList();
            Assert.Equal(2, allSystems.Count);

            var savedDefaultSystem = allSystems.FirstOrDefault(ss => ss.Id == savedClub.DefaultScoringSystemId);
            Assert.NotNull(savedDefaultSystem);
            Assert.Equal("Default Scoring System", savedDefaultSystem.Name);
        }

        [Fact]
        public async Task SaveNewClub_WithDuplicateInitials_ThrowsInvalidOperationException()
        {
            // Arrange
            var newClub = new Club
            {
                Name = "Duplicate Club",
                Initials = _clubInitials  // Use existing club's initials
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.SaveNewClub(newClub));
            Assert.Contains("initials already exists", exception.Message);
        }

        [Fact]
        public async Task SaveNewClub_SetsNewGuidForClub()
        {
            // Arrange
            var newClub = new Club
            {
                Name = "Test Club",
                Initials = "GUID"
            };
            var originalId = newClub.Id;

            // Act
            var returnedId = await _service.SaveNewClub(newClub);

            // Assert
            Assert.NotEqual(Guid.Empty, returnedId);
            Assert.Equal(returnedId, newClub.Id);
            Assert.NotEqual(originalId, returnedId);
        }

        [Fact]
        public async Task SaveNewClub_WithScoringSystemWithScoreCodes_CreatesAllScoreCodes()
        {
            // Arrange
            var scoringSystem = new ScoringSystem
            {
                Name = "System With Score Codes",
                DiscardPattern = "1",
                ScoreCodes = new List<ScoreCode>
                {
                    new ScoreCode { Name = "DNC", CameToStart = false },
                    new ScoreCode { Name = "DNS", CameToStart = false },
                    new ScoreCode { Name = "FIN", CameToStart = true, Finished = true }
                }
            };

            var newClub = new Club
            {
                Name = "Club With Score Codes",
                Initials = "CWSC",
                DefaultScoringSystem = scoringSystem,
                ScoringSystems = new List<ScoringSystem> { scoringSystem }
            };

            // Act
            var clubId = await _service.SaveNewClub(newClub);

            // Assert
            var savedClub = _context.Clubs.FirstOrDefault(c => c.Id == clubId);
            var savedSystem = _context.ScoringSystems.FirstOrDefault(ss => ss.Id == savedClub.DefaultScoringSystemId);
            Assert.NotNull(savedSystem);

            var scoreCodes = _context.ScoreCodes.Where(sc => sc.ScoringSystemId == savedSystem.Id).ToList();
            Assert.Equal(3, scoreCodes.Count);
            Assert.Contains(scoreCodes, sc => sc.Name == "DNC");
            Assert.Contains(scoreCodes, sc => sc.Name == "DNS");
            Assert.Contains(scoreCodes, sc => sc.Name == "FIN");
        }

        [Fact]
        public async Task SaveNewClub_WithOptionalClubProperties_PersistsAllProperties()
        {
            // Arrange
            var newClub = new Club
            {
                Name = "Full Property Club",
                Initials = "FPC",
                Description = "Test Description",
                Url = "https://example.com",
                IsHidden = false,
                ShowClubInResults = true,
                Locale = "en-US"
            };

            // Act
            var clubId = await _service.SaveNewClub(newClub);

            // Assert
            var savedClub = _context.Clubs.FirstOrDefault(c => c.Id == clubId);
            Assert.NotNull(savedClub);
            Assert.Equal("Full Property Club", savedClub.Name);
            Assert.Equal("FPC", savedClub.Initials);
            Assert.Equal("Test Description", savedClub.Description);
            Assert.Equal("https://example.com", savedClub.Url);
            Assert.False(savedClub.IsHidden);
            Assert.True(savedClub.ShowClubInResults);
            Assert.Equal("en-US", savedClub.Locale);
        }

        [Fact]
        public async Task SaveNewClub_DefaultScoringSystemIdsAreGenerated()
        {
            // Arrange
            var system1 = new ScoringSystem
            {
                Name = "System 1",
                DiscardPattern = "1"
            };
            var system2 = new ScoringSystem
            {
                Name = "System 2",
                DiscardPattern = "2"
            };

            var newClub = new Club
            {
                Name = "Club Multiple Systems",
                Initials = "CMS",
                DefaultScoringSystem = system1,
                ScoringSystems = new List<ScoringSystem> { system1, system2 }
            };

            // Act
            var clubId = await _service.SaveNewClub(newClub);

            // Assert
            var savedClub = _context.Clubs.FirstOrDefault(c => c.Id == clubId);
            var allSystems = _context.ScoringSystems.Where(ss => ss.ClubId == clubId).ToList();

            // All systems should have generated IDs
            Assert.True(allSystems.All(s => s.Id != Guid.Empty));
            Assert.True(allSystems.Count == 2);
        }
    }
}
