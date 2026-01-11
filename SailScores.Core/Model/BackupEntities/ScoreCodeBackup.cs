using System;

namespace SailScores.Core.Model.BackupEntities;

/// <summary>
/// Backup entity for score code.
/// </summary>
public class ScoreCodeBackup
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Formula { get; set; }
    public int? FormulaValue { get; set; }
    public string ScoreLike { get; set; }
    public bool? Discardable { get; set; }
    public bool? CameToStart { get; set; }
    public bool? Started { get; set; }
    public bool? Finished { get; set; }
    public bool? PreserveResult { get; set; }
    public bool? AdjustOtherScores { get; set; }
    public bool? CountAsParticipation { get; set; }
}
