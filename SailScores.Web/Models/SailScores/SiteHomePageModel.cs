using SailScores.Core.Model.Summary;

namespace SailScores.Web.Models.SailScores;

public class SiteHomePageModel
{
    public ClubSelectorModel ClubSelectorModel { get; set; }

    public RegattaSelectorModel RegattaSelectorModel { get; set; }
    
    public IEnumerable<ClubSummary> RecentlyActiveClubs { get; set; }
    
    public IEnumerable<ClubSummary> AllVisibleClubs { get; set; }

}