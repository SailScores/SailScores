using System;

namespace SailScores.Core.Model.BackupEntities;

/// <summary>
/// Backup entity for boat class.
/// </summary>
public class BoatClassBackup
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
}
