using System.Collections.Generic;
using System.Threading.Tasks;
using Sailscores.ImportExport.Sailwave.Elements;
using Sailscores.ImportExport.Sailwave.Elements.File;

namespace Sailscores.ImportExport.Sailwave.Writers
{
    public class SeriesWriter
    {
        public static async Task<IEnumerable<FileRow>> WriteSeries(Series series)
        {
            return await WriteDetails(series);
        }

        private static async Task<IEnumerable<FileRow>> WriteDetails(Series series)
        {
            List<FileRow> returnRows = new List<FileRow>();
            SeriesDetailsWriter detailsWriter = new SeriesDetailsWriter();
            returnRows.AddRange(await detailsWriter.GetRows(series.Details));

            ScoringSystemSetWriter scoreWriter = new ScoringSystemSetWriter();
            returnRows.AddRange(await scoreWriter.GetRowsForSet(series));

            returnRows.Add(UserInterfaceInfoWriter.GetRow(series.UserInterface));

            var competitorWriter = new CompetitorSetWriter();
            returnRows.AddRange(await competitorWriter.GetRowsForSet(series));

            RaceSetWriter raceSetWriter = new RaceSetWriter();
            returnRows.AddRange(await raceSetWriter.GetRowsForSet(series));

            RaceResultSetWriter resultWriter = new RaceResultSetWriter();
            returnRows.AddRange(await resultWriter.GetRowsForSet(series));

            ColumnSetWriter columnWriter = new ColumnSetWriter();
            returnRows.AddRange(await columnWriter.GetRowsForSet(series));

            return returnRows;
        }
    }
}
