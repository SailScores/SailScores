using System.ComponentModel.DataAnnotations;
using SailScores.Api.Enumerations;
using SailScores.Core.Model;

namespace SailScores.Web.Models.SailScores;

// One score code row on the results view edit page.
public class TemplateScoreCodeViewModel
{
    public TemplateScoreCodeViewModel()
    {
    }

    public TemplateScoreCodeViewModel(SeriesResultsTemplateScoreCode scoreCode)
    {
        Code = scoreCode.Code;
        Display = scoreCode.Display;
        Faded = scoreCode.Faded;
    }

    [Required]
    [StringLength(20)]
    public string Code { get; set; }

    public string Description { get; set; }

    public ScoreCodeDisplay Display { get; set; } = ScoreCodeDisplay.CodeAndPoints;

    public bool Faded { get; set; }

    public SeriesResultsTemplateScoreCode ToModel() => new()
    {
        Code = Code,
        Display = Display,
        Faded = Faded
    };
}
