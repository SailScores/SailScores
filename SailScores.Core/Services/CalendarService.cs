using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SailScores.Core.Model;
using SailScores.Core.Models;
using SailScores.Core.Utility;
using SailScores.Database;
using SailScores.Database.Entities;

namespace SailScores.Core.Services;

public class CalendarService : ICoreCalendarService
{
    private readonly ISailScoresContext _context;
    public CalendarService(ISailScoresContext context)
    {
        _context = context;
    }

    public async Task<List<CalendarEvent>> GetEventsAsync(string clubInitials, DateOnly startDate, DateOnly endDate)
    {
        var clubId = await GetClubId(clubInitials);
        var eventList = new List<CalendarEvent>();

        eventList.AddRange(await GetRegattaCalendarEntries(
            clubInitials,
            clubId,
            startDate.ToDateTime(),
            endDate.ToDateTime()));

        eventList.AddRange(await GetSeriesCalendarEntries(
            clubInitials,
            clubId,
            startDate.ToDateTime(),
            endDate.ToDateTime()));

        // someday: get other events
        // todo: get races without series
        return eventList;
    }

    private async Task<IEnumerable<CalendarEvent>> GetRegattaCalendarEntries(
        string clubInitials,
        Guid clubId,
        DateTime startDate,
        DateTime endDate)
    {
        var regattas = _context
            .Regattas
            .Where(r => r.ClubId == clubId)
            .Where(r => r.StartDate.HasValue && r.StartDate.Value <= endDate)
            .Where(r => r.EndDate.HasValue && r.EndDate.Value >= startDate)
            .Select(r => new CalendarEvent
            {
                StartDate = DateOnly.FromDateTime(r.StartDate.Value),
                EndDate = DateOnly.FromDateTime(r.EndDate.Value),
                Title = $"{r.Season.Name} {r.Name}",
                Description = r.Description,
                Uri = new Uri($"/{clubInitials}/Regatta/{r.Season.Name}/{r.UrlName}", UriKind.Relative),
                EventType = CalendarEventType.Regatta,
                Category = CalendarEventType.Regatta
            });

        return regattas;
    }

    private async Task<IEnumerable<CalendarEvent>> GetSeriesCalendarEntries(
        string clubInitials,
        Guid clubId,
        DateTime startDate,
        DateTime endDate)
    {
        // add series that are date restricted if their date range is 4 days or less.

        // then add other series, with contigous date ranges merged: date ranges
        // should be based on the races within the series.

        var startDateOnly = DateOnly.FromDateTime(startDate);
        var endDateOnly = DateOnly.FromDateTime(endDate);

        // exclude any series that are part of a regatta
        var regattaSeriesIds = await _context.Regattas
            .Where(r => r.ClubId == clubId)
            .SelectMany(r => r.RegattaSeries.Select(rs => rs.SeriesId))
            .ToListAsync();

        var dateLimitedSeriesList = _context
            .Series
            .Where(s => s.ClubId == clubId)
            .Where(s => s.Type != Database.Entities.SeriesType.Regatta)
            .Where(s => (s.DateRestricted ?? false)
                && s.EnforcedStartDate.Value <= endDateOnly
                && s.EnforcedEndDate.Value >= startDateOnly)
            .Where(s => s.EnforcedEndDate.Value.DayNumber - s.EnforcedStartDate.Value.DayNumber <= 4)
            .Where(s => !regattaSeriesIds.Contains(s.Id))
            .Include(s => s.RaceSeries)
            .ThenInclude(rs => rs.Race)
            .ThenInclude(r => r.Fleet)
            .AsSplitQuery();

        var dateLimitedSeriesIds = await dateLimitedSeriesList
            .Select(s => s.Id)
            .ToListAsync();

        var otherSeriesList = _context
            .Series
            .Where(s => s.ClubId == clubId)
            .Where(s => s.RaceSeries.Any(r =>
                r.Race.Date >= startDate
                && r.Race.Date <= endDate))
            .Where(s => !dateLimitedSeriesIds.Contains(s.Id))
            .Where(s => !regattaSeriesIds.Contains(s.Id))
            .Include(s => s.RaceSeries)
            .ThenInclude(rs => rs.Race)
            .ThenInclude (r => r.Fleet)
            .Include(s => s.Season)
            .AsSplitQuery();

        var calendarEvents = new List<CalendarEvent>();
        await foreach (var series in dateLimitedSeriesList.AsAsyncEnumerable())
        {
            var fleetName = series.RaceSeries.Select(r => r.Race)
                .Where(r => r.Fleet != null).OrderByDescending(r => r.Date)
                .Select(r => r.Fleet.Name)
                .FirstOrDefault();
            calendarEvents.Add(new CalendarEvent
            {
                StartDate = series.EnforcedStartDate.Value,
                EndDate = series.EnforcedEndDate.Value,
                Title = $"{series.Name}",
                Description = series.Description,
                Uri = new Uri($"/{clubInitials}/{series.Season.Name}/{series.UrlName}", UriKind.Relative),
                EventType = CalendarEventType.Series,
                Category = fleetName
            });
        }
        await foreach (var series in otherSeriesList.AsAsyncEnumerable())
        {
            var races = series.RaceSeries
                .Select(rs => rs.Race)
                .Where(r => r.Date.HasValue && r.Date.Value >= startDate && r.Date.Value <= endDate)
                .OrderBy(r => r.Date);
            var fleetName = series.RaceSeries.Select(r => r.Race)
                .Where(r => r.Fleet != null).OrderByDescending(r => r.Date)
                .Select(r => r.Fleet.Name)
                .FirstOrDefault();

            var currentEvent = new CalendarEvent
            {
                Title = $"{series.Name}",
                Description = series.Description,
                Uri = new Uri($"/{clubInitials}/{series.Season.Name}/{series.UrlName}", UriKind.Relative),
                EventType = CalendarEventType.Series,
                Category = fleetName
            };
            foreach (var race in races)
            {
                // keep track of current dates of races.
                // as soon as we get to a gap, add the previous event and start a new one.
                if (currentEvent.StartDate == default)
                {
                    currentEvent.StartDate = DateOnly.FromDateTime(race.Date.Value);
                    currentEvent.EndDate = DateOnly.FromDateTime(race.Date.Value);
                } else
                {
                    var raceDateOnly = DateOnly.FromDateTime(race.Date.Value);
                    if(raceDateOnly.DayNumber - currentEvent.EndDate.DayNumber <= 1)
                    {
                        // extend the current event
                        currentEvent.EndDate = raceDateOnly;
                    } else
                    {
                        // gap detected, add the current event and start a new one
                        calendarEvents.Add(currentEvent);
                        currentEvent = new CalendarEvent
                        {
                            StartDate = raceDateOnly,
                            EndDate = raceDateOnly,
                            Title = $"{series.Name}",
                            Description = series.Description,
                            Uri = new Uri($"/{clubInitials}/{series.Season.Name}/{series.UrlName}", UriKind.Relative),
                            EventType = CalendarEventType.Series,
                            Category = fleetName
                        };
                    }
                }

            }
            // add the last event if it has a start date
            if (currentEvent.StartDate != default)
            {
                calendarEvents.Add(currentEvent);
            }
        }

        return calendarEvents;

    }

    private async Task<Guid> GetClubId(string clubInitials)
    {
        return await _context.Clubs
            .Where(c => c.Initials == clubInitials)
            .Select(c => c.Id)
            .FirstOrDefaultAsync();
    }
}
