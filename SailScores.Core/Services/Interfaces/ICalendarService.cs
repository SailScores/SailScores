using System.Collections.Generic;
using System.Threading.Tasks;
using SailScores.Core.Models;

namespace SailScores.Core.Services
{
    public interface ICalendarService
    {
        Task<List<CalendarEvent>> GetEventsAsync(string clubInitials);
    }
}
