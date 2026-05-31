using System;
using System.ComponentModel.DataAnnotations;

namespace SailScores.Core.Model;

public class BoatRotation
{
    public Guid Id { get; set; }
    public Guid ClubId { get; set; }
    public Guid CompetitorId { get; set; }

    [StringLength(20)]
    public String BoatSailNumber { get; set; }

    public Guid? BoatClassId { get; set; }
    public DateTime RotationDate { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedUtc { get; set; }
    public String CreatedBy { get; set; }
}
