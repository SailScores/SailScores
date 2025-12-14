using SailScores.Core.Models;
using SailScores.Web.Services.Interfaces;

namespace SailScores.Web.Services;

public class CalendarService : ICalendarService
{
    private readonly CoreServices.ICoreCalendarService _coreCalendarService;
    public CalendarService(CoreServices.ICoreCalendarService coreCalendarService)
    {
        _coreCalendarService = coreCalendarService;
    }

    public async Task<IEnumerable<CalendarEvent>> GetCalendarEventsAsync(
        string clubInitials,
        DateOnly? startDate = null,
        DateOnly? endDate = null)
    {
        if (startDate == null)
        {
            startDate = GetDefaultStartDate();
        }
        if (endDate == null)
        {
            endDate = GetDefaultEndDate();
        }

        var events = await _coreCalendarService.GetEventsAsync(clubInitials, startDate.Value, endDate.Value);
        var orderedEvents = events.OrderBy(e => e.Title);
        return orderedEvents;
    }

    public DateOnly GetDefaultStartDate()
    {
        // default to the previous Monday minus one week:
        DateTime today = DateTime.Today;
        int daysSinceMonday = ((int)today.DayOfWeek + 6) % 7;
        DateTime previousMonday = today.AddDays(-daysSinceMonday);
        return DateOnly.FromDateTime(previousMonday.AddDays(-7));
    }
    public DateOnly GetDefaultEndDate()
    {
        // default to the next Sunday plus five weeks:
        DateTime today = DateTime.Today;
        int daysUntilSunday = (7 - (int)today.DayOfWeek) % 7;
        DateTime nextSunday = today.AddDays(daysUntilSunday);
        return DateOnly.FromDateTime(nextSunday.AddDays(35));
    }
}
