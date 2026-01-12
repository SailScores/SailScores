using System;

namespace SailScores.Core.Model.BackupEntities;

/// <summary>
/// Backup entity for competitor forwarder (URL redirect).
/// </summary>
public class CompetitorForwarderBackup
{
    public Guid Id { get; set; }
    public string OldClubInitials { get; set; }
    public string OldCompetitorUrl { get; set; }
    public Guid CompetitorId { get; set; }
    public DateTime Created { get; set; }
}
