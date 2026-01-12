using System;

namespace SailScores.Core.Model.BackupEntities;

/// <summary>
/// Backup entity for club sequence.
/// </summary>
public class ClubSequenceBackup
{
    public Guid Id { get; set; }
    public int NextValue { get; set; }
    public string SequenceType { get; set; }
    public string SequencePrefix { get; set; }
    public string SequenceSuffix { get; set; }
}
