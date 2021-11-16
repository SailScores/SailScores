namespace SailScores.Database.Entities;

// Exists to support the return of SQL view results. Does
// NOT need to be an actual table in db.
public class SiteStats
{
    public string ClubName { get; set; }
    public string ClubInitials { get; set; }
    public DateTime? LastRaceDate { get; set; }
    public DateTime? LastRaceUpdate { get; set; }

    public int? RaceCount { get; set; }
    public int? ScoreCount { get; set; }
}