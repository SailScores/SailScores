using SailScores.Core.Services;
using SailScores.Database;
using SailScores.Test.Unit.Utilities;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Moq;

namespace SailScores.Test.Unit.Core.Services
{
    public class ReportServiceTests
    {
        private readonly ReportService _service;
        private readonly Guid _clubId;
        private readonly ISailScoresContext _context;

        public ReportServiceTests()
        {
            _context = InMemoryContextBuilder.GetContext();

            // Mock the required dependencies
            var conversionServiceMock = new Mock<IConversionService>();
            var clubServiceMock = new Mock<IClubService>();

            _service = new ReportService(_context, conversionServiceMock.Object, clubServiceMock.Object);
            _clubId = _context.Clubs.First().Id;
        }

        [Fact]
        public async Task GetWindDataAsync_ReturnsData()
        {
            // Act
            var result = await _service.GetWindDataAsync(_clubId);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetWindDataAsync_WithDateRange_FiltersData()
        {
            // Arrange
            var startDate = DateTime.Today.AddDays(-30);
            var endDate = DateTime.Today;

            // Act
            var result = await _service.GetWindDataAsync(_clubId, startDate, endDate);

            // Assert
            Assert.NotNull(result);
            Assert.All(result, item => 
            {
                Assert.True(item.Date >= startDate && item.Date <= endDate);
            });
        }

        [Fact]
        public async Task GetSkipperStatisticsAsync_ReturnsData()
        {
            // Act
            var result = await _service.GetSkipperStatisticsAsync(_clubId);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetParticipationMetricsAsync_WithMonthGrouping_ReturnsData()
        {
            // Act
            var result = await _service.GetParticipationMetricsAsync(_clubId, "month");

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetParticipationMetricsAsync_WithWeekGrouping_ReturnsData()
        {
            // Act
            var result = await _service.GetParticipationMetricsAsync(_clubId, "week");

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        public async Task GetParticipationMetricsAsync_WithDayGrouping_ReturnsData()
        {
            // Act
            var result = await _service.GetParticipationMetricsAsync(_clubId, "day");

            // Assert
            Assert.NotNull(result);
        }
    }
}
