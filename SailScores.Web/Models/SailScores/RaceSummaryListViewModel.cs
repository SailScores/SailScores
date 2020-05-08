using SailScores.Core.Model;
using System.Collections.Generic;

namespace SailScores.Web.Models.SailScores
{
    public class RaceSummaryListViewModel
    {
        public IEnumerable<RaceSummaryViewModel> Races { get; set; }
        public IEnumerable<Season> Seasons { get; set; }
        public Season CurrentSeason { get; set; }
    }
}
