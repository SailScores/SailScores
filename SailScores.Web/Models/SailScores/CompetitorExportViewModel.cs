using System;
using System.Collections.Generic;
using SailScores.Core.Model;

namespace SailScores.Web.Models.SailScores;

public class CompetitorExportViewModel
{
    public IDictionary<string, IEnumerable<Competitor>> Competitors { get; set; }
    public SeriesResultsTemplate Template { get; set; }
    public IDictionary<Guid, CompetitorFieldDefinition> CustomFieldDefinitions { get; set; }
}
