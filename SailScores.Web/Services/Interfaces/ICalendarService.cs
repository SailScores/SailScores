using SailScores.Core.Models;

namespace SailScores.Web.Services.Interfaces;

public interface ICalendarService
{
    Task<IEnumerable<CalendarEvent>> GetCalendarEventsAsync(
        string clubInitials,
        DateOnly? startDate = null,
        DateOnly? endDate = null);
    DateOnly GetDefaultEndDate();
    DateOnly GetDefaultStartDate();
}
