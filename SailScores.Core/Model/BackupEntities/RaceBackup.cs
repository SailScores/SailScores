using System;
using System.Collections.Generic;
using SailScores.Api.Enumerations;

namespace SailScores.Core.Model.BackupEntities;

/// <summary>
/// Backup entity for race.
/// </summary>
public class RaceBackup
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public DateTime? Date { get; set; }
    public RaceState? State { get; set; }
    public int Order { get; set; }
    public string Description { get; set; }
    public string TrackingUrl { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public string UpdatedBy { get; set; }
    public DateTime? StartTime { get; set; }
    public bool TrackTimes { get; set; }
    
    /// <summary>
    /// Fleet ID reference.
    /// </summary>
    public Guid? FleetId { get; set; }
    
    /// <summary>
    /// Weather data for this race.
    /// </summary>
    public WeatherBackup Weather { get; set; }
    
    /// <summary>
    /// Scores for this race.
    /// </summary>
    public IList<ScoreBackup> Scores { get; set; }
    
    /// <summary>
    /// Series IDs this race is associated with.
    /// </summary>
    public IList<Guid> SeriesIds { get; set; }
}
