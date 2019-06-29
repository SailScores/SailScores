using SailScores.Core.Model;
using SailScores.Core.Scoring;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using SailScores.Web.Extensions;

namespace SailScores.Test.Unit
{


    public class DateTimeExtensionsTests
    {
        [Theory]
        [InlineData( 1, "a minute ago" )]
        [InlineData( 2, "2 minutes ago")]
        [InlineData( 3, "3 minutes ago")]
        [InlineData( 4, "4 minutes ago")]
        [InlineData( 19, "19 minutes ago")]
        [InlineData( 20, "20 minutes ago")]
        [InlineData( 22, "22 minutes ago")]
        [InlineData( 46, "46 minutes ago" )]
        [InlineData( 44, "44 minutes ago")]
        [InlineData( 90, "an hour ago")]
        [InlineData( 130, "2 hours ago")]
        [InlineData( 200, "3 hours ago")]
        [InlineData( 630, "10 hours ago")]
        [InlineData( 1380, "23 hours ago")]
        [InlineData( 1500, "yesterday")]
        [InlineData( 3000, "2 days ago")]
        [InlineData( 14400, "10 days ago")]
        [InlineData(1440 * 15, "15 days ago")]
        [InlineData(1440 * 30, "one month ago")]
        [InlineData(1440 * 65, "2 months ago")]
        [InlineData(1440 * 365, "one year ago")]
        [InlineData(1440 * 730, "2 years ago")]
        [InlineData(1440 * 3650, "10 years ago")]
        public void HoursAgo_ReturnsString(int minutesAgo, string output)
        {
            var timeAgo = DateTime.UtcNow.AddMinutes(-1 * minutesAgo);
            var result = ((DateTime?)timeAgo).ToApproxTimeAgoString();
                
            Assert.Equal(output, result);

        }
    }
}
