using System;

namespace SailScores.Core.Model.BackupEntities;

public class CompetitorChangeBackup
{
    public Guid Id { get; set; }
    public Guid CompetitorId { get; set; }
    public Guid ChangeTypeId { get; set; }
    public string ChangedBy { get; set; }
    public DateTime ChangeTimeStamp { get; set; }
    public string NewValue { get; set; }
    public string Summary { get; set; }
}
