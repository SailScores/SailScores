using SailScores.Core.Model;

namespace SailScores.Web.Models.SailScores;

public class RaceSummaryListViewModel
{
    public IEnumerable<RaceSummaryViewModel> Races { get; set; }
    public IEnumerable<Season> Seasons { get; set; }
    public Season CurrentSeason { get; set; }
}