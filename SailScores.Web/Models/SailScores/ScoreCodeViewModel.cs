using System.ComponentModel.DataAnnotations;

namespace SailScores.Web.Models.SailScores;

public class ScoreCodeViewModel
{

    public Guid Id { get; set; }
    public Guid ClubId { get; set; }
    public Guid ScoringSystemId { get; set; }
    [StringLength(20)]
    public String Name { get; set; }
    [StringLength(1000)]
    public String Description { get; set; }

    // can be:
    // COD - Use value of ScoreLike to find another code to use
    // FIN+ - competitors who finished this race + FormulaValue
    // SER+ - competitors in this series + FormulaValue
    // CTS+ - competitors who came to start + FormulaValue
    // AVE - average of all non-discarded races
    // AVE P - average of prior races only
    // AVE ND - average excluding discards
    // AVE W - average of all non-discarded races, rounded to whole number
    // AVE W P - average of prior races only, rounded to whole number
    // AVE W ND - average excluding discards, rounded to whole number
    // PLC% - Place + xx% of DNF score (xx is stored FormulaValue)
    // MAN - allow scorer to enter score manually
    // TIE - Tied with previous finisher
    public string Formula { get; set; }

    [Display(Name = "Formula Value")]
    [Range(0, 999.999, ErrorMessage = "Formula Value must be between 0 and 999.999.")]
    [DisplayFormat(DataFormatString = "{0:0.###}", ApplyFormatInEditMode = true)]
    public decimal? FormulaValue { get; set; }
    [Display(Name = "Score like another code:")]
    public string ScoreLike { get; set; }
    [Display(Name = "Result can be discarded")]
    public bool Discardable { get; set; }

    // These next three could be rolled up into one value with four options:
    // Competitor made it to:
    //   None
    //   Came To Start
    //   Started but did not finish
    //   Finished
    [Display(Name = "Came To Start")]
    public bool CameToStart { get; set; }
    public bool Started { get; set; }
    public bool Finished { get; set; }
    [Display(Name = "Preserve the finish place")]
    public bool PreserveResult { get; set; }

    // Should scoring of other following competitors use this as a finisher ahead? 
    [Display(Name = "Adjust other competitors finishing after this score")]
    public bool AdjustOtherScores { get; set; }

    // For High Point scoring, even if not "Came To Start" count this as a race
    // towards participation
    [Display(Name = "Count as Participation (For codes not set as \"Came To Start\")")]
    public bool CountAsParticipation { get; set; }

}
