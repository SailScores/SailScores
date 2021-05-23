using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using SailScores.ImportExport.Sailwave.Csv;
using SailScores.ImportExport.Sailwave.Elements;
using SailScores.ImportExport.Sailwave.Interfaces;

namespace SailScores.ImportExport.Sailwave
{
    public class SailwaveFileWriter : IFileWriter
    {
        public SailwaveFileWriter()
        {
        }

        public async Task WriteAsync(Series series, string filePath)
        {
            using (var writer = new StreamWriter(filePath))
            {
                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = false,
                    ShouldQuote = (s) => true
                };
                var csv = new CsvWriter(writer, config);
                csv.Context.RegisterClassMap<ColumnMapToCsv>();
                csv.WriteRecords(await Writers.SeriesWriter.WriteSeries(series));
            }
        }
    }
}
