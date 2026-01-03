using Moq;
using SailScores.Core.Model;
using CoreServices = SailScores.Core.Services;
using SailScores.Web.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;


namespace SailScores.Test.Unit.Web.Services
{
    public class ReportServiceTests
    {
        private readonly Mock<CoreServices.IReportService> _coreReportServiceMock;
        private readonly Mock<CoreServices.IClubService> _clubServiceMock;
        private readonly ReportService _service;

        private readonly string _clubInitials = "TST";
        private readonly Guid _clubId = Guid.NewGuid();

        public ReportServiceTests()
        {
            _coreReportServiceMock = new Mock<CoreServices.IReportService>();
            _clubServiceMock = new Mock<CoreServices.IClubService>();

            _clubServiceMock.Setup(s => s.GetClubId(_clubInitials))
                .ReturnsAsync(_clubId);
            _clubServiceMock.Setup(s => s.GetClubName(_clubInitials))
                .ReturnsAsync("Test Club");

            _service = new ReportService(
                _coreReportServiceMock.Object,
                _clubServiceMock.Object);
        }

        [Fact]
        public async Task GetWindAnalysisAsync_NonAdvancedClub_LimitsDateRangeTo60Days()
        {
            // Arrange
            var club = new Club 
            { 
                Id = _clubId, 
                UseAdvancedFeatures = false 
            };
            _clubServiceMock.Setup(s => s.GetMinimalClub(_clubId))
                .ReturnsAsync(club);

            var startDate = DateTime.Today.AddDays(-90);
            var endDate = DateTime.Today;

            var expectedStartDate = DateTime.Today.AddDays(-60);

            _coreReportServiceMock.Setup(s => s.GetWindDataAsync(
                _clubId, 
                It.IsAny<DateTime?>(), 
                It.IsAny<DateTime?>()))
                .ReturnsAsync(new List<CoreServices.WindDataPoint>());

            // Act
            var result = await _service.GetWindAnalysisAsync(_clubInitials, startDate, endDate);

            // Assert
            _coreReportServiceMock.Verify(s => s.GetWindDataAsync(
                _clubId,
                It.Is<DateTime?>(d => d.HasValue && d.Value.Date == expectedStartDate.Date),
                endDate), Times.Once);
            Assert.False(result.UseAdvancedFeatures);
        }

        [Fact]
        public async Task GetWindAnalysisAsync_AdvancedClub_DoesNotLimitDateRange()
        {
            // Arrange
            var club = new Club 
            { 
                Id = _clubId, 
                UseAdvancedFeatures = true 
            };
            _clubServiceMock.Setup(s => s.GetMinimalClub(_clubId))
                .ReturnsAsync(club);

            var startDate = DateTime.Today.AddDays(-90);
            var endDate = DateTime.Today;

            _coreReportServiceMock.Setup(s => s.GetWindDataAsync(
                _clubId, 
                It.IsAny<DateTime?>(), 
                It.IsAny<DateTime?>()))
                .ReturnsAsync(new List<CoreServices.WindDataPoint>());

            // Act
            var result = await _service.GetWindAnalysisAsync(_clubInitials, startDate, endDate);

            // Assert
            _coreReportServiceMock.Verify(s => s.GetWindDataAsync(
                _clubId,
                startDate,
                endDate), Times.Once);
            Assert.True(result.UseAdvancedFeatures);
        }

        [Fact]
        public async Task GetSkipperStatsAsync_NonAdvancedClub_LimitsDateRangeTo60Days()
        {
            // Arrange
            var club = new Club 
            { 
                Id = _clubId, 
                UseAdvancedFeatures = false 
            };
            _clubServiceMock.Setup(s => s.GetMinimalClub(_clubId))
                .ReturnsAsync(club);

            var startDate = DateTime.Today.AddDays(-90);
            var endDate = DateTime.Today;

            var expectedStartDate = DateTime.Today.AddDays(-60);

            _coreReportServiceMock.Setup(s => s.GetSkipperStatisticsAsync(
                _clubId, 
                It.IsAny<DateTime?>(), 
                It.IsAny<DateTime?>()))
                .ReturnsAsync(new List<CoreServices.SkipperStatistics>());

            // Act
            var result = await _service.GetSkipperStatsAsync(_clubInitials, startDate, endDate);

            // Assert
            _coreReportServiceMock.Verify(s => s.GetSkipperStatisticsAsync(
                _clubId,
                It.Is<DateTime?>(d => d.HasValue && d.Value.Date == expectedStartDate.Date),
                endDate), Times.Once);
            Assert.False(result.UseAdvancedFeatures);
        }

