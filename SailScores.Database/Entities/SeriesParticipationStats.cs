namespace SailScores.Database.Entities;

// Exists to support the return of SQL view results. Does
// NOT need to be an actual table in db.
public class SeriesParticipationStats
{
    public string SeasonName { get; set; }
    public DateTime? SeasonStart { get; set; }

    public string SeriesName { get; set; }
    public string SeriesType { get; set; }

    // Number of distinct classes in the series
    public int? ClassCount { get; set; }

    public string ClassName { get; set; }

    public int? RaceCount { get; set; }
    public int? CompetitorsStarted { get; set; }
    public int? DistinctCompetitorsStarted { get; set; }
    public decimal? AverageCompetitorsPerRace { get; set; }
    public int? DistinctDaysRaced { get; set; }
    public DateTime? LastRace { get; set; }
    public DateTime? FirstRace { get; set; }
}
