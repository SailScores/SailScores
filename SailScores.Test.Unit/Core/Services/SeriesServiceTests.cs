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
using Xunit;

namespace SailScores.Test.Unit.Core.Services
{
    public class SeriesServiceTests
    {
        private readonly Series _fakeSeries;
        private readonly DbObjectBuilder _dbObjectBuilder;
        private readonly SeriesService _service;

        private readonly Mock<IScoringCalculatorFactory> _mockScoringCalculatorFactory;
        private readonly IMapper _mapper;
        private readonly ISailScoresContext _context;
        private readonly Mock<IScoringService> _mockScoringService;
        private readonly Mock<IConversionService> _mockConversionService;

        public SeriesServiceTests()
        {
            _mockScoringCalculatorFactory = new Mock<IScoringCalculatorFactory>();
            _mockScoringService = new Mock<IScoringService>();
            _mockConversionService = new Mock<IConversionService>();

            var options = new DbContextOptionsBuilder<SailScoresContext>()
                .UseInMemoryDatabase(databaseName: "Series_Test_database")
                .Options;

            _context = new SailScoresContext(options);

            var config = new MapperConfiguration(opts =>
            {
                opts.AddProfile(new DbToModelMappingProfile());
            });

            _mapper = config.CreateMapper();

            var compA = new Competitor
            {
                Name = "Comp A"
            };
            var race1 = new Race
            {
                Date = DateTime.Today
            };

            _fakeSeries = new Series
            {
                Id = Guid.NewGuid(),
                Name = "Fake Series",
                Competitors = new List<Competitor> {
                    compA
                },
                Races = new List<Race>
                {
                    race1
                },
                Season = new Season
                {
                    Id = Guid.NewGuid(),
                    Name = "New Season",
                    Start = new DateTime(2019, 1, 1),
                    End = new DateTime(2019, 12, 31)
                }
            };

            _context.Series.Add(_mapper.Map<Database.Entities.Series>(_fakeSeries));
            _context.SaveChanges();

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
    }
}
