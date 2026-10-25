using System.IO;
using SailScores.Api.Enumerations;
using SailScores.Core.Model;

namespace SailScores.Web.Services.Interfaces;

public interface ICsvService
{
    // The results view sets which standard scoring columns are included.
    Stream GetCsv(Series series, SeriesResultsTemplate template = null);
    Stream GetCsv(IDictionary<string, IEnumerable<Competitor>> competitors);
    Stream GetCsv(
        IDictionary<string, IEnumerable<Competitor>> competitors,
        SeriesResultsTemplate template,
        IDictionary<Guid, CompetitorFieldDefinition> customFieldDefinitions);
}
