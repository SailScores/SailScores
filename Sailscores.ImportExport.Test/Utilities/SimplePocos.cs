using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SailScores.ImportExport.Sailwave.Elements;
using SailScores.ImportExport.Sailwave.Parsers;

namespace SailScores.ImportExport.Sailwave.Tests.Utilities
{
    public class SimplePocos
    {

        public static Series GetSeries()
        {
            return SeriesParser.GetSeries(SimpleFile.GetStream());
        }
    }
}
