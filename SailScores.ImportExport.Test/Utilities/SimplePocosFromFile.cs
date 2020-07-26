using System.IO;
using SailScores.ImportExport.Sailwave.Elements;
using SailScores.ImportExport.Sailwave.Parsers;

namespace SailScores.ImportExport.Sailwave.Tests.Utilities
{
    public class SimplePocosFromFile
    {

        public static Series GetSeries(string filePath)
        {
            using var reader = new StreamReader(filePath);
            return SeriesParser.GetSeries(reader);
        }
    }
}
