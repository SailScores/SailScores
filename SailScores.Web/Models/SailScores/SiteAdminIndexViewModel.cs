namespace SailScores.Web.Models.SailScores;

public class SiteAdminIndexViewModel
{
    public IList<SiteAdminClubSummary> Clubs { get; set; } = new List<SiteAdminClubSummary>();
    public string? UpdateSortParm { get; set; }
    public string? NameSortParm { get; set; }
    public string? RaceSortParm { get; set; }
    public string? CurrentSort { get; set; }
}

public class SiteAdminClubSummary
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Initials { get; set; }
    public bool IsHidden { get; set; }
    public DateTime? LatestSeriesUpdate { get; set; }
    public DateTime? LatestRaceDate { get; set; }
}
