using System;
using System.Linq;
using System.Threading.Tasks;
using SailScores.Core.Services;
using SailScores.Core.Models;
using SailScores.Test.Unit.Utilities;
using SailScores.Database;
using SailScores.Database.Entities;
using Xunit;

namespace SailScores.Test.Unit.Core.Services
{
    public class CalendarServiceTests
    {
        private readonly ISailScoresContext _context;
        private readonly CalendarService _service;
        private readonly string _clubInitials;

        public CalendarServiceTests()
        {
            _context = InMemoryContextBuilder.GetContext();
            _service = new CalendarService(_context);
            _clubInitials = _context.Clubs.First().Initials;
        }

        [Fact]
        public async Task GetEventsAsync_IncludesRegattaAndSeries_WhenRangeCoversThem()
        {
            var race = _context.Races.First();
            var regatta = _context.Regattas.First();

            var raceDate = race.Date ?? DateTime.Now;
            var regattaStart = regatta.StartDate ?? DateTime.Now;

            var start = DateOnly.FromDateTime(raceDate < regattaStart ? raceDate : regattaStart);
            var end = DateOnly.FromDateTime(raceDate > regattaStart ? raceDate : regattaStart);
            end = end.AddDays(1);

            var events = await _service.GetEventsAsync(_clubInitials, start, end);

            Assert.NotNull(events);
            Assert.Contains(events, e => e.EventType == CalendarEventType.Regatta);
            Assert.Contains(events, e => e.EventType == CalendarEventType.Series);
        }

        [Fact]
        public async Task GetEventsAsync_ReturnsEmpty_WhenRangeDoesNotIncludeAnyEvents()
        {
            var start = DateOnly.FromDateTime(DateTime.Today.AddYears(5));
            var end = start.AddDays(1);

            var events = await _service.GetEventsAsync(_clubInitials, start, end);

            Assert.NotNull(events);
            Assert.Empty(events);
        }

        [Fact]
        public async Task GetEventsAsync_ReturnsEmpty_WhenClubNotFound()
        {
            var start = DateOnly.FromDateTime(DateTime.Today.AddYears(-1));
            var end = DateOnly.FromDateTime(DateTime.Today.AddYears(1));

            var events = await _service.GetEventsAsync("NO_SUCH_CLUB", start, end);

            Assert.NotNull(events);
            Assert.Empty(events);
        }
    }
}
