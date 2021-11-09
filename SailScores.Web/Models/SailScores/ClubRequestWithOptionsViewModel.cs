namespace SailScores.Web.Models.SailScores;

public class ClubRequestWithOptionsViewModel : ClubRequestViewModel
{
    public IList<ClubSummaryViewModel> ClubOptions { get; set; }
}