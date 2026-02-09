namespace SailScores.Web.Authorization;

/// <summary>
/// Constants for authorization policy names used throughout the application.
/// These should match the policy names registered in Startup.cs.
/// </summary>
public static class AuthorizationPolicies
{
    /// <summary>
    /// Policy for Club Administrator permission level.
    /// Required for creating, editing, and deleting club resources.
    /// </summary>
    public const string ClubAdmin = "ClubAdmin";

    /// <summary>
    /// Policy for Series Scorekeeper permission level.
    /// Required for editing series and races.
    /// </summary>
    public const string SeriesScorekeeper = "SeriesScorekeeper";

    /// <summary>
    /// Policy for Race Scorekeeper permission level.
    /// Required for editing race results.
    /// </summary>
    public const string RaceScorekeeper = "RaceScorekeeper";
}
