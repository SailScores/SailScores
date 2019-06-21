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
        [InlineData( 0, "a minute ago" )]
        [InlineData( 1, "a minute ago" )]
        [InlineData( 2, "a few minutes ago")]
        [InlineData( 3, "a few minutes ago")]
        [InlineData( 4, "a few minutes ago")]
        [InlineData( 19, "a few minutes ago")]
        [InlineData( 20, "a half hour ago")]
        [InlineData( 21, "a half hour ago")]
        [InlineData( 22, "a half hour ago")]
        [InlineData( 46, "an hour ago" )]
        [InlineData( 44, "a half hour ago")]
        [InlineData( 90, "an hour ago")]
        [InlineData( 130, "two hours ago")]
        [InlineData( 200, "three hours ago")]
        [InlineData( 630, "10 hours ago")]
        [InlineData( 1380, "23 hours ago")]
        [InlineData( 1500, "a day ago")]
        [InlineData( 3000, "two days ago")]
        [InlineData( 14400, "a week ago")]
        [InlineData(1440 * 15, "two weeks ago")]
        [InlineData(1440 * 30, "a month ago")]
        [InlineData(1440 * 65, "two months ago")]
        [InlineData(1440 * 365, "a year ago")]
        [InlineData(1440 * 730, "two years ago")]
        [InlineData(1440 * 3650, "10 years ago")]
        public void HoursAgo_ReturnsString(int minutesAgo, string output)
        {
            var timeAgo = DateTime.UtcNow.AddMinutes(-1 * minutesAgo);
            var result = ((DateTime?)timeAgo).ToApproxTimeAgoString();
                
            Assert.Equal(output, result);

        }
    }
}
