using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using SailScores.Core.Mapping;
using SailScores.Core.Model;
using SailScores.Core.Scoring;
using SailScores.Core.Services;
using SailScores.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace SailScores.Test.Unit.Core.Services;

public class SeriesServiceTests
{
    private readonly Series _fakeSeries;
    private readonly DbObjectBuilder _dbObjectBuilder;
    private readonly SeriesService _service;
    private readonly Mock<IScoringCalculator> _mockCalculator;
    private readonly Mock<IScoringCalculatorFactory> _mockScoringCalculatorFactory;
    private readonly IMapper _mapper;
    private readonly ISailScoresContext _context;
    private readonly string _clubInitials;
    private readonly string _seriesUrlName;
    private readonly Mock<IScoringService> _mockScoringService;
    private readonly Mock<IForwarderService> _mockForwarderService;
    private readonly Mock<IConversionService> _mockConversionService;
    private readonly Mock<IIndexNowService> _mockIndexNowService;
    private readonly IMemoryCache _realCache;

    public SeriesServiceTests()
    {
        _mockCalculator = new Mock<IScoringCalculator>();
        _mockCalculator.Setup(c => c.CalculateResults(It.IsAny<Series>())).Returns(new SeriesResults
        {
            Results = new Dictionary<Competitor, SeriesCompetitorResults>()
        });
        _mockScoringCalculatorFactory = new Mock<IScoringCalculatorFactory>();
        _mockScoringCalculatorFactory.Setup(f => f.CreateScoringCalculatorAsync(It.IsAny<SailScores.Core.Model.ScoringSystem>()))
            .ReturnsAsync(_mockCalculator.Object);
        _mockScoringService = new Mock<IScoringService>();
        _mockForwarderService = new Mock<IForwarderService>();
        _mockConversionService = new Mock<IConversionService>();
        _mockIndexNowService = new Mock<IIndexNowService>();
        _realCache = new MemoryCache(new MemoryCacheOptions());

        _context = Utilities.InMemoryContextBuilder.GetContext();
        _clubInitials = _context.Clubs.First().Initials;

        var _clubId = _context.Clubs.First().Id;


        var config = new MapperConfiguration(opts =>
        {
            opts.AddProfile(new DbToModelMappingProfile());
        });

        _mapper = config.CreateMapper();

        _fakeSeries = _mapper.Map<Series>(_context.Series.Where(s => s.ClubId == _clubId).First());

        _seriesUrlName = _fakeSeries.UrlName;

        //yep, this means we are testing the real DbObjectBuilder as well:
        _dbObjectBuilder = new DbObjectBuilder(
            _context,
            _mapper
            );
        _service = new SailScores.Core.Services.SeriesService(
            _mockScoringCalculatorFactory.Object,
            _mockScoringService.Object,
            _mockForwarderService.Object,
            _mockConversionService.Object,
            _dbObjectBuilder,
            _context,
            _realCache,
            _mapper,
            _mockIndexNowService.Object
            );

    }

    [Fact]
    public async Task SaveSeries_Unlocked_CalculatesScores()
    {
        var seriesToUpdate = _mapper.Map<Series>(_fakeSeries);
        seriesToUpdate.Competitors = new List<Competitor> { };
        seriesToUpdate.ResultsLocked = false;
        await _service.Update(seriesToUpdate);

        _mockScoringCalculatorFactory.Verify(cf =>
            cf.CreateScoringCalculatorAsync(It.IsAny<ScoringSystem>()),
            Times.Once);
    }

    [Fact]
    public async Task SaveSeries_Locked_DoesNotCalculateScores()
    {
        var seriesToUpdate = _mapper.Map<Series>(_fakeSeries);
        seriesToUpdate.ResultsLocked = true;
        await _service.Update(seriesToUpdate);

        _mockScoringCalculatorFactory.Verify(cf =>
            cf.CreateScoringCalculatorAsync(It.IsAny<ScoringSystem>()),
            Times.Never);
    }

