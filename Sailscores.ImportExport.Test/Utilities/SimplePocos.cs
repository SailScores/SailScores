using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Sailscores.ImportExport.Sailwave.Elements;
using Sailscores.ImportExport.Sailwave.Parsers;

namespace Sailscores.ImportExport.Sailwave.Tests.Utilities
{
    public class SimplePocos
    {

        public static Series GetSeries()
        {
            return SeriesParser.GetSeries(SimpleFile.GetStream());
        }
    }
}
