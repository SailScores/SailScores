using AutoMapper;
using SailScores.Core.Services;
using SailScores.Database;
using SailScores.Test.Unit.Utilities;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SailScores.Core.Model;
using Xunit;
using Microsoft.Extensions.Caching.Memory;
using Moq;

namespace SailScores.Test.Unit.Core.Services
{
    public class ScoringServiceTests
    {
        ScoringService _service;

        private readonly ISailScoresContext _context;
        private readonly Mock<IMemoryCache> _cache;
        private readonly IMapper _mapper;

        public ScoringServiceTests()
        {
            _context = InMemoryContextBuilder.GetContext();
            _cache = new Mock<IMemoryCache>();
            var cacheEntry = Mock.Of<ICacheEntry>();

            _cache.Setup(x => x.CreateEntry(It.IsAny<object>())).Returns(cacheEntry);

            _mapper = MapperBuilder.GetSailScoresMapper();
            _service = new ScoringService(
                _context,
                _cache.Object,
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
            Assert.Contains(_context.ScoringSystems, (ss) => ss.Name == "Hah");
        }

        [Fact]
        public async Task DeleteScoringSystemAsync_NewSystem_RemovesFromDb()
        {
            var scoringSystem = new ScoringSystem
            {
                Id = Guid.NewGuid(),
                Name = "Hah"
            };

            await _service.SaveScoringSystemAsync(scoringSystem);
            Assert.Contains(_context.ScoringSystems, (ss) => ss.Name == "Hah");

            // Act again
            await _service.DeleteScoringSystemAsync(scoringSystem.Id);

            // Assert
            Assert.DoesNotContain(_context.ScoringSystems, (ss) => ss.Name == "Hah");
        }




        [Fact]
        public async Task GetScoreCodeAsync_ReturnsFromDb()
        {
            // Arrange
            var id = (await _context.ScoreCodes.FirstAsync()).Id;
            // Act
            var result = _service.GetScoreCodeAsync(id);
            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task SaveScoreCodeAsync_NewCode_SavesToDb()
        {
            // Arrange
            var system = await _context.ScoringSystems.FirstAsync();
            var code = new ScoreCode
            {
                Name = "FIFO",
                ScoringSystemId = system.Id
            };
            // Act
            await _service.SaveScoreCodeAsync(code);

            // Assert
            Assert.Contains(_context.ScoreCodes, sc => sc.Name == "FIFO");
        }

        [Fact]
        public async Task SaveScoreCodeAsync_ExistingCode_SavesToDb()
        {
            // Arrange
            var existingCode = await _context.ScoreCodes.FirstAsync();
            existingCode.Name = "HMM";

            var code = _mapper.Map<ScoreCode>(existingCode);
            
            // Act
            await _service.SaveScoreCodeAsync(code);

            // Assert
            Assert.Contains(_context.ScoreCodes, sc => sc.Name == "HMM");
        }

        [Fact]
        public async Task DeleteScoreCodeAsync_RemovesFromDb()
        {
            // Arrange
            var existingCode = await _context.ScoreCodes.FirstAsync();
            Assert.Contains(_context.ScoreCodes, sc => sc.Id == existingCode.Id);

            // Act
            await _service.DeleteScoreCodeAsync(existingCode.Id);

            // Assert
            Assert.DoesNotContain(_context.ScoreCodes, sc => sc.Id == existingCode.Id);
        }

        [Fact]
        public async Task IsScoringSystemInUseAsync_NotInUse_ReturnsFalse()
        {

           // Act
           var result = await _service.IsScoringSystemInUseAsync(Guid.NewGuid());

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task IsScoringSystemInUseAsync_InUse_ReturnsTrue()
        {
            // Arrange
            var existingClub = await _context.Clubs.FirstAsync();

            // Act
            var result = await _service.IsScoringSystemInUseAsync(existingClub.DefaultScoringSystemId.Value);

            // Assert
            Assert.True(result);
        }

    }
}
