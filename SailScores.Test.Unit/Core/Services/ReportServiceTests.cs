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
        public async Task GetSkipperStatisticsAsync_WithFinishesAndCodedScore_AveragesFinishPlacesOnly()
        {
            // Arrange: seeded race has Comp1 in 1st; add a 4th place and a coded (no place) result
            var competitor = _context.Competitors.First(c => c.Name == "Comp1");
            var series = _context.Series.First(s => s.UrlName == "SeriesOne");
            AddRace(competitor.Id, series, place: 4, code: null);
            AddRace(competitor.Id, series, place: null, code: "DNF");
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetSkipperStatisticsAsync(_clubId);

            // Assert
            var stat = Assert.Single(result, s => s.CompetitorId == competitor.Id);
            Assert.Equal(3, stat.RacesParticipated);
            Assert.Equal(2.5m, stat.AveragePlace);
        }

        [Fact]
        public async Task GetSkipperStatisticsAsync_WithOnlyCodedScores_ReturnsNullAveragePlace()
        {
            // Arrange
            var competitor = _context.Competitors.First(c => c.Name == "Comp12");
            var series = _context.Series.First(s => s.UrlName == "SeriesOne");
            AddRace(competitor.Id, series, place: null, code: "DNC");
            await _context.SaveChangesAsync();

            // Act
            var result = await _service.GetSkipperStatisticsAsync(_clubId);

            // Assert
            var stat = Assert.Single(result, s => s.CompetitorId == competitor.Id);
            Assert.Null(stat.AveragePlace);
        }

        private void AddRace(Guid competitorId, Database.Entities.Series series, int? place, string code)
        {
            _context.Races.Add(new Database.Entities.Race
            {
                Id = Guid.NewGuid(),
                Date = DateTime.Now,
                ClubId = _clubId,
                Scores = new System.Collections.Generic.List<Database.Entities.Score>
                {
                    new Database.Entities.Score { CompetitorId = competitorId, Place = place, Code = code }
                },
                SeriesRaces = new System.Collections.Generic.List<Database.Entities.SeriesRace>
                {
                    new Database.Entities.SeriesRace { Series = series }
                }
            });
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
