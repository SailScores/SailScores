using SailScores.Api.Enumerations;
using SailScores.Core.Model;
using System.ComponentModel.DataAnnotations;

namespace SailScores.Web.Models.SailScores;

public class SeriesResultsTemplateViewModel
{
    public SeriesResultsTemplateViewModel()
    {
    }

    public SeriesResultsTemplateViewModel(SeriesResultsTemplate template)
    {
        Id = template.Id;
        ClubId = template.ClubId;
        Name = template.Name;
        SailNumberVisibility = template.SailNumberVisibility;
        SailNumberHeader = template.SailNumberHeader;
        AlternativeSailNumberVisibility = template.AlternativeSailNumberVisibility;
        AlternativeSailNumberHeader = template.AlternativeSailNumberHeader;
        CompetitorNameVisibility = template.CompetitorNameVisibility;
        CompetitorNameHeader = template.CompetitorNameHeader;
        BoatNameVisibility = template.BoatNameVisibility;
        BoatNameHeader = template.BoatNameHeader;
        CompetitorClubVisibility = template.CompetitorClubVisibility;
        CompetitorClubHeader = template.CompetitorClubHeader;
        TotalHeader = template.TotalHeader;
        PreDiscardTotalVisibility = template.PreDiscardTotalVisibility;
        ParticipationVisibility = template.ParticipationVisibility;
        ShowClubLogo = template.ShowClubLogo;
        HighlightTopThree = template.HighlightTopThree;
        ColumnSeparators = template.ColumnSeparators;
        RaceColumnsVisibility = template.RaceColumnsVisibility;
        TotalsSeparator = template.TotalsSeparator;
        ShowWeather = template.ShowWeather;
        DiscardStrikethrough = template.DiscardStrikethrough;
        DiscardFaded = template.DiscardFaded;
        ScoreCodes = (template.ScoreCodes ?? new List<SeriesResultsTemplateScoreCode>())
            .Select(c => new TemplateScoreCodeViewModel(c))
            .ToList();
    }

    // The template's own settings; custom field selections are saved separately.
    public SeriesResultsTemplate ToModel() => new()
    {
        Id = Id,
        ClubId = ClubId,
        Name = Name,
        SailNumberVisibility = SailNumberVisibility,
        SailNumberHeader = SailNumberHeader,
        AlternativeSailNumberVisibility = AlternativeSailNumberVisibility,
        AlternativeSailNumberHeader = AlternativeSailNumberHeader,
        CompetitorNameVisibility = CompetitorNameVisibility,
        CompetitorNameHeader = CompetitorNameHeader,
        BoatNameVisibility = BoatNameVisibility,
        BoatNameHeader = BoatNameHeader,
        CompetitorClubVisibility = CompetitorClubVisibility,
        CompetitorClubHeader = CompetitorClubHeader,
        TotalHeader = TotalHeader,
        PreDiscardTotalVisibility = PreDiscardTotalVisibility,
        ParticipationVisibility = ParticipationVisibility,
        ShowClubLogo = ShowClubLogo,
        HighlightTopThree = HighlightTopThree,
        ColumnSeparators = ColumnSeparators,
        RaceColumnsVisibility = RaceColumnsVisibility,
        TotalsSeparator = TotalsSeparator,
        ShowWeather = ShowWeather,
        DiscardStrikethrough = DiscardStrikethrough,
        DiscardFaded = DiscardFaded,
        ScoreCodes = (ScoreCodes ?? new List<TemplateScoreCodeViewModel>())
            .Select(c => c.ToModel())
            .ToList(),
    };

    public Guid Id { get; set; }

    public Guid ClubId { get; set; }

    public string ClubInitials { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; }

    [Display(Name = "Sail Number")]
    public ColumnVisibility SailNumberVisibility { get; set; } = ColumnVisibility.Always;

    // Left empty, the results show the default header in the club's language.
    [Display(Name = "Sail Number Header")]
    [StringLength(50)]
    public string SailNumberHeader { get; set; }

    [Display(Name = "Alternative Sail Number")]
    public ColumnVisibility AlternativeSailNumberVisibility { get; set; } = ColumnVisibility.Hidden;

    [Display(Name = "Alternative Sail Number Header")]
    [StringLength(50)]
    public string AlternativeSailNumberHeader { get; set; }

    [Display(Name = "Competitor Name")]
    public ColumnVisibility CompetitorNameVisibility { get; set; } = ColumnVisibility.Always;

    [Display(Name = "Competitor Name Header")]
    [StringLength(50)]
    public string CompetitorNameHeader { get; set; }

    [Display(Name = "Boat Name")]
    public ColumnVisibility BoatNameVisibility { get; set; } = ColumnVisibility.OnLargerScreens;

    [Display(Name = "Boat Name Header")]
    [StringLength(50)]
    public string BoatNameHeader { get; set; }

    [Display(Name = "Competitor Club")]
    public ColumnVisibility CompetitorClubVisibility { get; set; } = ColumnVisibility.Hidden;

    [Display(Name = "Competitor Club Header")]
    [StringLength(50)]
    public string CompetitorClubHeader { get; set; }

    [Display(Name = "Total Header")]
    [StringLength(50)]
    public string TotalHeader { get; set; }

    [Display(Name = "Pre-Discard Total")]
    public ColumnVisibility PreDiscardTotalVisibility { get; set; } = ColumnVisibility.Hidden;

    [Display(Name = "Participation")]
    public ColumnVisibility ParticipationVisibility { get; set; } = ColumnVisibility.Hidden;

    [Display(Name = "Show Club Logo")]
    public bool ShowClubLogo { get; set; }

    [Display(Name = "Highlight top three places")]
    public bool HighlightTopThree { get; set; } = true;

    [Display(Name = "Column separators")]
    public ResultsColumnSeparator ColumnSeparators { get; set; } = ResultsColumnSeparator.None;

    [Display(Name = "Race columns")]
    public ColumnVisibility RaceColumnsVisibility { get; set; } = ColumnVisibility.Always;

    [Display(Name = "Line between totals and races")]
    public bool TotalsSeparator { get; set; }

    [Display(Name = "Show weather")]
    public bool ShowWeather { get; set; } = true;

    [Display(Name = "Strike through")]
    public bool DiscardStrikethrough { get; set; } = true;

    [Display(Name = "Faded")]
    public bool DiscardFaded { get; set; } = true;

    public IList<TemplateScoreCodeViewModel> ScoreCodes { get; set; } = new List<TemplateScoreCodeViewModel>();

    // The edit form is showing the default settings, not yet saved.
    public bool IsReset { get; set; }

    public IList<TemplateCustomFieldSelectionViewModel> CustomFieldSelections { get; set; } = new List<TemplateCustomFieldSelectionViewModel>();

    public IList<CompetitorFieldDefinition> AvailableCustomFields { get; set; } = new List<CompetitorFieldDefinition>();

    // The club has custom competitor fields turned on; otherwise the form hides that section.
    public bool CustomFieldsEnabled { get; set; }

    // The club has alternative sail numbers turned on; otherwise the form notes they are off.
    public bool AlternativeSailNumbersEnabled { get; set; }

    // Recent club series the preview can show.
    public IList<Series> PreviewSeries { get; set; } = new List<Series>();

    // Used in delete view to show warning
    public bool IsClubDefault { get; set; }
}
