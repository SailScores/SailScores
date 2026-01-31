using System;

namespace SailScores.Core.Model.BackupEntities;

/// <summary>
/// Backup entity for document.
/// </summary>
public class DocumentBackup
{
    public Guid Id { get; set; }
    public Guid? RegattaId { get; set; }
    public string Name { get; set; }
    public string ContentType { get; set; }
    public byte[] FileContents { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime CreatedLocalDate { get; set; }
    public string CreatedBy { get; set; }
}
