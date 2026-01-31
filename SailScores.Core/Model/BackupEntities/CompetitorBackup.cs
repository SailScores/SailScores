using System;
using System.Collections.Generic;

namespace SailScores.Core.Model.BackupEntities;

/// <summary>
/// Backup entity for competitor.
/// </summary>
public class CompetitorBackup
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string SailNumber { get; set; }
    public string AlternativeSailNumber { get; set; }
    public string BoatName { get; set; }
    public string HomeClubName { get; set; }
    public string Notes { get; set; }
    public bool? IsActive { get; set; }
    public Guid BoatClassId { get; set; }
    public string UrlName { get; set; }
    public string UrlId { get; set; }
    public DateTime? Created { get; set; }
    
    /// <summary>
    /// Fleet IDs this competitor is associated with.
    /// </summary>
    public IList<Guid> FleetIds { get; set; }
}
