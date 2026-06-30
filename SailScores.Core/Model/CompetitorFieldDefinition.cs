using System;

namespace SailScores.Core.Model;

public class CompetitorFieldDefinition
{
    public Guid Id { get; set; }
    public Guid ClubId { get; set; }
    public string Name { get; set; }
    public string DisplayHeader { get; set; }
    public CustomFieldDataType DataType { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
}

public enum CustomFieldDataType
{
    Text = 0,
    Number = 1
}
