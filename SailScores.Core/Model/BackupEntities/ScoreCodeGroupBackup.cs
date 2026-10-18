using System;
using System.Collections.Generic;

namespace SailScores.Core.Model.BackupEntities;

/// <summary>
/// Backup entity for a score code group: a limit on how often a set of codes may be used.
/// </summary>
public class ScoreCodeGroupBackup
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public ScoreCodeGroupLimitationType LimitationType { get; set; }
    public decimal LimitationValue { get; set; }
    public bool UseNonDiscardedRaces { get; set; }
    public string OverageCodeName { get; set; }
    public ScoreCodeGroupOverageSelection OverageSelectionMethod { get; set; }

    /// <summary>
    /// Names of the codes in the group. Codes are matched by name, so they may be
    /// inherited from a parent system.
    /// </summary>
    public IList<string> Codes { get; set; }
}
