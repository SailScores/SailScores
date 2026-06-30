using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SailScores.Database.Entities;

public class CompetitorFieldDefinition
{
    public Guid Id { get; set; }

    public Guid ClubId { get; set; }
    public Club Club { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; }

    [Required]
    [StringLength(100)]
    public string DisplayHeader { get; set; }

    public CustomFieldDataType DataType { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public IList<CompetitorFieldValue> Values { get; set; } = new List<CompetitorFieldValue>();

    public IList<SeriesResultsTemplateCustomField> TemplateFields { get; set; } = new List<SeriesResultsTemplateCustomField>();
}

public enum CustomFieldDataType
{
    Text = 0,
    Number = 1
}
