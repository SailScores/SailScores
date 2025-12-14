using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SailScores.Core.Models;

namespace SailScores.Core.Services;

public interface ICoreCalendarService
{
    Task<List<CalendarEvent>> GetEventsAsync(string clubInitials, DateOnly startDate, DateOnly endDate);
}
