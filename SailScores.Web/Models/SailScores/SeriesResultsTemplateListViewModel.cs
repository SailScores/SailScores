using SailScores.Core.Model;

namespace SailScores.Web.Models.SailScores;

public class SeriesResultsTemplateListViewModel
{
    public string ClubInitials { get; set; }
    public IList<SeriesResultsTemplate> Templates { get; set; }
}
