using System;

namespace SailScores.Core.Model.BackupEntities;

/// <summary>
/// Backup entity for competitor handicap.
/// </summary>
public class CompetitorHandicapBackup
{
    public Guid Id { get; set; }
    public Guid CompetitorId { get; set; }
    public Guid HandicapSystemId { get; set; }
    public decimal Value { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string Notes { get; set; }
}
