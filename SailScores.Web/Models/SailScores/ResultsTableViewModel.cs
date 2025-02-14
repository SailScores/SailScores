using SailScores.Core.FlatModel;
using SailScores.Core.Model;

namespace SailScores.Web.Models.SailScores;

public class ResultsTableViewModel
{
    public Guid SeriesId { get; set; }
    public FlatResults Results { get; set; }

    public bool ShowTrend { get; set; }
    public bool ShowTrendOnAllDevices { get; set; }

    public bool ShowCompetitorClub { get; set; }

    public bool IsExport { get; set; }
    public bool ShowExportButtons { get; set; }

    public bool ExcludeCompetitorLinks { get; set; }

    public bool PreferAlternativeSailNumbers { get; set; }

    public string TrendLegend { get; set; }
    public bool HideDncDiscards { get; set; }
    public DateTime? UpdatedDate { get; set; }
}