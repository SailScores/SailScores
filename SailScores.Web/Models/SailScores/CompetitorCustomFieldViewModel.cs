using System;
using SailScores.Api.Enumerations;
using SailScores.Core.Model;

namespace SailScores.Web.Models.SailScores;

public class CompetitorCustomFieldViewModel
{
    public Guid FieldDefinitionId { get; set; }

    public string DisplayHeader { get; set; }

    public CustomFieldDataType DataType { get; set; }

    public string Value { get; set; }

    public DateTime? EffectiveFrom { get; set; }

    public DateTime? EffectiveTo { get; set; }

    public bool ShowDates { get; set; }
}

public class TemplateCustomFieldSelectionViewModel
{
    public Guid FieldDefinitionId { get; set; }

    public string DisplayHeader { get; set; }

    public CustomFieldDataType DataType { get; set; }

    public ColumnVisibility Visibility { get; set; } = ColumnVisibility.Always;

    public int DisplayOrder { get; set; }

    public bool Selected { get; set; }
}
