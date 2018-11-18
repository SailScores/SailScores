using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Sailscores.ImportExport.Sailwave.Elements;
using Sailscores.ImportExport.Sailwave.Parsers;

namespace Sailscores.ImportExport.Sailwave.Tests.Utilities
{
    public class SimplePocosFromFile
    {

        public static Series GetSeries(string filePath)
        {
            using (var reader = new StreamReader(filePath))
            {
                return SeriesParser.GetSeries(reader);
            }
        }
    }
}
