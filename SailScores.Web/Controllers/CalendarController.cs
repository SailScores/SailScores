using Microsoft.AspNetCore.Mvc;
using SailScores.Core.Models;
using SailScores.Web.Models.SailScores;
using SailScores.Web.Services;
using SailScores.Web.Services.Interfaces;

namespace SailScores.Web.Controllers;

public class CalendarController : Controller
{
    private readonly ICalendarService _calendarService;
    public CalendarController(ICalendarService calendarService)
    {
        _calendarService = calendarService;
    }

    // GET: /{clubInitials}/Calendar
    public async Task<IActionResult> Index(string clubInitials,
        DateOnly? start,
        DateOnly? end)
    {
        if (start == null)
        {
            start = _calendarService.GetDefaultStartDate();
        }
        if (end == null)
        {
            end = _calendarService.GetDefaultEndDate();
        }
        var calendarEvents = await _calendarService.GetCalendarEventsAsync(clubInitials, start, end);
        var vm = new CalendarViewModel
        {
            ClubInitials = clubInitials?.ToUpperInvariant(),
            List = calendarEvents,
            CanEdit = false,
            StartDate = start.Value,
            EndDate = end.Value
        };
        return View(vm);
    }
}
