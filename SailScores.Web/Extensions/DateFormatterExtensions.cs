using System;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace SailScores.Web.Extensions
{
    public static class DateFormatterExtensions
    {
        public static String ToShortString(this DateTime? date)
        {
            return date?.ToString("ddd") + ", " + date?.ToString("M");
        }
        public static String ToSuperShortString(this DateTime? date)
        {
            //TODO: currently only US-English friendly
            return date?.ToString("M-d");
        }
    }
}
