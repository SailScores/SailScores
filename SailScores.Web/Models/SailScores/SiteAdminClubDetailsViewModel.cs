using SailScores.Core.Model;

namespace SailScores.Web.Models.SailScores;

public class SiteAdminClubDetailsViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Initials { get; set; }
    public bool IsHidden { get; set; }
    public IList<Series> Series { get; set; }
    public DateTime? LatestSeriesUpdate { get; set; }
    public DateTime? LatestRaceDate { get; set; }
    public int RaceCount { get; set; }
}
