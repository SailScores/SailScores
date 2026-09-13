using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SailScores.Database.Entities;

public class ScoreCodeGroup
{
    /// <summary>
    /// Parameterless constructor for EF.
    /// </summary>
    public ScoreCodeGroup() { }

    /// <summary>
    /// Parameterized constructor for mapping from model.
    /// </summary>
    public ScoreCodeGroup(
        Guid id,
        Guid scoringSystemId,
        string name,
        int limitationType,
        decimal limitationValue,
        bool useNonDiscardedRaces,
        string overageCodeName,
        int overageSelectionMethod)
    {
        Id = id;
        ScoringSystemId = scoringSystemId;
        Name = name;
        LimitationType = limitationType;
        LimitationValue = limitationValue;
        UseNonDiscardedRaces = useNonDiscardedRaces;
        OverageCodeName = overageCodeName;
        OverageSelectionMethod = overageSelectionMethod;
    }

    public Guid Id { get; set; }

    public Guid ScoringSystemId { get; set; }

    [StringLength(200)]
    public string Name { get; set; }

    public int LimitationType { get; set; }

    [Column("LimitationValue", TypeName = "decimal(6,3)")]
    public decimal LimitationValue { get; set; }

    public bool UseNonDiscardedRaces { get; set; }

    [StringLength(20)]
    public string OverageCodeName { get; set; }

    public int OverageSelectionMethod { get; set; }

    // Navigation properties
    [ForeignKey("ScoringSystemId")]
    public ScoringSystem ScoringSystem { get; set; }

    public IList<ScoreCodeGroupCode> Codes { get; set; } = new List<ScoreCodeGroupCode>();
}
