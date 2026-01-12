using System;

namespace SailScores.Core.Model.BackupEntities;

/// <summary>
/// Backup entity for file (used for club logo).
/// </summary>
public class FileBackup
{
    public Guid Id { get; set; }
    public byte[] FileContents { get; set; }
    public DateTime Created { get; set; }
    public DateTime? ImportedTime { get; set; }
}
