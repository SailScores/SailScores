using System.IO;
using Sailscores.ImportExport.Sailwave.Elements;
using Sailscores.ImportExport.Sailwave.Interfaces;
using Sailscores.ImportExport.Sailwave.Parsers;

namespace Sailscores.ImportExport.Sailwave
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
