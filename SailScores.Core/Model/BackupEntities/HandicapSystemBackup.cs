using System;
using SailScores.Database.Entities;
using DbHandicapSystemType = SailScores.Database.Entities.HandicapSystemType;

namespace SailScores.Core.Model.BackupEntities;

/// <summary>
/// Backup entity for handicap system.
/// </summary>
public class HandicapSystemBackup
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public DbHandicapSystemType SystemType { get; set; }
    public string Description { get; set; }
    public Guid? ParentSystemId { get; set; }
}
