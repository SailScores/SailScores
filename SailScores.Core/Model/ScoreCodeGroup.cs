using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace SailScores.Core.Model;

public class ScoreCodeGroup
{
    /// <summary>
    /// Parameterless constructor for model binding and EF.
    /// </summary>
    public ScoreCodeGroup() { }

    /// <summary>
    /// Parameterized constructor for mapping from database entity.
    /// </summary>
    public ScoreCodeGroup(
        Guid id,
        Guid scoringSystemId,
        string name,
        ScoreCodeGroupLimitationType limitationType,
        decimal limitationValue,
        bool useNonDiscardedRaces,
        string overageCodeName,
        ScoreCodeGroupOverageSelection overageSelectionMethod,
        IList<string> includedCodeNames)
    {
        Id = id;
        ScoringSystemId = scoringSystemId;
        Name = name;
        LimitationType = limitationType;
        LimitationValue = limitationValue;
        UseNonDiscardedRaces = useNonDiscardedRaces;
        OverageCodeName = overageCodeName;
        OverageSelectionMethod = overageSelectionMethod;
        IncludedCodeNames = includedCodeNames ?? new List<string>();
    }

    public Guid Id { get; set; }

    public Guid ScoringSystemId { get; set; }

    /// <summary>
    /// Optional name for the group. If null or empty, DisplayName will use joined code names.
    /// </summary>
    [StringLength(200)]
    public string Name { get; set; }

    public ScoreCodeGroupLimitationType LimitationType { get; set; }

    public decimal LimitationValue { get; set; }

    /// <summary>
    /// When LimitationType is PercentOfRaces: if true, calculate percent against non-discarded
    /// races; if false, calculate against all races. Ignored for other limitation types.
    /// </summary>
    public bool UseNonDiscardedRaces { get; set; } = true;

    [StringLength(20)]
    public string OverageCodeName { get; set; } = "DNC";

    public ScoreCodeGroupOverageSelection OverageSelectionMethod { get; set; } = ScoreCodeGroupOverageSelection.LatestFirst;

    /// <summary>
    /// List of score code names included in this group.
    /// </summary>
    public IList<string> IncludedCodeNames { get; set; } = new List<string>();

    /// <summary>
    /// Returns Name if supplied, otherwise returns IncludedCodeNames joined by ", ".
    /// </summary>
    public string DisplayName
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(Name))
            {
                return Name;
            }
            return IncludedCodeNames != null && IncludedCodeNames.Count > 0
                ? string.Join(", ", IncludedCodeNames.OrderBy(c => c))
                : "(No codes)";
        }
    }
}
