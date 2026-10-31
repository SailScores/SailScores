using SailScores.Core.Model;
using System.ComponentModel.DataAnnotations;

namespace SailScores.Web.Models.SailScores;

public class SeriesResultsTemplateListViewModel
{
    public string ClubInitials { get; set; }
    public IList<SeriesResultsTemplate> Templates { get; set; }

    [Display(Name = "Standard Series")]
    public Guid? DefaultSeriesResultsTemplateId { get; set; }

    [Display(Name = "Regattas")]
    public Guid? DefaultRegattaSeriesResultsTemplateId { get; set; }

    // Null uses the standard series default.
    [Display(Name = "Summary Series")]
    public Guid? DefaultSummarySeriesResultsTemplateId { get; set; }

    // Race dates in results headers; blank uses the club's localization.
    [Display(Name = "Date Format")]
    [StringLength(30)]
    public string DefaultDateFormat { get; set; }

    public bool IsClubDefault(Guid templateId) =>
        templateId == DefaultSeriesResultsTemplateId
        || templateId == DefaultRegattaSeriesResultsTemplateId
        || templateId == DefaultSummarySeriesResultsTemplateId;
}
