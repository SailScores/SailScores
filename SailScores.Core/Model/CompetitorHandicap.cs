using System;
using System.ComponentModel.DataAnnotations;

namespace SailScores.Core.Model;

public class CompetitorHandicap
{
    public Guid Id { get; set; }

    public Guid CompetitorId { get; set; }
    public Competitor Competitor { get; set; }

    public Guid HandicapSystemId { get; set; }
    public HandicapSystem HandicapSystem { get; set; }

    public decimal Value { get; set; }

    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }

    [StringLength(1000)]
    public string Notes { get; set; }
}
