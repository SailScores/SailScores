namespace SailScores.Database.Entities;

public class CompetitorChange
{
    public Guid Id { get; set; }
    public Guid CompetitorId { get; set; }

    public ChangeType ChangeType { get; set; }
    public Guid ChangeTypeId { get; set; }
    [StringLength(200)]
    public String ChangedBy { get; set; }
    [Column(TypeName = "datetime2")]
    public DateTime ChangeTimeStamp { get; set; }

    public string NewValue { get; set; }

    [StringLength(250)]
    public string Summary { get; set; }

}