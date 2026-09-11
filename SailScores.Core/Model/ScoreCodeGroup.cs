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
    /// Parameterized constructor for explicit field initialization.
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

    /// <summary>
    /// Constructor for mapping from database entity.
    /// </summary>
    public ScoreCodeGroup(SailScores.Database.Entities.ScoreCodeGroup dbEntity)
    {
        Id = dbEntity.Id;
        ScoringSystemId = dbEntity.ScoringSystemId;
        Name = dbEntity.Name;
        LimitationType = (ScoreCodeGroupLimitationType)dbEntity.LimitationType;
        LimitationValue = dbEntity.LimitationValue;
        UseNonDiscardedRaces = dbEntity.UseNonDiscardedRaces;
        OverageCodeName = dbEntity.OverageCodeName;
        OverageSelectionMethod = (ScoreCodeGroupOverageSelection)dbEntity.OverageSelectionMethod;
        IncludedCodeNames = dbEntity.Codes?.Select(c => c.CodeName).ToList() ?? new List<string>();
    }

    public Guid Id { get; set; }

    public Guid ScoringSystemId { get; set; }

    /// <summary>
    /// Optional name for the group. If null or empty, DisplayName will use joined code names.
    /// </summary>
    [StringLength(200)]
    public string Name { get; set; }


    [Display(Name = "Limit Type")]
    public ScoreCodeGroupLimitationType LimitationType { get; set; }

    [DisplayFormat(DataFormatString = "{0:#.##}")]
    [Display(Name = "Limit Value")]
    public decimal LimitationValue { get; set; }

    /// <summary>
    /// When LimitationType is PercentOfRaces: if true, calculate percent against non-discarded
    /// races; if false, calculate against all races. Ignored for other limitation types.
    /// </summary>
    /// 
    [Display(Name = "Use Discard Races Only")]
    public bool UseNonDiscardedRaces { get; set; } = true;

    [StringLength(20)]

    [Display(Name = "Overage Code")]
    public string OverageCodeName { get; set; } = "DNC";


    [Display(Name = "Overage Selection")]
    public ScoreCodeGroupOverageSelection OverageSelectionMethod { get; set; } = ScoreCodeGroupOverageSelection.LatestFirst;

    /// <summary>
    /// List of score code names included in this group.
    /// </summary>
    /// 
    [Display(Name = "Included Codes")]
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

    /// <summary>
    /// Converts this model to a database entity.
    /// </summary>
    public SailScores.Database.Entities.ScoreCodeGroup ToDbObject()
    {
        return new SailScores.Database.Entities.ScoreCodeGroup(
            Id,
            ScoringSystemId,
            Name,
            (int)LimitationType,
            LimitationValue,
            UseNonDiscardedRaces,
            OverageCodeName,
            (int)OverageSelectionMethod)
        {
            Codes = (IncludedCodeNames ?? new List<string>())
                .Select(codeName => new SailScores.Database.Entities.ScoreCodeGroupCode { CodeName = codeName })
                .ToList()
        };
    }
}
