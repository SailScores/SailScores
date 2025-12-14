using System;

namespace SailScores.Core.Models;

public class CalendarEvent
{
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    public string Title { get; set; }
    public string Description { get; set; }
    public Uri Uri { get; set; }

    // typically "series", "regatta", or "race"
    // likely to add "custom" in the future.
    public string EventType { get; set; }

    // used so that associated events can be grouped
    // for display. Not expected to be presented to
    // users. Often set to class name for events.
    public string Category { get; set; }
}

public static class CalendarEventType
{
    public const string Series = "series";
    public const string Regatta = "regatta";
    public const string Race = "race";
}
