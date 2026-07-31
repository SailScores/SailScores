using System;

namespace SailScores.Core.Model;

public class CompetitorFieldValue
{
    public Guid Id { get; set; }
    public Guid CompetitorId { get; set; }
    public Guid FieldDefinitionId { get; set; }
    public string Value { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string Notes { get; set; }
}
