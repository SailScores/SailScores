namespace SailScores.Database.Entities;

// Exists to support the return of SQL view results. Does
// NOT need to be an actual table in db.
public class AllCompHistogramStats
{
    public string CompetitorName { get; set; }
    public Guid CompetitorId { get; set; }
    public String SailNumber { get; set; }
    public String SeasonName { get; set; }
    public String AggregationType { get; set; }
    public int? Place { get; set; }
    public string Code { get; set; }
    public int? CountOfDistinct { get; set; }
}
