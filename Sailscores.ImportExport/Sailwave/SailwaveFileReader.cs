using System.IO;
using SailScores.ImportExport.Sailwave.Elements;
using SailScores.ImportExport.Sailwave.Interfaces;
using SailScores.ImportExport.Sailwave.Parsers;

namespace SailScores.ImportExport.Sailwave
{
    public class SailwaveFileReader : IFileReader
    {
        public Series Series { get; set; }

        public SailwaveFileReader(string fileName)
        {
            using (var reader = new StreamReader(fileName))
            {
                Series = SeriesParser.GetSeries(reader);
            }
        }
    }
}
