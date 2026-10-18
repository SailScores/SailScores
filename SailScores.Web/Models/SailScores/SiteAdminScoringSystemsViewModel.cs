namespace SailScores.Web.Models.SailScores;

public class SiteAdminScoringSystemsViewModel
{
    public IList<SiteAdminScoringSystemSummary> Systems { get; set; } = new List<SiteAdminScoringSystemSummary>();
}

public class SiteAdminScoringSystemSummary
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string? ParentSystemName { get; set; }
    public string? CalculatorName { get; set; }
    public bool IsUsedForNewClubs { get; set; }
    public bool IsRestrictedToClubs { get; set; }
    public IList<string> ClubNames { get; set; } = new List<string>();
}
