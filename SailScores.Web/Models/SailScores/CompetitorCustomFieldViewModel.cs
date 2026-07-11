using System;
using System.ComponentModel.DataAnnotations;
using SailScores.Api.Enumerations;
using SailScores.Core.Model;

namespace SailScores.Web.Models.SailScores;

public class CompetitorCustomFieldViewModel
{
    public Guid FieldDefinitionId { get; set; }

    [Display(Name = "Field Name")]
    [StringLength(200)]
    public string Name { get; set; }

    [Display(Name = "Title")]
    [StringLength(100)]
    public string? DisplayHeader { get; set; }

    [Display(Name = "Type")]
    public CustomFieldDataType DataType { get; set; }

    public IList<CompetitorCustomFieldValueViewModel> Values { get; set; } = new List<CompetitorCustomFieldValueViewModel>();

    [Display(Name = "Show Dates")]
    public bool ShowDates { get; set; }
}

public class CompetitorCustomFieldValueViewModel
{
    public Guid Id { get; set; }

    [Display(Name = "Value")]
    [StringLength(500)]
    public string Value { get; set; }

    [Display(Name = "Start Date")]
    public DateTime? EffectiveFrom { get; set; }

    [Display(Name = "End Date")]
    public DateTime? EffectiveTo { get; set; }
}

public class TemplateCustomFieldSelectionViewModel
{
    public Guid FieldDefinitionId { get; set; }

    [Display(Name = "Field Name")]
    [StringLength(200)]
    public string Name { get; set; }

    [Display(Name = "Title")]
    [StringLength(100)]
    public string? DisplayHeader { get; set; }

    [Display(Name = "Type")]
    public CustomFieldDataType DataType { get; set; }

    public ColumnVisibility Visibility { get; set; } = ColumnVisibility.Always;

    [Display(Name = "Order")]
    public int DisplayOrder { get; set; }

    public bool Selected { get; set; }
}
