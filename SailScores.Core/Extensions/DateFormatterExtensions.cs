using System;
using System.Globalization;
using System.Threading;

namespace SailScores.Core.Extensions
{
    public static class DateFormatterExtensions
    {
        public static string ToShortString(this DateTime? date)
        {
            return date?.ToString("ddd", CultureInfo.CurrentCulture) + 
                ", " + date?.ToString("M", CultureInfo.CurrentCulture);
        }
        public static string ToSuperShortString(this DateTime? date)
        {

            string pattern = Thread.CurrentThread.CurrentUICulture.DateTimeFormat.ShortDatePattern;
            pattern = GetShortPatternWithoutYear(pattern);
            return date?.ToString(pattern, CultureInfo.CurrentCulture);
        }

        private static string GetShortPatternWithoutYear(string pattern)
        {
            if (pattern.EndsWith("yyyy", StringComparison.InvariantCulture))
                pattern = pattern.Substring(0, pattern.Length - 5);
            else if (pattern.StartsWith("yyyy", StringComparison.InvariantCulture))
                pattern = pattern.Substring(5);
            // some even end with yyyy.
            else if (pattern.EndsWith("yyyy.", StringComparison.InvariantCulture))
                pattern = pattern.Substring(0, pattern.Length - 5);
            else if (pattern.EndsWith("yy", StringComparison.InvariantCulture))
                pattern = pattern.Substring(0, pattern.Length - 3);
            // and some seldom with yy.
            else if (pattern.EndsWith("yy.", StringComparison.InvariantCulture))
                pattern = pattern.Substring(0, pattern.Length - 3);
            // bul
            else if (pattern.EndsWith("yyyy 'г.'", StringComparison.InvariantCulture))
                pattern = pattern.Substring(0, pattern.Length - 9);
            // tuk
            else if (pattern.EndsWith(".yy 'ý.'", StringComparison.InvariantCulture))
                pattern = pattern.Substring(0, pattern.Length - 8);
            return pattern;
        }
    }
}
