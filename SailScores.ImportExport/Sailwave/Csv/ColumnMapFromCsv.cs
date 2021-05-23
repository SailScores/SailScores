using System;
using CsvHelper;
using CsvHelper.Configuration;
using SailScores.ImportExport.Sailwave.Elements.File;

namespace SailScores.ImportExport.Sailwave.Csv
{
    public sealed class ColumnMapFromCsv : ClassMap<FileRow>
    {
        public ColumnMapFromCsv()
        {
            Map(c => c.Name).Index(0);
            Map(c => c.Value).Index(1);
            Map(c => c.CompetitorOrScoringSystemId).Index(2);
            Map(c => c.RaceId).Index(3);
            Map(c => c.RowType)
                .Convert(row => GetRowType(row.Row));
        }

        private static RowType GetRowType(IReaderRow row)
        {
            var rowName = row.GetField<string>(0);
            int? scoreSysOrCompId = row.GetField<int?>(2);
            int? raceId = row.GetField<int?>(3);

            RowType returnValue = GetRowTypeFromName(rowName);
            if (returnValue == RowType.Unknown)
            {
                if (raceId.HasValue && scoreSysOrCompId.HasValue)
                {
                    returnValue = RowType.RaceResult;
                }
            }
            return returnValue;
        }

        private static RowType GetRowTypeFromName(string rowName)
        {
            if (String.IsNullOrWhiteSpace(rowName))
            {
                return RowType.Unknown;
            }
            if (rowName.ToUpperInvariant() == "UI")
            {
                return RowType.UserInterface;
            }
            if (rowName.Length < 3)
            {
                return RowType.Unknown;
            }
            string firstThree = rowName.Substring(0, 3).ToUpperInvariant();
            switch (firstThree)
            {
                case "SER":
                    return RowType.Series;
                case "SCR":
                    return RowType.ScoringSystem;
                case "COM":
                    return RowType.Competitor;
                case "RAC":
                    return RowType.Race;
                case "COL":
                    return RowType.Column;
                default:
                    return RowType.Unknown;
            }
        }
    }
}
