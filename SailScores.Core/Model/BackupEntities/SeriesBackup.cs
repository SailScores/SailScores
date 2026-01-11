using System;
using System.Collections.Generic;
using SailScores.Api.Enumerations;

namespace SailScores.Core.Model.BackupEntities;

/// <summary>
/// Backup entity for series.
/// </summary>
public class SeriesBackup
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string UrlName { get; set; }
    public string Description { get; set; }
    public Database.Entities.SeriesType? Type { get; set; }
    public bool? IsImportantSeries { get; set; }
    public bool? ResultsLocked { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public string UpdatedBy { get; set; }
    public Guid? ScoringSystemId { get; set; }
    public TrendOption? TrendOption { get; set; }
    public Guid? FleetId { get; set; }
    public bool? PreferAlternativeSailNumbers { get; set; }
    public bool? ExcludeFromCompetitorStats { get; set; }
    public bool? HideDncDiscards { get; set; }
    public bool? ChildrenSeriesAsSingleRace { get; set; }
    public int? RaceCount { get; set; }
    public bool? DateRestricted { get; set; }
    public DateOnly? EnforcedStartDate { get; set; }
    public DateOnly? EnforcedEndDate { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    
    /// <summary>
    /// Season ID reference.
    /// </summary>
    public Guid? SeasonId { get; set; }
    
    /// <summary>
    /// Child series IDs for series-to-series relationships.
    /// </summary>
    public IList<Guid> ChildrenSeriesIds { get; set; }
    
    /// <summary>
    /// Parent series IDs for series-to-series relationships.
    /// </summary>
    public IList<Guid> ParentSeriesIds { get; set; }
}
