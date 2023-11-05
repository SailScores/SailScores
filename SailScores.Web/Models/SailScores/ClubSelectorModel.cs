using SailScores.Core.Model;
using SailScores.Core.Model.Summary;

namespace SailScores.Web.Models.SailScores;

public class ClubSelectorModel
{
    public IEnumerable<ClubSummary> Clubs { get; set; }

    public string SelectedClubInitials { get; set; }
}