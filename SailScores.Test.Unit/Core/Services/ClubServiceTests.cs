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
    }
}
