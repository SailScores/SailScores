using System;
using System.Collections.Generic;
using System.Linq;
using SailScores.Api.Enumerations;
using SailScores.Core.FlatModel;
using SailScores.Core.Model;
using SailScores.Core.Scoring;

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
    public IList<SeriesResultsTemplateCustomField> CustomFieldColumns { get; set; } = new List<SeriesResultsTemplateCustomField>();
    public IDictionary<Guid, CompetitorFieldDefinition> CustomFieldDefinitions { get; set; } = new Dictionary<Guid, CompetitorFieldDefinition>();

    // Backward compatibility - computed from CompetitorClubVisibility
    public bool ShowCompetitorClub => CompetitorClubVisibility != ColumnVisibility.Hidden;

    public bool IsExport { get; set; }
    public bool ShowExportButtons { get; set; }

    public bool ExcludeCompetitorLinks { get; set; }

    public bool PreferAlternativeSailNumbers { get; set; }

    // From the results template: shows the pre-discard total scoring column even when
    // the scoring system hides it.
    public bool ShowPreDiscardTotal { get; set; }

    // Scoring columns (after the total) that aren't hidden, with the template override applied.
    public IList<FlatResultColumn> VisibleScoringColumns =>
        (Results?.Columns ?? new List<FlatResultColumn>())
            .Select(c => ShowPreDiscardTotal
                    && c.Key == ScoringColumns.PreDiscardTotalKey
                    && c.Visibility == ColumnVisibility.Hidden
                ? new FlatResultColumn
                {
                    Key = c.Key,
                    Header = c.Header,
                    Format = c.Format,
                    Visibility = ColumnVisibility.Always
                }
                : c)
            .Where(c => c.Visibility != ColumnVisibility.Hidden)
            .ToList();

    public string TrendLegend { get; set; }
    public bool HideDncDiscards { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public string DefaultDateFormat { get; set; }
}
