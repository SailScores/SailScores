using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Moq;
using SailScores.Core.Mapping;
using SailScores.Core.Model;
using SailScores.Core.Scoring;
using SailScores.Core.Services;
using SailScores.Database;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SailScores.Api.Dtos;
using Xunit;
using SailScores.Test.Unit.Utilities;
using System.Linq;

namespace SailScores.Test.Unit.Core.Services
{
    public class ClubServiceTests
    {
        private readonly ClubService _service;
        private readonly IMapper _mapper;
        private readonly ISailScoresContext _context;
        private Guid _clubId;

        public ClubServiceTests()
        {
            _context = Utilities.InMemoryContextBuilder.GetContext();
            _clubId = _context.Clubs.First().Id;
            _mapper = MapperBuilder.GetSailScoresMapper();

            _service = new SailScores.Core.Services.ClubService(
                _context,
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
            var result = await _service.GetClubId("TEST");
            Assert.Equal(_clubId, result);
        }

        [Fact]
        public async Task GetFullClub_Initials_ReturnsClub()
        {
            var result = await _service.GetFullClub("TEST");
            Assert.Equal(_clubId, result.Id);
        }

        [Fact]
        public async Task DoesClubHaveCompetitors_competitors_ReturnsTrue()
        {
            //arrange
            // act
            var result = await _service.DoesClubHaveCompetitors(_clubId);

            // Assert
            Assert.True(result);
        }


    }
}
