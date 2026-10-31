using SailScores.Web.Services;

namespace SailScores.Web.Models.SailScores;

public class RaceIndexFilters
{
    public bool IsValid { get; set; } = true;

    // The race list query, which may be narrower than the season.
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public Guid? FleetId { get; set; }

    // First of the month when the URL names a month, not when one is the default.
    public DateTime? RequestedMonth { get; set; }
    public string FleetName { get; set; }

    public bool HasActiveFilter { get; set; }

    public IList<RaceFilterChip> MonthChips { get; set; } = new List<RaceFilterChip>();
    public IList<RaceFilterChip> FleetChips { get; set; } = new List<RaceFilterChip>();

    public bool ShowMonthChips { get; set; }
    public bool ShowFleetChips { get; set; }

    public bool IsFleetAndMonth => RequestedMonth.HasValue && FleetId.HasValue;
}

public class RaceFilterChip
{
    public string Label { get; set; }
    public int Count { get; set; }
    public bool IsSelected { get; set; }

    // Query string values for the link. Null leaves the parameter out.
    public string Month { get; set; }
    public string Fleet { get; set; }

    public bool IsFleetAndMonth => Fleet != null
        && Month != null
        && Month != RaceIndexFilterPolicy.AllMonths;
}
