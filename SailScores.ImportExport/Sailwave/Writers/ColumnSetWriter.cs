using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using SailScores.ImportExport.Sailwave.Elements;
using SailScores.ImportExport.Sailwave.Elements.File;

namespace SailScores.ImportExport.Sailwave.Writers
{
    public class ColumnSetWriter : GenericSetWriter<Column>
    {
        public override async Task<IEnumerable<FileRow>> GetRows(Column thing)
        {
            var returnRow = new FileRow
            {
                Name = RowNames.Column,
                Value = GetColumnValue(thing)
            };

            return new List<FileRow>{returnRow};
        }
        
        private static string GetColumnValue(Column column)
        {
            // 8 pipe separated values
            //"1|AltSailNo|15|No|No|40||"

            var strings = new List<string>();

            strings.Add(((int)column.Type).ToString(CultureInfo.InvariantCulture));  // 0
            strings.Add(column.Name);
            strings.Add(column.Rank.ToString(CultureInfo.InvariantCulture));
            strings.Add(Utilities.BoolToYesNo(column.Display));
            strings.Add(Utilities.BoolToYesNo(column.Publish)); //4
            strings.Add(column.Width.ToString(CultureInfo.InvariantCulture));
            strings.Add(column.Alias);
            strings.Add(column.Format);

            return String.Join("|", strings);
        }

        protected override IEnumerable<Column> GetIndividualItems(Series series)
        {
            return series.Columns;
        }

        protected override int? GetRaceId(Column thing)
        {
            return null;
        }

        protected override int? GetCompetitorOrScoreId(Column thing)
        {
            return null;
        }
    }
}
