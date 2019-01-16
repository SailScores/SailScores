using System.Collections.Generic;
using System.Linq;
using SailScores.ImportExport.Sailwave.Elements;
using SailScores.ImportExport.Sailwave.Elements.File;

namespace SailScores.ImportExport.Sailwave.Parsers
{
    public class ColumnSetParser
    {
        public static List<Column> GetColumns(IEnumerable<FileRow> rows)
        {
            var returnList = new List<Column>();
            var columnDefinitions = rows
                .Where(r => r.RowType == RowType.Column);
            foreach (var columnDefRow in columnDefinitions)
            {
                var column = ColumnParser.GetColumn(columnDefRow);

                returnList.Add(column);
            }

            return returnList;
        }
    }
}
