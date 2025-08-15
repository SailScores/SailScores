namespace SailScores.Database.Entities;

public class SeriesToSeriesLink
{
    [ForeignKey(nameof(ParentSeries))]
    public Guid ParentSeriesId { get; set; }
    public Series ParentSeries { get; set; }
    
    [ForeignKey(nameof(ChildSeries))]
    public Guid ChildSeriesId { get; set; }
    public Series ChildSeries { get; set; }
}