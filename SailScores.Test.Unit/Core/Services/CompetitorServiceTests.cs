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

namespace SailScores.Test.Unit.Core.Services
{
    public class CompetitorServiceTests
    {
        private readonly Series _fakeSeries;
        private readonly CompetitorService _service;
        private readonly Mock<IScoringCalculator> _mockCalculator;
        private readonly Mock<IScoringCalculatorFactory> _mockScoringCalculatorFactory;
        private readonly IMapper _mapper;
        private readonly ISailScoresContext _context;

        public CompetitorServiceTests()
        {
            _mockCalculator = new Mock<IScoringCalculator>();
            _mockCalculator.Setup(c => c.CalculateResults(It.IsAny<Series>())).Returns(new SeriesResults());
            _mockScoringCalculatorFactory = new Mock<IScoringCalculatorFactory>();
            _mockScoringCalculatorFactory.Setup(f => f.CreateScoringCalculatorAsync(It.IsAny<SailScores.Core.Model.ScoringSystem>()))
                .ReturnsAsync(_mockCalculator.Object);

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
                },
                Results = new SeriesResults()
            };

            _context.Series.Add(_mapper.Map<Database.Entities.Series>(_fakeSeries));
            _context.SaveChanges();

            //yep, this means we are testing the real DbObjectBuilder as well:
            _service = new SailScores.Core.Services.CompetitorService(
                _context,
                _mapper
                );
        }

        [Fact]
        public async Task SaveAsync_NullCompetitor_throws()
        {
            // arrange

            // act
            Exception ex = await Assert.ThrowsAsync<ArgumentNullException>(() => _service.SaveAsync((Competitor)null));

            Assert.NotNull(ex);
            // assert
        }

        [Fact]
        public async Task SaveAsync_NullCompetitorDto_throws()
        {
            // arrange

            // act
            Exception ex = await Assert.ThrowsAsync<ArgumentNullException>(() => _service.SaveAsync((CompetitorDto)null));

            Assert.NotNull(ex);
            // assert
        }

    }
}
