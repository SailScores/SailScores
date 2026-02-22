namespace SailScores.Web.Models.SailScores;

public abstract class ClubBaseViewModel
{
    public string ClubInitials { get; set; }

    public string ClubName;

    public bool CanEdit { get; set; }

    public bool CanEditSeries { get; set; }

    public string WindSpeedUnits { get; set; }
}
