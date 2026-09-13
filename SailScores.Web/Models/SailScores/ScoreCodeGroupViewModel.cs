using SailScores.Core.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SailScores.Web.Models.SailScores;

public class ScoreCodeGroupViewModel : ScoreCodeGroup
{
    /// <summary>
    /// Parameterless constructor for model binding and form handling.
    /// </summary>
    public ScoreCodeGroupViewModel() { }

    /// <summary>
    /// Parameterized constructor for mapping from model.
    /// </summary>
    public ScoreCodeGroupViewModel(
        ScoreCodeGroup group,
        IList<ScoreCode> availableCodes)
    {
        Id = group.Id;
        ScoringSystemId = group.ScoringSystemId;
        Name = group.Name;
        LimitationType = group.LimitationType;
        LimitationValue = group.LimitationValue;
        UseNonDiscardedRaces = group.UseNonDiscardedRaces;
        OverageCodeName = group.OverageCodeName;
        OverageSelectionMethod = group.OverageSelectionMethod;
        IncludedCodeNames = group.IncludedCodeNames;
        AvailableCodes = availableCodes;
    }

    /// <summary>
    /// List of all available score codes for this scoring system (own + inherited).
    /// </summary>
    public IList<ScoreCode> AvailableCodes { get; set; } = new List<ScoreCode>();
}
