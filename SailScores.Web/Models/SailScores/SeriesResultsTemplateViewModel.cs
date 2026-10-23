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
        CompetitorNameVisibility = template.CompetitorNameVisibility;
        CompetitorNameHeader = template.CompetitorNameHeader;
        BoatNameVisibility = template.BoatNameVisibility;
        BoatNameHeader = template.BoatNameHeader;
        CompetitorClubVisibility = template.CompetitorClubVisibility;
        ShowPreDiscardTotal = template.ShowPreDiscardTotal;
        ShowClubLogo = template.ShowClubLogo;
        HighlightTopThree = template.HighlightTopThree;
        ColumnSeparators = template.ColumnSeparators;
        RaceColumnsVisibility = template.RaceColumnsVisibility;
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
        CompetitorNameVisibility = CompetitorNameVisibility,
        CompetitorNameHeader = CompetitorNameHeader,
        BoatNameVisibility = BoatNameVisibility,
        BoatNameHeader = BoatNameHeader,
        CompetitorClubVisibility = CompetitorClubVisibility,
        ShowPreDiscardTotal = ShowPreDiscardTotal,
        ShowClubLogo = ShowClubLogo,
        HighlightTopThree = HighlightTopThree,
        ColumnSeparators = ColumnSeparators,
        RaceColumnsVisibility = RaceColumnsVisibility,
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

    [Display(Name = "Competitor Name")]
    public ColumnVisibility CompetitorNameVisibility { get; set; } = ColumnVisibility.Always;

    [Display(Name = "Competitor Name Header")]
    [StringLength(50)]
    public string CompetitorNameHeader { get; set; } = "Helm";

    [Display(Name = "Boat Name")]
    public ColumnVisibility BoatNameVisibility { get; set; } = ColumnVisibility.OnLargerScreens;

    [Display(Name = "Boat Name Header")]
    [StringLength(50)]
    public string BoatNameHeader { get; set; } = "Boat";

    [Display(Name = "Competitor Club")]
    public ColumnVisibility CompetitorClubVisibility { get; set; } = ColumnVisibility.Hidden;

    [Display(Name = "Show Pre-Discard Total")]
    public bool ShowPreDiscardTotal { get; set; } = false;

    [Display(Name = "Show Club Logo")]
    public bool ShowClubLogo { get; set; }

    [Display(Name = "Highlight top three places")]
    public bool HighlightTopThree { get; set; } = true;

    [Display(Name = "Column separators")]
    public ResultsColumnSeparator ColumnSeparators { get; set; } = ResultsColumnSeparator.None;

    [Display(Name = "Race columns")]
    public ColumnVisibility RaceColumnsVisibility { get; set; } = ColumnVisibility.Always;

    public IList<TemplateScoreCodeViewModel> ScoreCodes { get; set; } = new List<TemplateScoreCodeViewModel>();

    public IList<TemplateCustomFieldSelectionViewModel> CustomFieldSelections { get; set; } = new List<TemplateCustomFieldSelectionViewModel>();

    public IList<CompetitorFieldDefinition> AvailableCustomFields { get; set; } = new List<CompetitorFieldDefinition>();

    // Used in delete view to show warning
    public bool IsClubDefault { get; set; }
}
