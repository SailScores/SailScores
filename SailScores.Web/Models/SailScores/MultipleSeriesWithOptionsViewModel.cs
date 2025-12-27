using System.ComponentModel.DataAnnotations;
using SailScores.Api.Enumerations;
using SailScores.Core.Model;

namespace SailScores.Web.Models.SailScores;

public class MultipleSeriesWithOptionsViewModel
{
    [Display(Name = "Season")]
    [Required]
    public Guid SeasonId { get; set; }

    public IEnumerable<Season> SeasonOptions { get; set; } = new List<Season>();

    [Display(Name = "Scoring System")]
    public Guid? ScoringSystemId { get; set; }

    public IList<ScoringSystem> ScoringSystemOptions { get; set; } = new List<ScoringSystem>();

    [Display(Name = "Calculate Rank Trend")]
    public TrendOption? TrendOption { get; set; } = Api.Enumerations.TrendOption.None;

    [Display(Name = "Hide discarded DNC scores")]
    public bool HideDncDiscards { get; set; }

    public IList<MultipleSeriesRowViewModel> Series { get; set; } = new List<MultipleSeriesRowViewModel>();

    [Display(Name = "Create summary series")]
    public bool CreateSummarySeries { get; set; }

    [Display(Name = "Summary series name")]
    public string? SummarySeriesName { get; set; }

    [Display(Name = "Children series each count as a single race")]
    public bool SummaryChildrenSeriesAsSingleRace { get; set; }
}

public class MultipleSeriesRowViewModel
{
    [Required]
    public string Name { get; set; } = string.Empty;

    public DateOnly? EnforcedStartDate { get; set; }

    public DateOnly? EnforcedEndDate { get; set; }
}
