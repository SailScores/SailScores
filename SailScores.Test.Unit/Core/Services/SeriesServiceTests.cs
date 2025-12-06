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
            _mapper
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
}
