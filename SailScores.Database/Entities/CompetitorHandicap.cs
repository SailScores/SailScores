namespace SailScores.Database.Entities;

public class CompetitorHandicap
{
    public Guid Id { get; set; }

    public Guid CompetitorId { get; set; }
    public Competitor Competitor { get; set; }

    public Guid HandicapSystemId { get; set; }
    public HandicapSystem HandicapSystem { get; set; }

    // Interpretation depends on system type:
    //   PhrfToD / PhrfToT: seconds-per-mile rating (negative = faster than scratch)
    //   Portsmouth: yardstick number (baseline 1000)
    //   TimeOnTime: time correction coefficient
    public decimal Value { get; set; }

    // Effective date range. Constraints (enforced by filtered unique indexes):
    //   At most one row per (CompetitorId, HandicapSystemId) may have EffectiveFrom IS NULL.
    //   At most one row per (CompetitorId, HandicapSystemId) may have EffectiveTo IS NULL.
    //   A single row may have both null (competitor has exactly one rating, valid forever).
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }

    [StringLength(1000)]
    public string Notes { get; set; }
}
