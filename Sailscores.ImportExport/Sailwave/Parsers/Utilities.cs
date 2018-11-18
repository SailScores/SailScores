using System;

namespace Sailscores.ImportExport.Sailwave.Parsers
{
    static class Utilities
    {
        public static bool? GetBool(string input)
        {
            if (input == null)
            {
                return null;
            }
            if (input.StartsWith("y", StringComparison.InvariantCultureIgnoreCase)
                || input.StartsWith("t", StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }
            if (input.StartsWith("n", StringComparison.InvariantCultureIgnoreCase)
                || input.StartsWith("f", StringComparison.InvariantCultureIgnoreCase))
            {
                return false;
            }

            if (Int32.TryParse(input, out int i))
            {
                return i != 0;
            }

            return null;

        }

        public static int? GetInt(string input)
        {
            if (Int32.TryParse(input, out var i))
            {
                return i;
            }

            return null;
        }
        
        internal static decimal? GetDecimal(string input)
        {
            if (Decimal.TryParse(input, out decimal d))
            {
                return d;
            }

            return null;
        }
    }
}
