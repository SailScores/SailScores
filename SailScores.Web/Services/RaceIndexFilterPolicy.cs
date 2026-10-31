using SailScores.Core.Model;
using SailScores.Web.Models.SailScores;
using System.Globalization;

namespace SailScores.Web.Services;

// Decides how to filter the Race Index page. Clubs with few races a season
// see a plain list, and busier clubs get month and fleet chips.
public static class RaceIndexFilterPolicy
{
    public const int ShowChipsMinimumRaces = 25;
    public const int DefaultToMonthMinimumRaces = 100;
    public const string AllMonths = "all";

    private const string MonthFormat = "yyyy-MM";

    public static RaceIndexFilters Build(
        IList<RaceFacet> facets,
        string month,
        string fleet,
        DateTime seasonStart,
        DateTime seasonEnd,
        DateTime today)
    {
        var wantsAllMonths = month == AllMonths;
        DateTime? requestedMonth = null;
        if (!String.IsNullOrEmpty(month) && !wantsAllMonths)
        {
            if (!DateTime.TryParseExact(
                    month,
                    MonthFormat,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var parsedMonth)
                || parsedMonth > seasonEnd
                || parsedMonth.AddMonths(1) <= seasonStart)
            {
                return new RaceIndexFilters { IsValid = false };
            }
            requestedMonth = parsedMonth;
        }

        var selectedFleet = String.IsNullOrEmpty(fleet)
            ? null
            : facets.FirstOrDefault(f => f.FleetShortName == fleet);
        if (!String.IsNullOrEmpty(fleet) && selectedFleet == null)
        {
            return new RaceIndexFilters { IsValid = false };
        }
        var fleetKey = selectedFleet?.FleetShortName;

        var fleetFacets = selectedFleet == null
            ? facets
            : facets.Where(f => f.FleetId == selectedFleet.FleetId).ToList();
        var defaultMonth = GetDefaultMonth(fleetFacets, today);
        var effectiveMonth = requestedMonth ?? (wantsAllMonths ? null : defaultMonth);

        var filters = new RaceIndexFilters
        {
            StartDate = seasonStart,
            EndDate = seasonEnd,
            FleetId = selectedFleet?.FleetId,
            FleetName = selectedFleet?.FleetName,
            RequestedMonth = requestedMonth,
            EffectiveMonth = effectiveMonth,
            HasActiveFilter = !String.IsNullOrEmpty(month) || selectedFleet != null
        };
        if (effectiveMonth.HasValue)
        {
            filters.StartDate = Max(seasonStart, effectiveMonth.Value);
            filters.EndDate = Min(seasonEnd, effectiveMonth.Value.AddMonths(1).AddTicks(-1));
        }

        filters.MonthChips = BuildMonthChips(
            fleetFacets,
            requestedMonth,
            effectiveMonth,
            defaultMonth,
            fleetKey);
        filters.FleetChips = BuildFleetChips(facets, requestedMonth, wantsAllMonths, fleetKey, today);

        var isBusy = facets.Sum(f => f.Count) >= ShowChipsMinimumRaces;
        var distinctMonths = facets.Select(GetMonth).Distinct().Count();
        var distinctFleets = GetFleets(facets).Count;
        filters.ShowMonthChips = requestedMonth.HasValue
            || (distinctMonths > 1 && (isBusy || filters.HasActiveFilter));
        filters.ShowFleetChips = selectedFleet != null
            || (distinctFleets > 1 && (isBusy || filters.HasActiveFilter));

        return filters;
    }

    // Only long lists default to a month. Looks at the selected fleet's races
    // so that a small fleet in a busy club still gets its full season.
    private static DateTime? GetDefaultMonth(IList<RaceFacet> facets, DateTime today)
    {
        var months = facets.Select(GetMonth).Distinct().OrderBy(m => m).ToList();
        if (facets.Sum(f => f.Count) < DefaultToMonthMinimumRaces || months.Count < 2)
        {
            return null;
        }
        var startedMonths = months.Where(m => m <= today).ToList();
        return startedMonths.Count > 0 ? startedMonths[^1] : months[0];
    }

