using System;

namespace SailScores.Core.Model.BackupEntities;

/// <summary>
/// Backup entity for announcement.
/// </summary>
public class AnnouncementBackup
{
    public Guid Id { get; set; }
    public Guid? RegattaId { get; set; }
    public string Content { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime CreatedLocalDate { get; set; }
    public string CreatedBy { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public DateTime? UpdatedLocalDate { get; set; }
    public string UpdatedBy { get; set; }
    public DateTime? ArchiveAfter { get; set; }
}
