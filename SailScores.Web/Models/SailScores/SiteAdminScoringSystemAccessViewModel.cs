namespace SailScores.Web.Models.SailScores;

public class SiteAdminScoringSystemAccessViewModel
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? ParentSystemName { get; set; }
    public string? CalculatorName { get; set; }
    public bool IsUsedForNewClubs { get; set; }

    public bool IsRestrictedToClubs { get; set; }
    public IList<Guid> SelectedClubIds { get; set; } = new List<Guid>();

    public IList<SiteAdminClubOption> ClubOptions { get; set; } = new List<SiteAdminClubOption>();
}

public class SiteAdminClubOption
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Initials { get; set; }
    public bool IsHidden { get; set; }
}
