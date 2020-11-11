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
using SailScores.Api.Dtos;
using SailScores.Test.Unit.Utilities;
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
        private readonly Guid _clubId;
        private readonly String _clubInitials;

        public CompetitorServiceTests()
        {
            _mockCalculator = new Mock<IScoringCalculator>();
            _mockCalculator.Setup(c => c.CalculateResults(It.IsAny<Series>())).Returns(new SeriesResults());
            _mockScoringCalculatorFactory = new Mock<IScoringCalculatorFactory>();
            _mockScoringCalculatorFactory.Setup(f => f.CreateScoringCalculatorAsync(It.IsAny<SailScores.Core.Model.ScoringSystem>()))
                .ReturnsAsync(_mockCalculator.Object);

            _context = Utilities.InMemoryContextBuilder.GetContext();
            _clubId = _context.Clubs.First().Id;
            _clubInitials = _context.Clubs.First().Initials;
            _mapper = MapperBuilder.GetSailScoresMapper();

            _service = new SailScores.Core.Services.CompetitorService(
                _context,
                _mapper
                );
        }

        [Fact]
        public async Task GetInactive_ReturnsOnlyInactive()
        {
            // arrange

            // act
            var result = await _service.GetInactiveCompetitorsAsync(
                _clubId,
                null);

            Assert.True(result.Any());
            Assert.True(result.All(c => !c.IsActive));
            // assert
        }

        [Fact]
        public async Task GetCompetitors_ReturnsActive()
        {
            // arrange

            // act
            var result = await _service.GetCompetitorsAsync(
                _clubId,
                null);

            Assert.True(result.Any());
            Assert.True(result.All(c => c.IsActive));
            // assert
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
