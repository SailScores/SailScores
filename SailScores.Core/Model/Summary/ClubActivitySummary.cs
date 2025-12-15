using System;

namespace SailScores.Core.Model.Summary;

public class ClubActivitySummary
{
    public Guid Id { get; set; }
    public String Name { get; set; }
    public String Initials { get; set; }
    public String Description { get; set; }
    public bool IsHidden { get; set; }
    public int RecentRaceCount { get; set; }
    public int RecentSeriesCount { get; set; }
    public DateTime MostRecentActivity { get; set; }
}
