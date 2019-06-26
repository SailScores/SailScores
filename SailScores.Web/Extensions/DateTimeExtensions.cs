using Humanizer;
using SailScores.Web.Services;
using System;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace SailScores.Web.Extensions
{
    public static class DateTimeExtensions
    {


        public static string ToApproxTimeAgoString(this DateTime? dateTime)
        {
            if (!dateTime.HasValue)
            {
                return String.Empty;
            }
            return dateTime.Value.Humanize();
        }

    }
}
