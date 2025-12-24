namespace SailScores.Database.Entities;

// Exists to support the return of SQL view results. Does
// NOT need to be an actual table in db.
public class AllCompHistogramFields
{
    public string Code { get; set; }
    public int? MaxPlace { get; set; }
}
