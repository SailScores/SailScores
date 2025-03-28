using System.IO;
using SailScores.Core.Model;

namespace SailScores.Web.Services.Interfaces;

public interface ICsvService
{
    Stream GetCsv(Series series);
    Stream GetCsv(IDictionary<string, IEnumerable<Competitor>> competitors);
}