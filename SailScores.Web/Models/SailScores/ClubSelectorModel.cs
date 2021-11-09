using SailScores.Core.Model;

namespace SailScores.Web.Models.SailScores;

public class ClubSelectorModel
{
    public List<Club> Clubs { get; set; }

    public string SelectedClubInitials { get; set; }
}