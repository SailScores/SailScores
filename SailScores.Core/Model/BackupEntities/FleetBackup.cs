using System;
using System.Collections.Generic;
using SailScores.Api.Enumerations;

namespace SailScores.Core.Model.BackupEntities;

/// <summary>
/// Backup entity for fleet.
/// </summary>
public class FleetBackup
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string ShortName { get; set; }
    public string NickName { get; set; }
    public string Description { get; set; }
    public bool? IsActive { get; set; }
    public FleetType FleetType { get; set; }
    
    /// <summary>
    /// Boat class IDs associated with this fleet.
    /// </summary>
    public IList<Guid> BoatClassIds { get; set; }
}
