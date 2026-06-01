using SailScores.Api.Enumerations;
using SailScores.Core.FlatModel;
using SailScores.Core.Model;

namespace SailScores.Web.Models.SailScores;

public class ResultsTableViewModel
{
    public Guid SeriesId { get; set; }
    public FlatResults Results { get; set; }

    public bool ShowTrend { get; set; }
    public bool ShowTrendOnAllDevices { get; set; }

    // Column visibility settings from template
    public ColumnVisibility SailNumberVisibility { get; set; } = ColumnVisibility.Always;
    public ColumnVisibility CompetitorNameVisibility { get; set; } = ColumnVisibility.Always;
    public string CompetitorNameHeader { get; set; } = "Helm";
    public ColumnVisibility BoatNameVisibility { get; set; } = ColumnVisibility.OnLargerScreens;
    public string BoatNameHeader { get; set; } = "Boat";
    public ColumnVisibility CompetitorClubVisibility { get; set; } = ColumnVisibility.Hidden;

    // Backward compatibility - computed from CompetitorClubVisibility
    public bool ShowCompetitorClub => CompetitorClubVisibility != ColumnVisibility.Hidden;

    public bool IsExport { get; set; }
    public bool ShowExportButtons { get; set; }

    public bool ExcludeCompetitorLinks { get; set; }

    public bool PreferAlternativeSailNumbers { get; set; }

    public string TrendLegend { get; set; }
    public bool HideDncDiscards { get; set; }
    public DateTime? UpdatedDate { get; set; }
}
