namespace SailScores.Database.Entities;

public class ChangeType
{
    public Guid Id { get; set; }
    [StringLength(200)]
    public String Name { get; set; }
    [StringLength(2000)]
    public String Description { get; set; }

    public static Guid CreatedId => new Guid("b6c92ed8-1d15-4a1a-977f-6e59bd0160c7");
    public static Guid DeletedId => new("ee49c9c4-d556-4cab-b740-a3baad9c73c9");
    public static Guid ActivatedId => new("153a8b2a-accf-404c-bb39-61db55f5ee1e");
    public static Guid DeactivatedId => new("87533c82-936d-44bb-8055-9292046a7b9e");
}