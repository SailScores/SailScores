using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SailScores.Core.Utility;

internal static class DateTimeExtensions
{
    public static DateOnly ToDateOnly(this DateTime datetime)
        => DateOnly.FromDateTime(datetime);

    public static DateTime ToDateTime(this DateOnly dateOnly)
        => dateOnly.ToDateTime();
}