    private static IList<RaceFilterChip> BuildMonthChips(
        IList<RaceFacet> fleetFacets,
        DateTime? requestedMonth,
        DateTime? effectiveMonth,
        DateTime? defaultMonth,
        string fleet)
    {
        var monthCounts = fleetFacets
            .GroupBy(GetMonth)
            .ToDictionary(g => g.Key, g => g.Sum(f => f.Count));
        if (requestedMonth.HasValue)
        {
            monthCounts.TryAdd(requestedMonth.Value, 0);
        }

        var spansYears = monthCounts.Keys.Select(m => m.Year).Distinct().Count() > 1;
        var labelFormat = spansYears ? "MMM yyyy" : "MMM";

        var chips = new List<RaceFilterChip>
        {
            new()
            {
                Count = monthCounts.Values.Sum(),
                IsSelected = !effectiveMonth.HasValue,
                Month = defaultMonth.HasValue ? AllMonths : null,
                Fleet = fleet
            }
        };
        chips.AddRange(monthCounts
            .OrderBy(m => m.Key)
            .Select(m => new RaceFilterChip
            {
                Label = m.Key.ToString(labelFormat, CultureInfo.CurrentCulture),
                Count = m.Value,
                IsSelected = m.Key == effectiveMonth,
                Month = m.Key == defaultMonth
                    ? null
                    : m.Key.ToString(MonthFormat, CultureInfo.InvariantCulture),
                Fleet = fleet
            }));
        return chips;
    }

    // Counts only follow a month named in the URL. A default month must not
    // hide the fleets that have not raced in it.
    private static IList<RaceFilterChip> BuildFleetChips(
        IList<RaceFacet> facets,
        DateTime? requestedMonth,
        bool wantsAllMonths,
        string selectedFleet,
        DateTime today)
    {
        var monthFacets = requestedMonth.HasValue
            ? facets.Where(f => GetMonth(f) == requestedMonth.Value).ToList()
            : facets;

        var chips = new List<RaceFilterChip>
        {
            new()
            {
                Count = monthFacets.Sum(f => f.Count),
                IsSelected = selectedFleet == null,
                Month = GetMonthLink(facets, requestedMonth, wantsAllMonths, today)
            }
        };
        chips.AddRange(GetFleets(facets)
            .Select(f => new RaceFilterChip
            {
                Label = f.FleetName,
                Count = monthFacets
                    .Where(m => m.FleetId == f.FleetId)
                    .Sum(m => m.Count),
                IsSelected = f.FleetShortName == selectedFleet,
                Month = GetMonthLink(
                    facets.Where(m => m.FleetId == f.FleetId).ToList(),
                    requestedMonth,
                    wantsAllMonths,
                    today),
                Fleet = f.FleetShortName
            })
            .Where(c => c.Count > 0 || c.IsSelected));
        return chips;
    }

    // Keeps the current month in a link, unless that month is already what the
    // bare URL would show. This keeps one URL for each page.
    private static string GetMonthLink(
        IList<RaceFacet> facets,
        DateTime? requestedMonth,
        bool wantsAllMonths,
        DateTime today)
    {
        var defaultMonth = GetDefaultMonth(facets, today);
        if (wantsAllMonths)
        {
            return defaultMonth.HasValue ? AllMonths : null;
        }
        return requestedMonth == defaultMonth
            ? null
            : requestedMonth?.ToString(MonthFormat, CultureInfo.InvariantCulture);
    }

    private static IList<RaceFacet> GetFleets(IEnumerable<RaceFacet> facets)
    {
        return facets
            .Where(f => !String.IsNullOrEmpty(f.FleetShortName))
            .GroupBy(f => f.FleetId)
            .Select(g => g.First())
            .OrderBy(f => f.FleetName)
            .ToList();
    }

    private static DateTime GetMonth(RaceFacet facet)
    {
        return new DateTime(facet.Year, facet.Month, 1);
    }

    private static DateTime Max(DateTime first, DateTime second)
    {
        return first > second ? first : second;
    }

    private static DateTime Min(DateTime first, DateTime second)
    {
        return first < second ? first : second;
    }
}
