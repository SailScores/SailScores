namespace SailScores.Database.Entities;

public class ChangeType
{
    public Guid Id { get; set; }
    [StringLength(200)]
    public String Name { get; set; }
    [StringLength(2000)]
    public String Description { get; set; }
}