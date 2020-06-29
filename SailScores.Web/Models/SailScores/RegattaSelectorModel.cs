using System.Collections.Generic;

namespace SailScores.Web.Models.SailScores
{
    public class RegattaSelectorModel
    {
        public List<RegattaSummaryViewModel> Regattas { get; set; }
        public string SelectedRegattaName { get; set; }
    }
}
