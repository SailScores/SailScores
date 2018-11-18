using System;
using Sailscores.ImportExport.Sailwave.Elements;
using Sailscores.ImportExport.Sailwave.Elements.File;

namespace Sailscores.ImportExport.Sailwave.Parsers
{
    public class ColumnParser
    {

        private static char _separator = '|';

        // rows should come in filtered for a single race.
        public static Column GetColumn(FileRow row)
        {
            if (!row.Name.Equals("column", StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }

            return MakeColumn(row.Value);
        }

        public static Column MakeColumn(string info)
        {
            var elements = info.Split(_separator);
            if (elements.Length != 8)
            {
                throw new ArgumentException(info, "Column info does not appear to be valid when constructing Column");
            }

            var col = new Column
            {
                Type = GetColumnType(elements[0]),
                Name = elements[1],
                Rank = Utilities.GetInt(elements[2]) ?? 0,
                Display = Utilities.GetBool(elements[3]) ?? false,
                Publish = Utilities.GetBool(elements[4]) ?? false,
                Width = Utilities.GetInt(elements[5]) ?? 40,
                Alias = elements[6],
                Format = elements[7],
            };
            return col;
        }

        private static ColumnType GetColumnType(string s)
        {
            int typeNumber = Utilities.GetInt(s) ?? 1;
            if (typeof(ColumnType).IsEnumDefined(typeNumber))
            {
                return (ColumnType) typeNumber;
            }
            return ColumnType.Standard;
        }
        
    }
}