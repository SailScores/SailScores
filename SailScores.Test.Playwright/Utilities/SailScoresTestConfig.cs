

namespace SailScores.Test.Playwright.Utilities;

public class SailScoresTestConfig
{
    public string BaseUrl { get; set; }
    public string TestEmail { get; set; }
    public string TestPassword { get; set; }
    public string TestClubInitials { get; set; }
    public string RaceScorekeeperEmail { get; set; }
    public string SeriesScorekeeperEmail { get; set; }
    public string PermissionTestPassword { get; set; }
    public bool Headless { get; set; } = true;
}
