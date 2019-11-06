using SailScores.Core.Model;
using System.IO;

namespace SailScores.Web.Services
{
    public interface ICsvService
    {
        Stream GetCsv(Series series);
    }
}