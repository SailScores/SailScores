using AutoMapper;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using SailScores.Core.Services;
using SailScores.Database;
using SailScores.Test.Unit.Utilities;
using System;
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

        public ClubServiceTests()
        {
            _context = Utilities.InMemoryContextBuilder.GetContext();
            _clubId = _context.Clubs.First().Id;
            _clubInitials = _context.Clubs.First().Initials;
            _realCache = new MemoryCache(new MemoryCacheOptions());
            _mapper = MapperBuilder.GetSailScoresMapper();

            _service = new SailScores.Core.Services.ClubService(
                _context,
                _realCache,
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
            Assert.Empty(result.Where(f => f.FleetType != Api.Enumerations.FleetType.SelectedBoats));
            Assert.NotEmpty(result.Where(f => f.FleetType == Api.Enumerations.FleetType.SelectedBoats));

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
            Assert.Empty(result.Where(c => c.IsHidden));
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
    }
}
