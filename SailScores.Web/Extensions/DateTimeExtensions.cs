using Humanizer;
using System;

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