        [Fact]
        public async Task GetSkipperStatsAsync_AdvancedClub_DoesNotLimitDateRange()
        {
            // Arrange
            var club = new Club 
            { 
                Id = _clubId, 
                UseAdvancedFeatures = true 
            };
            _clubServiceMock.Setup(s => s.GetMinimalClub(_clubId))
                .ReturnsAsync(club);

            var startDate = DateTime.Today.AddDays(-90);
            var endDate = DateTime.Today;

            _coreReportServiceMock.Setup(s => s.GetSkipperStatisticsAsync(
                _clubId, 
                It.IsAny<DateTime?>(), 
                It.IsAny<DateTime?>()))
                .ReturnsAsync(new List<CoreServices.SkipperStatistics>());

            // Act
            var result = await _service.GetSkipperStatsAsync(_clubInitials, startDate, endDate);

            // Assert
            _coreReportServiceMock.Verify(s => s.GetSkipperStatisticsAsync(
                _clubId,
                startDate,
                endDate), Times.Once);
            Assert.True(result.UseAdvancedFeatures);
        }

        [Fact]
        public async Task GetParticipationAsync_NonAdvancedClub_LimitsDateRangeTo60Days()
        {
            // Arrange
            var club = new Club 
            { 
                Id = _clubId, 
                UseAdvancedFeatures = false 
            };
            _clubServiceMock.Setup(s => s.GetMinimalClub(_clubId))
                .ReturnsAsync(club);

            var startDate = DateTime.Today.AddDays(-120);
            var endDate = DateTime.Today;

            var expectedStartDate = DateTime.Today.AddDays(-90);

            _coreReportServiceMock.Setup(s => s.GetParticipationMetricsAsync(
                _clubId, 
                It.IsAny<string>(),
                It.IsAny<DateTime?>(), 
                It.IsAny<DateTime?>()))
                .ReturnsAsync(new List<CoreServices.ParticipationMetric>());

            // Act
            var result = await _service.GetParticipationAsync(_clubInitials, "month", startDate, endDate);

            // Assert
            _coreReportServiceMock.Verify(s => s.GetParticipationMetricsAsync(
                _clubId,
                "month",
                It.Is<DateTime?>(d => d.HasValue && d.Value.Date == expectedStartDate.Date),
                endDate), Times.Once);
            Assert.False(result.UseAdvancedFeatures);
        }

        [Fact]
        public async Task GetParticipationAsync_AdvancedClub_DoesNotLimitDateRange()
        {
            // Arrange
            var club = new Club 
            { 
                Id = _clubId, 
                UseAdvancedFeatures = true 
            };
            _clubServiceMock.Setup(s => s.GetMinimalClub(_clubId))
                .ReturnsAsync(club);

            var startDate = DateTime.Today.AddDays(-120);
            var endDate = DateTime.Today;

            _coreReportServiceMock.Setup(s => s.GetParticipationMetricsAsync(
                _clubId, 
                It.IsAny<string>(),
                It.IsAny<DateTime?>(), 
                It.IsAny<DateTime?>()))
                .ReturnsAsync(new List<CoreServices.ParticipationMetric>());

            // Act
            var result = await _service.GetParticipationAsync(_clubInitials, "month", startDate, endDate);

            // Assert
            _coreReportServiceMock.Verify(s => s.GetParticipationMetricsAsync(
                _clubId,
                "month",
                startDate,
                endDate), Times.Once);
            Assert.True(result.UseAdvancedFeatures);
        }

        [Fact]
        public async Task GetWindAnalysisAsync_NonAdvancedClubWithNoStartDate_SetsStartDateTo60DaysAgo()
        {
            // Arrange
            var club = new Club 
            { 
                Id = _clubId, 
                UseAdvancedFeatures = false 
            };
            _clubServiceMock.Setup(s => s.GetMinimalClub(_clubId))
                .ReturnsAsync(club);

            var expectedStartDate = DateTime.Today.AddDays(-60);

            _coreReportServiceMock.Setup(s => s.GetWindDataAsync(
                _clubId, 
                It.IsAny<DateTime?>(), 
                It.IsAny<DateTime?>()))
                .ReturnsAsync(new List<CoreServices.WindDataPoint>());

            // Act
            var result = await _service.GetWindAnalysisAsync(_clubInitials, null, null);

            // Assert
            _coreReportServiceMock.Verify(s => s.GetWindDataAsync(
                _clubId,
                It.Is<DateTime?>(d => d.HasValue && d.Value.Date == expectedStartDate.Date),
                null), Times.Once);
        }
    }
}
