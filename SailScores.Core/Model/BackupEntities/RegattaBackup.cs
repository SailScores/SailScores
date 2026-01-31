using System;
using System.Collections.Generic;

namespace SailScores.Core.Model.BackupEntities;

/// <summary>
/// Backup entity for regatta.
/// </summary>
public class RegattaBackup
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string UrlName { get; set; }
    public string Description { get; set; }
    public string Url { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
    public Guid? ScoringSystemId { get; set; }
    public bool? PreferAlternateSailNumbers { get; set; }
    public bool? HideFromFrontPage { get; set; }
    
    /// <summary>
    /// Season ID reference.
    /// </summary>
    public Guid? SeasonId { get; set; }
    
    /// <summary>
    /// Series IDs associated with this regatta.
    /// </summary>
    public IList<Guid> SeriesIds { get; set; }
    
    /// <summary>
    /// Fleet IDs associated with this regatta.
    /// </summary>
    public IList<Guid> FleetIds { get; set; }
}
