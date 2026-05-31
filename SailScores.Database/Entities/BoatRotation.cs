using System.ComponentModel.DataAnnotations;

namespace SailScores.Database.Entities;

public class BoatRotation
{
    public Guid Id { get; set; }

    [Required]
    public Guid ClubId { get; set; }
    public Club Club { get; set; }

    [Required]
    public Guid CompetitorId { get; set; }
    public Competitor Competitor { get; set; }

    [Required]
    [StringLength(20)]
    public String BoatSailNumber { get; set; }

    public Guid? BoatClassId { get; set; }
    public BoatClass BoatClass { get; set; }

    [Required]
    public DateTime RotationDate { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedUtc { get; set; }

    [StringLength(200)]
    public String CreatedBy { get; set; }
}
