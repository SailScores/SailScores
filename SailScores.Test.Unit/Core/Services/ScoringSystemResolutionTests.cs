using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using SailScores.Core.Services;
using SailScores.Database;
using SailScores.Test.Unit.Utilities;
using Xunit;
using Db = SailScores.Database.Entities;

namespace SailScores.Test.Unit.Core.Services;

public class ScoringSystemResolutionTests
{
    private readonly ISailScoresContext _context;
    private readonly Mock<IMemoryCache> _cache;
    private readonly IMapper _mapper;
    private readonly ScoringService _service;

    private readonly Guid _clubId;
    private readonly Guid _clubScoringSystemId;
    private readonly Guid _seasonScoringSystemId;
    private readonly Guid _seriesScoringSystemId;
    private readonly Guid _seasonId;

    public ScoringSystemResolutionTests()
    {
        var options = new DbContextOptionsBuilder<SailScoresContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new SailScoresContext(options);
        _cache = new Mock<IMemoryCache>();
        var cacheEntry = Mock.Of<ICacheEntry>();
        _cache.Setup(x => x.CreateEntry(It.IsAny<object>())).Returns(cacheEntry);

        _mapper = MapperBuilder.GetSailScoresMapper();

        // Setup test data
        _clubId = Guid.NewGuid();
        _clubScoringSystemId = Guid.NewGuid();
        _seasonScoringSystemId = Guid.NewGuid();
        _seriesScoringSystemId = Guid.NewGuid();
        _seasonId = Guid.NewGuid();

        SetupTestData();

        _service = new ScoringService(
            _context,
            _cache.Object,
            _mapper);
    }

    private void SetupTestData()
    {
        // Create scoring systems
        var clubScoringSystem = new Db.ScoringSystem
        {
            Id = _clubScoringSystemId,
            Name = "Club Default Scoring System",
            ClubId = _clubId,
            DiscardPattern = "0",
            ScoreCodes = new List<Db.ScoreCode>
            {
                new Db.ScoreCode { Name = "DNC", CameToStart = false }
            }
        };
        _context.ScoringSystems.Add(clubScoringSystem);

        var seasonScoringSystem = new Db.ScoringSystem
        {
            Id = _seasonScoringSystemId,
            Name = "Season Default Scoring System",
            ClubId = _clubId,
            DiscardPattern = "0",
            ScoreCodes = new List<Db.ScoreCode>
            {
                new Db.ScoreCode { Name = "DNC", CameToStart = false }
            }
        };
        _context.ScoringSystems.Add(seasonScoringSystem);

        var seriesScoringSystem = new Db.ScoringSystem
        {
            Id = _seriesScoringSystemId,
            Name = "Series Specific Scoring System",
            ClubId = _clubId,
            DiscardPattern = "0",
            ScoreCodes = new List<Db.ScoreCode>
            {
                new Db.ScoreCode { Name = "DNC", CameToStart = false }
            }
        };
        _context.ScoringSystems.Add(seriesScoringSystem);

        // Create club with default scoring system
        var club = new Db.Club
        {
            Id = _clubId,
            Name = "Test Club",
            Initials = "TEST",
            DefaultScoringSystemId = _clubScoringSystemId
        };
        _context.Clubs.Add(club);

        _context.SaveChanges();
    }

    [Fact]
    public async Task GetScoringSystemFromCacheAsync_SeriesHasScoringSystem_UsesSeriesSystem()
    {
        // Arrange
        var season = new Db.Season
        {
            Id = _seasonId,
            Name = "Test Season",
            UrlName = "test-season",
            ClubId = _clubId,
            Start = new DateTime(2024, 1, 1),
            End = new DateTime(2024, 12, 31),
            DefaultScoringSystemId = _seasonScoringSystemId
        };
        _context.Seasons.Add(season);

        var series = new Db.Series
        {
            Id = Guid.NewGuid(),
            Name = "Test Series",
            UrlName = "test-series",
            ClubId = _clubId,
            Season = season,
            ScoringSystemId = _seriesScoringSystemId
        };
        _context.Series.Add(series);
        await _context.SaveChangesAsync();

        var modelSeries = new SailScores.Core.Model.Series
        {
            Id = series.Id,
            ClubId = _clubId,
            ScoringSystemId = _seriesScoringSystemId
        };

        // Act
        var result = await _service.GetScoringSystemFromCacheAsync(modelSeries);

        // Assert
        Assert.Equal(_seriesScoringSystemId, result.Id);
        Assert.Equal("Series Specific Scoring System", result.Name);
    }

