using System;
using System.ComponentModel.DataAnnotations;

namespace SailScores.Core.Model;

public class ClassHandicap
{
    public Guid Id { get; set; }

    public Guid BoatClassId { get; set; }
    public BoatClass BoatClass { get; set; }

    public Guid HandicapSystemId { get; set; }
    public HandicapSystem HandicapSystem { get; set; }

    public decimal Value { get; set; }

    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }

    [StringLength(1000)]
    public string Notes { get; set; }
}
