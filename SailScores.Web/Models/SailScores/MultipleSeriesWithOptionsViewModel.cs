using System.ComponentModel.DataAnnotations;
using SailScores.Api.Enumerations;
using SailScores.Core.Model;

namespace SailScores.Web.Models.SailScores;

public class MultipleSeriesWithOptionsViewModel
{
    public Guid ClubId { get; set; }

    [Display(Name = "Season")]
    [Required]
    public Guid SeasonId { get; set; }

    public IEnumerable<Season> SeasonOptions { get; set; } = new List<Season>();

    [Display(Name = "Scoring System")]
    public Guid? ScoringSystemId { get; set; }

    public IList<ScoringSystem> ScoringSystemOptions { get; set; } = new List<ScoringSystem>();

    [Display(Name = "Fleet")]
    public Guid? FleetId { get; set; }

    public IList<Fleet> FleetOptions { get; set; } = new List<Fleet>();

    [Display(Name = "Use Original Race Positions")]
    public bool? UseFullRaceScores { get; set; }

    [Display(Name = "Calculate Rank Trend")]
    public TrendOption? TrendOption { get; set; } = Api.Enumerations.TrendOption.None;

    [Display(Name = "Hide discarded DNC scores")]
    public bool HideDncDiscards { get; set; }

    public IList<MultipleSeriesRowViewModel> Series { get; set; } = new List<MultipleSeriesRowViewModel>();

    [Display(Name = "Add series to summary series")]
    public string? SummarySummaryOption { get; set; } = "none"; // "none", "create", or "existing"

    [Display(Name = "Summary series name")]
    public string? SummarySeriesName { get; set; }

    [Display(Name = "Use existing summary series")]
    public Guid? ExistingSummarySeriesId { get; set; }

    public IEnumerable<SeriesSummary> SummarySeriesOptions { get; set; } = new List<SeriesSummary>();

    [Display(Name = "Children series each count as a single race")]
    public bool SummaryChildrenSeriesAsSingleRace { get; set; }

    [Obsolete("Use SummarySummaryOption instead")]
    public bool CreateSummarySeries 
    { 
        get => SummarySummaryOption == "create";
        set => SummarySummaryOption = value ? "create" : "none";
    }
}

public class MultipleSeriesRowViewModel
{
    [Required]
    public string Name { get; set; } = string.Empty;

    public DateOnly? EnforcedStartDate { get; set; }

    public DateOnly? EnforcedEndDate { get; set; }
}
