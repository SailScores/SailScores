using SailScores.ImportExport.Sailwave.Elements;
using SailScores.ImportExport.Sailwave.Parsers;

namespace SailScores.ImportExport.Sailwave.Test.Utilities;

public class SimplePocos
{

    public static Series GetSeries()
    {
        return SeriesParser.GetSeries(SimpleFile.GetStream());
    }
}
