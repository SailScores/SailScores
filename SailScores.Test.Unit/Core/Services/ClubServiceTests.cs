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

namespace SailScores.Test.Unit.Core.Services
{
    public class ClubServiceTests
    {
        private readonly ClubService _service;
        private readonly IMapper _mapper;
        private readonly ISailScoresContext _context;
        private Club _fakeClub;

        public ClubServiceTests()
        {
            var compA = new Competitor
            {
                Name = "Comp A"
            };
            var race1 = new Race
            {
                Date = DateTime.Today
            };

            _fakeClub = new Club
            {
                Id = Guid.NewGuid(),
                Name = "Fake Club",
                Competitors = new List<Competitor>
                {
                    compA
                }
            };

            _context = InMemoryContextBuilder.GetContext();
            _mapper = MapperBuilder.GetSailScoresMapper();

            _context.Clubs.Add(_mapper.Map<Database.Entities.Club>(_fakeClub));
            _context.SaveChanges();

            _mapper = MapperBuilder.GetSailScoresMapper();

            _service = new SailScores.Core.Services.ClubService(
                _context,
                _mapper
                );
        }

        [Fact]
        public async Task DoesClubHaveCompetitors_competitors_ReturnsTrue()
        {
            //arrange
            // act
            var result = await _service.DoesClubHaveCompetitors(_fakeClub.Id);

            // Assert
            Assert.True(result);
        }

    }
}
