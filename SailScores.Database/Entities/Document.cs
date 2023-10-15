namespace SailScores.Database.Entities;

public class Document
{
    public Guid Id { get; set; }
    public Guid ClubId { get; set; }
    public Guid? RegattaId { get; set; }

    [StringLength(200)]
    public String Name { get; set; }

    [StringLength(128)]
    public String ContentType { get; set; }


    public byte[] FileContents { get; set; }
    
    [Column("CreatedDateUtc")]
    public DateTime CreatedDate { get; set; }

    [Column("CreatedDateLocal")]
    public DateTime CreatedLocalDate { get; set; }

    [StringLength(128)]
    public String CreatedBy { get; set; }
    public Guid? PreviousVersion { get; set; }

}