namespace SailScores.Database.Entities;

[Table("BoatClasses")]
public class BoatClass
{
    public Guid Id { get; set; }
    public Guid ClubId { get; set; }

    [StringLength(200)]
    public String Name { get; set; }

    [StringLength(2000)]
    public String Description { get; set; }

}