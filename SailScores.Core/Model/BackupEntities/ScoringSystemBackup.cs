using System;
using System.Collections.Generic;

namespace SailScores.Core.Model.BackupEntities;

/// <summary>
/// Backup entity for scoring system.
/// </summary>
public class ScoringSystemBackup
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string DiscardPattern { get; set; }
    public decimal? ParticipationPercent { get; set; }
    public Guid? ParentSystemId { get; set; }
    
    public IList<ScoreCodeBackup> ScoreCodes { get; set; }
}
