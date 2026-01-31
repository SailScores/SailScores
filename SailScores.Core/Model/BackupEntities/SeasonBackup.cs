using System;

namespace SailScores.Core.Model.BackupEntities;

/// <summary>
/// Backup entity for season.
/// </summary>
public class SeasonBackup
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string UrlName { get; set; }
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public Guid? DefaultScoringSystemId { get; set; }
}
