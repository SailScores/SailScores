using SailScores.Core.Model;
using System.ComponentModel.DataAnnotations;

namespace SailScores.Web.Models.SailScores;

public class SeriesResultsTemplateListViewModel
{
    public string ClubInitials { get; set; }
    public IList<SeriesResultsTemplate> Templates { get; set; }

    [Display(Name = "Default View for Standard Series")]
    public Guid? DefaultSeriesResultsTemplateId { get; set; }

    [Display(Name = "Default View for Regattas")]
    public Guid? DefaultRegattaSeriesResultsTemplateId { get; set; }

    // Null uses the standard series default.
    [Display(Name = "Default View for Summary Series")]
    public Guid? DefaultSummarySeriesResultsTemplateId { get; set; }

    public bool IsClubDefault(Guid templateId) =>
        templateId == DefaultSeriesResultsTemplateId
        || templateId == DefaultRegattaSeriesResultsTemplateId
        || templateId == DefaultSummarySeriesResultsTemplateId;
}
