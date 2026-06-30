using System;
using SailScores.Api.Enumerations;

namespace SailScores.Core.Model;

public class SeriesResultsTemplateCustomField
{
    public Guid Id { get; set; }
    public Guid SeriesResultsTemplateId { get; set; }
    public Guid FieldDefinitionId { get; set; }
    public ColumnVisibility Visibility { get; set; } = ColumnVisibility.Always;
    public int DisplayOrder { get; set; }
}
