using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using SailScores.Core.Model;
using SailScores.Web.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace SailScores.Test.Unit.Web.Services;

public class SeriesServiceIcalTimeZoneBehaviorTests
{
    [Fact(DisplayName = "GetSortedOccurrences applies X-WR-TIMEZONE to floating and UTC event times (behavioral contract)")]
    public void GetSortedOccurrences_AppliesCalendarDefaultTimeZone_WhenPossible_BehavioralContract()
    {
        var calendar = Calendar.Load(@"BEGIN:VCALENDAR
VERSION:2.0
X-WR-TIMEZONE:America/New_York
BEGIN:VEVENT
UID:floating-1
DTSTART:20250115T120000
DTEND:20250115T130000
SUMMARY:Floating Event
END:VEVENT
BEGIN:VEVENT
UID:utc-1
DTSTART:20250115T150000Z
DTEND:20250115T160000Z
SUMMARY:UTC Event
END:VEVENT
END:VCALENDAR");

        var season = new Season
        {
            Start = new DateTime(2025, 1, 1),
            End = new DateTime(2025, 12, 31)
        };

        var method = typeof(SeriesService).GetMethod(
            "GetSortedOccurrences",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);

        var result = method!.Invoke(null, [calendar, season]);
        var occurrences = Assert.IsType<List<Occurrence>>(result);

        var floating = occurrences.Single(o =>
            ((CalendarEvent)o.Source).Summary == "Floating Event");
        var utc = occurrences.Single(o =>
            ((CalendarEvent)o.Source).Summary == "UTC Event");

        Assert.Equal("America/New_York", floating.Period.StartTime.TzId);
        Assert.Equal(new DateTime(2025, 1, 15, 12, 0, 0), floating.Period.StartTime.Value);
        Assert.Equal("America/New_York", floating.Period.EndTime.TzId);
        Assert.Equal(new DateTime(2025, 1, 15, 13, 0, 0), floating.Period.EndTime.Value);

        Assert.Equal("America/New_York", utc.Period.StartTime.TzId);
        Assert.Equal(new DateTime(2025, 1, 15, 10, 0, 0), utc.Period.StartTime.Value);
        Assert.Equal("America/New_York", utc.Period.EndTime.TzId);
        Assert.Equal(new DateTime(2025, 1, 15, 11, 0, 0), utc.Period.EndTime.Value);
    }
}
