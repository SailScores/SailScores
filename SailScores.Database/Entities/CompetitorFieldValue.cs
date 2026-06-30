using System;
using System.ComponentModel.DataAnnotations;

namespace SailScores.Database.Entities;

public class CompetitorFieldValue
{
    public Guid Id { get; set; }

    public Guid CompetitorId { get; set; }
    public Competitor Competitor { get; set; }

    public Guid FieldDefinitionId { get; set; }
    public CompetitorFieldDefinition FieldDefinition { get; set; }

    [StringLength(500)]
    public string Value { get; set; }

    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }

    [StringLength(1000)]
    public string Notes { get; set; }
}
