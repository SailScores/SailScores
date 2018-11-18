using System.IO;
using System.Threading.Tasks;
using CsvHelper;
using Sailscores.ImportExport.Sailwave.Csv;
using Sailscores.ImportExport.Sailwave.Elements;
using Sailscores.ImportExport.Sailwave.Interfaces;

namespace Sailscores.ImportExport.Sailwave
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
                var csv = new CsvWriter(writer);
                csv.Configuration.RegisterClassMap<ColumnMapToCsv>();
                csv.Configuration.HasHeaderRecord = false;
                csv.Configuration.QuoteAllFields = true;
                csv.WriteRecords(await Writers.SeriesWriter.WriteSeries(series));
            }
        }
    }
}