    [Fact]
    public async Task GetScoringSystemFromCacheAsync_SeriesNoScoringSystem_SeasonHasDefault_UsesSeasonDefault()
    {
        // Arrange
        var season = new Db.Season
        {
            Id = Guid.NewGuid(),
            Name = "Season With Default",
            UrlName = "season-with-default",
            ClubId = _clubId,
            Start = new DateTime(2024, 1, 1),
            End = new DateTime(2024, 12, 31),
            DefaultScoringSystemId = _seasonScoringSystemId
        };
        _context.Seasons.Add(season);

        var series = new Db.Series
        {
            Id = Guid.NewGuid(),
            Name = "Series No System",
            UrlName = "series-no-system",
            ClubId = _clubId,
            Season = season,
            ScoringSystemId = null // No scoring system set on series
        };
        _context.Series.Add(series);
        await _context.SaveChangesAsync();

        var modelSeries = new SailScores.Core.Model.Series
        {
            Id = series.Id,
            ClubId = _clubId,
            ScoringSystemId = null // No scoring system set on series
        };

        // Act
        var result = await _service.GetScoringSystemFromCacheAsync(modelSeries);

        // Assert
        Assert.Equal(_seasonScoringSystemId, result.Id);
        Assert.Equal("Season Default Scoring System", result.Name);
    }

    [Fact]
    public async Task GetScoringSystemFromCacheAsync_SeriesNoScoringSystem_SeasonNoDefault_UsesClubDefault()
    {
        // Arrange
        var season = new Db.Season
        {
            Id = Guid.NewGuid(),
            Name = "Season No Default",
            UrlName = "season-no-default",
            ClubId = _clubId,
            Start = new DateTime(2024, 1, 1),
            End = new DateTime(2024, 12, 31),
            DefaultScoringSystemId = null // No season default
        };
        _context.Seasons.Add(season);

        var series = new Db.Series
        {
            Id = Guid.NewGuid(),
            Name = "Series No System",
            UrlName = "series-no-system-2",
            ClubId = _clubId,
            Season = season,
            ScoringSystemId = null // No scoring system set on series
        };
        _context.Series.Add(series);
        await _context.SaveChangesAsync();

        var modelSeries = new SailScores.Core.Model.Series
        {
            Id = series.Id,
            ClubId = _clubId,
            ScoringSystemId = null // No scoring system set on series
        };

        // Act
        var result = await _service.GetScoringSystemFromCacheAsync(modelSeries);

        // Assert
        Assert.Equal(_clubScoringSystemId, result.Id);
        Assert.Equal("Club Default Scoring System", result.Name);
    }

    [Fact]
    public async Task IsScoringSystemInUseAsync_UsedBySeasonDefault_ReturnsTrue()
    {
        // Arrange
        var season = new Db.Season
        {
            Id = Guid.NewGuid(),
            Name = "Season Using System",
            UrlName = "season-using-system",
            ClubId = _clubId,
            Start = new DateTime(2024, 1, 1),
            End = new DateTime(2024, 12, 31),
            DefaultScoringSystemId = _seasonScoringSystemId
        };
        _context.Seasons.Add(season);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.IsScoringSystemInUseAsync(_seasonScoringSystemId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task IsScoringSystemInUseAsync_NotUsedAnywhere_ReturnsFalse()
    {
        // Arrange
        var unusedScoringSystemId = Guid.NewGuid();
        var unusedScoringSystem = new Db.ScoringSystem
        {
            Id = unusedScoringSystemId,
            Name = "Unused Scoring System",
            ClubId = _clubId,
            DiscardPattern = "0"
        };
        _context.ScoringSystems.Add(unusedScoringSystem);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.IsScoringSystemInUseAsync(unusedScoringSystemId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetScoringSystemFromCacheAsync_SeriesHasEmptyGuid_UsesSeasonDefault()
    {
        // Arrange - This tests when series scoring system is explicitly set to empty Guid
        var season = new Db.Season
        {
            Id = Guid.NewGuid(),
            Name = "Season Test Empty Guid",
            UrlName = "season-test-empty-guid",
            ClubId = _clubId,
            Start = new DateTime(2024, 1, 1),
            End = new DateTime(2024, 12, 31),
            DefaultScoringSystemId = _seasonScoringSystemId
        };
        _context.Seasons.Add(season);

        var series = new Db.Series
        {
            Id = Guid.NewGuid(),
            Name = "Series Empty Guid",
            UrlName = "series-empty-guid",
            ClubId = _clubId,
            Season = season,
            ScoringSystemId = null // Empty/null guid means use default
        };
        _context.Series.Add(series);
        await _context.SaveChangesAsync();

        var modelSeries = new SailScores.Core.Model.Series
        {
            Id = series.Id,
            ClubId = _clubId,
            ScoringSystemId = null
        };

        // Act
        var result = await _service.GetScoringSystemFromCacheAsync(modelSeries);

        // Assert
        Assert.Equal(_seasonScoringSystemId, result.Id);
    }
}
