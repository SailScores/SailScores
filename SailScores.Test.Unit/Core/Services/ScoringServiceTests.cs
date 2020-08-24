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
    public class ScoringServiceTests
    {
        ScoringService _service;

        private readonly ISailScoresContext _context;
        private readonly IMapper _mapper;

        public ScoringServiceTests()
        {
            _context = InMemoryContextBuilder.GetContext();
            _mapper = MapperBuilder.GetSailScoresMapper();
            _service = new ScoringService(
                _context,
                _mapper);
        }

        [Fact]
        public async Task GetScoreCodesAsync_ReturnsOneDnc()
        {
            var scoringSystem = await _context.ScoringSystems.FirstAsync(ss => ss.ClubId != null);
            _context.ScoreCodes.Add(new Database.Entities.ScoreCode
            {
                ScoringSystemId = scoringSystem.Id,
                Name = "DNC",
                CameToStart = false
            });
            await _context.SaveChangesAsync();

            // Act
            var results = await _service.GetScoreCodesAsync(scoringSystem.ClubId.Value);

            // Assert
            Assert.Equal(1, results.Count(sc => sc.Name == "DNC"));

        }

        [Fact]
        public async Task GetScoringSystemsAsync_ReturnsASystemWithDnc()
        {
            var club = await _context.Clubs.FirstAsync();

            // Act
            var results = await _service.GetScoringSystemsAsync(club.Id, false);

            // Assert
            Assert.Contains(results, ss =>
                ss.ScoreCodes.Any(sc => sc.Name == "DNC") && ss.ClubId == club.Id);

        }

        [Fact]
        public async Task GetSiteDefaultSystemAsync_ReturnsOneSystem()
        {
            var club = await _context.Clubs.FirstAsync();

            // Act
            var result = await _service.GetSiteDefaultSystemAsync();

            // Assert
            Assert.NotNull(result);

        }

        [Fact]
        public async Task GetScoringSystemAsync_ReturnsNotNull()
        {
            var scoringSystem = await _context.ScoringSystems.FirstAsync(ss => ss.ClubId != null);
            _context.ScoreCodes.Add(new Database.Entities.ScoreCode
            {
                ScoringSystemId = scoringSystem.Id,
                Name = "DNC",
                CameToStart = false
            });
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetScoringSystemAsync(scoringSystem.Id);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task SaveScoringSystemAsync_NoId_SavesToDb()
        {
            var scoringSystem = new ScoringSystem
            {
                Name = "Hah"
            };


            // Act
            await _service.SaveScoringSystemAsync(scoringSystem);

            // Assert
            Assert.Contains(_context.ScoringSystems, (ss) =>  ss.Name == "Hah");
        }

        [Fact]
        public async Task DeleteScoringSystemAsync_NewSystem_RemovesFromDb()
        {
            var scoringSystem = new ScoringSystem
            {
                Id = Guid.NewGuid(),
                Name = "Hah"
            };


            // Act
            await _service.SaveScoringSystemAsync(scoringSystem);

            // Assert
            Assert.Contains(_context.ScoringSystems, (ss) => ss.Name == "Hah");


            await _service.DeleteScoringSystemAsync(scoringSystem.Id);

            // Assert
            Assert.DoesNotContain(_context.ScoringSystems, (ss) => ss.Name == "Hah");
        }


    }
}
