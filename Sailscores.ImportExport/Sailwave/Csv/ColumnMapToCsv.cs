using CsvHelper.Configuration;
using SailScores.ImportExport.Sailwave.Elements.File;

namespace SailScores.ImportExport.Sailwave.Csv
{
    public sealed class ColumnMapToCsv :ClassMap<FileRow>
    {
        public ColumnMapToCsv()
        {
            Map(c => c.Name).Index(0);
            Map(c => c.Value).Index(1);
            Map(c => c.CompetitorOrScoringSystemId).Index(2);
            Map(c => c.RaceId).Index(3);
            Map(c => c.RowType).Ignore();
        }
    }
}
