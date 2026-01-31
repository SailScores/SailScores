using System;

namespace SailScores.Core.Model.BackupEntities;

/// <summary>
/// Backup entity for historical results cache.
/// </summary>
public class HistoricalResultsBackup
{
    public Guid Id { get; set; }
    public Guid SeriesId { get; set; }
    public bool IsCurrent { get; set; }
    public string Results { get; set; }
    public DateTime Created { get; set; }
}
