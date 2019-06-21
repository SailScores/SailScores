using SailScores.Web.Services;
using System;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace SailScores.Web.Extensions
{
    public static class DateTimeExtensions
    {
        //values in minutes
        const int RoughlyHalfHour = 20;
        const int RoughlyAnHour = 46;
        const int RoughlyADay = 1425;
        const int RoughlyAWeek = 1440*7;
        const int RoughlyAMonth = 40320;
        const int RoughlyAYear = 1440 * 360;

        public static string ToApproxTimeAgoString(this DateTime? dateTime)
        {
            if (!dateTime.HasValue)
            {
                return String.Empty;
            }
            var minutesAgo = (DateTime.UtcNow - dateTime.Value).TotalMinutes;
            if(minutesAgo < 2)
            {
                return "a minute ago";
            }
            if(minutesAgo < RoughlyHalfHour)
            {
                return "a few minutes ago";
            } else if (minutesAgo < RoughlyAnHour)
            {
                return "a half hour ago";
            } else if (minutesAgo < RoughlyADay)
            {
                return GetHoursString(dateTime.Value);
            }
            else if (minutesAgo < RoughlyAWeek)
            {
                return GetDaysString(dateTime.Value);
            }
            else if (minutesAgo < RoughlyAMonth)
            {
                return GetWeeksString(dateTime.Value);
            }
            else if (minutesAgo < RoughlyAYear)
            {
                return GetMonthsString(dateTime.Value);
            }
            return GetYearsString(dateTime.Value);
        }

        private static string GetHoursString(DateTime dateTime)
        {
            var hoursAgo = (DateTime.UtcNow - dateTime).TotalHours + .25;
            var roundedHours = Convert.ToInt32(Math.Floor(hoursAgo));
            if (roundedHours < 2)
            {
                return "an hour ago";
            }
            return GetNumberString(roundedHours) + " hours ago";
        }

        private static string GetDaysString(DateTime dateTime)
        {
            var daysAgo = (DateTime.UtcNow - dateTime).TotalDays + .25;
            var roundedDays = Convert.ToInt32(Math.Floor(daysAgo));
            if (roundedDays < 2)
            {
                return "a day ago";
            }
            return GetNumberString(roundedDays) + " days ago";
        }

        private static string GetWeeksString(DateTime dateTime)
        {
            var weeksAgo = ((DateTime.UtcNow - dateTime).TotalDays / 7) + .25;
            var roundedWeeks = Convert.ToInt32(Math.Floor(weeksAgo));
            if (roundedWeeks < 2)
            {
                return "a week ago";
            }
            return GetNumberString(roundedWeeks) + " weeks ago";
        }

        private static string GetMonthsString(DateTime dateTime)
        {
            var monthsAgo = ((DateTime.UtcNow - dateTime).TotalDays/30.5) + .10;
            var roundedMonths = Convert.ToInt32(Math.Floor(monthsAgo));
            if (roundedMonths < 2)
            {
                return "a month ago";
            }
            return GetNumberString(roundedMonths) + " months ago";
        }
        private static string GetYearsString(DateTime dateTime)
        {
            var yearsAgo = ((DateTime.UtcNow - dateTime).TotalDays / 365.25) + .25;
            var roundedYears = Convert.ToInt32(Math.Floor(yearsAgo));
            if (roundedYears < 2)
            {
                return "a year ago";
            }
            return GetNumberString(roundedYears) + " years ago";
        }

        private static string GetNumberString(int number)
        {
            switch (number)
            {
                case 0:
                    return "zero";
                case 1:
                    return "one";
                case 2:
                    return "two";
                case 3:
                    return "three";
                case 4:
                    return "four";
                case 5:
                    return "five";
                case 6:
                    return "six";
                case 7:
                    return "seven";
                case 8:
                    return "eight";
                case 9:
                    return "nine";
                default:
                    return number.ToString();

            }
        }
    }
}