    [Fact]
    public async Task GetSeriesDetailAsync_ReturnsFromDb()
    {
        // Arrange
        var season = await _context.Seasons.FirstAsync();

        // Act
        var result = await _service.GetSeriesDetailsAsync(
            _clubInitials,
            season.UrlName,
            _seriesUrlName);


        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task UpdateSeriesResultsAsync_SavesHistoricalResults()
    {
        var historicalResultCount = _context.HistoricalResults.Count();

        await _service.UpdateSeriesResults(_fakeSeries.Id, String.Empty);

        Assert.True(_context.HistoricalResults.Count() > historicalResultCount);
    }

    [Fact]
    public async Task GetAllSeriesAsync_WithDateRestriction_FiltersCorrectly()
    {
        // Arrange
        var clubId = _context.Clubs.First().Id;
        var season = _context.Seasons.First();
        var testDate = season.Start.AddDays(5);

        // Create a date-restricted series
        var restrictedSeries = new Database.Entities.Series
        {
            Id = Guid.NewGuid(),
            ClubId = clubId,
            Name = "Date Restricted Series",
            UrlName = "date-restricted-series",
            Season = season,
            Type = Database.Entities.SeriesType.Standard,
            DateRestricted = true,
            EnforcedStartDate = DateOnly.FromDateTime(testDate.AddDays(-2)),
            EnforcedEndDate = DateOnly.FromDateTime(testDate.AddDays(2))
        };

        // Create a non-restricted series
        var nonRestrictedSeries = new Database.Entities.Series
        {
            Id = Guid.NewGuid(),
            ClubId = clubId,
            Name = "Non Restricted Series",
            UrlName = "non-restricted-series",
            Season = season,
            Type = Database.Entities.SeriesType.Standard,
            DateRestricted = false
        };

        _context.Series.Add(restrictedSeries);
        _context.Series.Add(nonRestrictedSeries);
        await _context.SaveChangesAsync();

        // Act - query with date within restricted series range
        var seriesWithinRange = await _service.GetAllSeriesAsync(clubId, testDate, false, false);

        // Assert - both series should be returned (restricted is within range, non-restricted has no restriction)
        Assert.Contains(seriesWithinRange, s => s.Id == restrictedSeries.Id);
        Assert.Contains(seriesWithinRange, s => s.Id == nonRestrictedSeries.Id);

        // Act - query with date outside restricted series range
        var dateOutsideRange = testDate.AddDays(10);
        var seriesOutsideRange = await _service.GetAllSeriesAsync(clubId, dateOutsideRange, false, false);

        // Assert - only non-restricted series should be returned
        Assert.DoesNotContain(seriesOutsideRange, s => s.Id == restrictedSeries.Id);
        Assert.Contains(seriesOutsideRange, s => s.Id == nonRestrictedSeries.Id);
    }

    [Fact]
    public async Task GetAllSeriesAsync_WithNullDate_ReturnsAllSeries()
    {
        // Arrange
        var clubId = _context.Clubs.First().Id;
        var season = _context.Seasons.First();

        // Create a date-restricted series
        var restrictedSeries = new Database.Entities.Series
        {
            Id = Guid.NewGuid(),
            ClubId = clubId,
            Name = "Restricted for Null Date Test",
            UrlName = "restricted-null-test",
            Season = season,
            Type = Database.Entities.SeriesType.Standard,
            DateRestricted = true,
            EnforcedStartDate = DateOnly.FromDateTime(season.Start),
            EnforcedEndDate = DateOnly.FromDateTime(season.End)
        };

        _context.Series.Add(restrictedSeries);
        await _context.SaveChangesAsync();

        // Act - query with null date
        var allSeries = await _service.GetAllSeriesAsync(clubId, null, false, false);

        // Assert - restricted series should be included when date is null
        Assert.Contains(allSeries, s => s.Id == restrictedSeries.Id);
    }

    [Fact]
    public async Task SaveNewSeries_SummaryWithChildren_PersistsChildLinks()
    {
        // Arrange
        var clubId = _context.Clubs.First().Id;
        var season = _context.Seasons.First();

        var child1 = new Database.Entities.Series
        {
            Id = Guid.NewGuid(),
            ClubId = clubId,
            Name = "Child Series 1",
            UrlName = "child-series-1",
            Season = season,
            Type = Database.Entities.SeriesType.Standard,
            RaceSeries = new List<Database.Entities.SeriesRace>(),
            ChildLinks = new List<Database.Entities.SeriesToSeriesLink>(),
            ParentLinks = new List<Database.Entities.SeriesToSeriesLink>()
        };
        var child2 = new Database.Entities.Series
        {
            Id = Guid.NewGuid(),
            ClubId = clubId,
            Name = "Child Series 2",
            UrlName = "child-series-2",
            Season = season,
            Type = Database.Entities.SeriesType.Standard,
            RaceSeries = new List<Database.Entities.SeriesRace>(),
            ChildLinks = new List<Database.Entities.SeriesToSeriesLink>(),
            ParentLinks = new List<Database.Entities.SeriesToSeriesLink>()
        };

        _context.Series.Add(child1);
        _context.Series.Add(child2);
        await _context.SaveChangesAsync();

        var summary = new Series
        {
            ClubId = clubId,
            Name = "My Summary",
            Season = _mapper.Map<Season>(season),
            Type = SeriesType.Summary,
            ChildrenSeriesIds = new List<Guid> { child1.Id, child2.Id },
            UpdatedBy = "test"
        };

        // Act
        var summaryId = await _service.SaveNewSeries(summary);

        // Assert
        var dbSummary = await _context.Series
            .Include(s => s.ChildLinks)
            .SingleAsync(s => s.Id == summaryId);

        Assert.NotNull(dbSummary.ChildLinks);
        Assert.Contains(dbSummary.ChildLinks, l => l.ChildSeriesId == child1.Id);
        Assert.Contains(dbSummary.ChildLinks, l => l.ChildSeriesId == child2.Id);
    }
}
