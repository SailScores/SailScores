namespace SailScores.Database.Entities;

public class ClubRequest
{
    public Guid Id { get; set; }

    [Required]
    [StringLength(200)]
    public String ClubName { get; set; }
    [StringLength(10)]
    public String ClubInitials { get; set; }

    [StringLength(1000)]
    public String ClubLocation { get; set; }
    [StringLength(200)]
    public String ClubWebsite { get; set; }
    [StringLength(200)]
    public String ContactName { get; set; }
    [StringLength(200)]
    public String ContactEmail { get; set; }

    public bool? Hide { get; set; }
    public bool? ForTesting { get; set; }
    public String Classes { get; set; }

    public String TypicalDiscardRules { get; set; }
    public String Comments { get; set; }

    public DateTime? RequestSubmitted { get; set; }
    public DateTime? RequestApproved { get; set; }
    [StringLength(2000)]
    public string AdminNotes { get; set; }

    public Guid? TestClubId { get; set; }
    public Guid? VisibleClubId { get; set; }

    public bool? Complete { get; set; }

}