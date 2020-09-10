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
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace SailScores.Test.Unit.Core.Services
{
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
        private readonly Mock<IConversionService> _mockConversionService;

        public SeriesServiceTests()
        {
            _mockCalculator = new Mock<IScoringCalculator>();
            _mockCalculator.Setup(c => c.CalculateResults(It.IsAny<Series>())).Returns(new SeriesResults());
            _mockScoringCalculatorFactory = new Mock<IScoringCalculatorFactory>();
            _mockScoringCalculatorFactory.Setup(f => f.CreateScoringCalculatorAsync(It.IsAny<SailScores.Core.Model.ScoringSystem>()))
                .ReturnsAsync(_mockCalculator.Object);
            _mockScoringService = new Mock<IScoringService>();
            _mockConversionService = new Mock<IConversionService>();

            _context = Utilities.InMemoryContextBuilder.GetContext();
            _clubInitials = _context.Clubs.First().Initials;


            var config = new MapperConfiguration(opts =>
            {
                opts.AddProfile(new DbToModelMappingProfile());
            });

            _mapper = config.CreateMapper();

            _fakeSeries = _mapper.Map<Series>(_context.Series.First());
            _seriesUrlName = _fakeSeries.UrlName;

            //yep, this means we are testing the real DbObjectBuilder as well:
            _dbObjectBuilder = new DbObjectBuilder(
                _context,
                _mapper
                );
            _service = new SailScores.Core.Services.SeriesService(
                _mockScoringCalculatorFactory.Object,
                _mockScoringService.Object,
                _mockConversionService.Object,
                _dbObjectBuilder,
                _context,
                _mapper
                );


        }

        [Fact]
        public async Task SaveSeries_Unlocked_CalculatesScores()
        {
            _fakeSeries.Competitors = new List<Competitor> { };
            await _service.Update(_fakeSeries);

            _mockScoringCalculatorFactory.Verify(cf =>
                cf.CreateScoringCalculatorAsync(It.IsAny<ScoringSystem>()),
                Times.Once);
        }

        [Fact]
        public async Task SaveSeries_Locked_DoesNotCalculateScores()
        {
            _fakeSeries.ResultsLocked = true;
            await _service.Update(_fakeSeries);

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
            var result = _service.GetSeriesDetailsAsync(
                _clubInitials,
                season.Name,
                _seriesUrlName);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task UpdateSeriesResultsAsync_SavesHistoricalResults()
        {
            var historicalRsultCount = _context.HistoricalResults.Count();

            await _service.UpdateSeriesResults(_fakeSeries.Id);

            Assert.True(_context.HistoricalResults.Count() > historicalRsultCount);
        }
    }
}
