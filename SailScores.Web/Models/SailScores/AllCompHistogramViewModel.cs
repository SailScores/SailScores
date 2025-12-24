using System;
using System.Collections.Generic;

namespace SailScores.Web.Models.SailScores;

public class AllCompHistogramViewModel
{
    public string ClubInitials { get; set; }
    public string ClubName { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool UseAdvancedFeatures { get; set; }

    public List<string> Codes { get; set; } = new List<string>();
    public List<int> Places { get; set; } = new List<int>();
    public List<AllCompHistogramRow> Rows { get; set; } = new List<AllCompHistogramRow>();
}

public class AllCompHistogramRow
{
    public string CompetitorName { get; set; }
    public string SailNumber { get; set; }
    public string SeasonName { get; set; }
    public string AggregationType { get; set; }

    public Dictionary<string, int?> CodeCounts { get; set; } = new Dictionary<string, int?>();
    public Dictionary<int, int?> PlaceCounts { get; set; } = new Dictionary<int, int?>();
}
