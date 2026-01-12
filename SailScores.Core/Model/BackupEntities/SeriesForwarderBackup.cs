using System;

namespace SailScores.Core.Model.BackupEntities;

/// <summary>
/// Backup entity for series forwarder (URL redirect).
/// </summary>
public class SeriesForwarderBackup
{
    public Guid Id { get; set; }
    public string OldClubInitials { get; set; }
    public string OldSeasonUrlName { get; set; }
    public string OldSeriesUrlName { get; set; }
    public Guid SeriesId { get; set; }
    public DateTime Created { get; set; }
}
