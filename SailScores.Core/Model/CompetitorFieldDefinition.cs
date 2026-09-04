using System;
using System.ComponentModel.DataAnnotations;

namespace SailScores.Core.Model;

public class CompetitorFieldDefinition
{
    public Guid Id { get; set; }
    public Guid ClubId { get; set; }

    [Display(Name = "Field name")]
    public string Name { get; set; }

    [Display(Name = "Display label")]
    public string? DisplayHeader { get; set; }

    [Display(Name = "Value type")]
    public CustomFieldDataType DataType { get; set; }

    [Display(Name = "Display order")]
    public int DisplayOrder { get; set; }

    [Display(Name = "Currently Enabled")]
    public bool IsActive { get; set; }

    [Display(Name = "Highly visible")]
    public bool? HighlyVisible { get; set; } = false;
}

public enum CustomFieldDataType
{
    Text = 0,
    Number = 1
}
