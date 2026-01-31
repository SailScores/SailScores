using System;

namespace SailScores.Core.Model.BackupEntities;

/// <summary>
/// Backup entity for regatta forwarder (URL redirect).
/// </summary>
public class RegattaForwarderBackup
{
    public Guid Id { get; set; }
    public string OldClubInitials { get; set; }
    public string OldSeasonUrlName { get; set; }
    public string OldRegattaUrlName { get; set; }
    public Guid RegattaId { get; set; }
    public DateTime Created { get; set; }
}
