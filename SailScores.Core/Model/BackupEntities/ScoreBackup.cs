using System;

namespace SailScores.Core.Model.BackupEntities;

/// <summary>
/// Backup entity for score.
/// </summary>
public class ScoreBackup
{
    public Guid Id { get; set; }
    public Guid CompetitorId { get; set; }
    public Guid RaceId { get; set; }
    public int? Place { get; set; }
    public string Code { get; set; }
    public decimal? CodePoints { get; set; }
    public DateTime? FinishTime { get; set; }
    public TimeSpan? ElapsedTime { get; set; }
}
