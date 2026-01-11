using System;

namespace SailScores.Core.Model.BackupEntities;

/// <summary>
/// Metadata about the backup file.
/// </summary>
public class ClubBackupMetadata
{
    public const int CurrentVersion = 1;

    public int Version { get; set; } = CurrentVersion;
    public DateTime CreatedDateUtc { get; set; }
    public string SourceClubInitials { get; set; }
    public string SourceClubName { get; set; }
    public Guid SourceClubId { get; set; }
    public string CreatedBy { get; set; }
}
