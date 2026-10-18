using SailScores.Api.Enumerations;
using Xunit;

namespace SailScores.Test.Unit.Entities;

public class SeriesEntityTests
{
    [Theory]
    [InlineData(SummaryRollupMode.ChildRaces, false)]
    [InlineData(SummaryRollupMode.ChildRankAsRace, true)]
    [InlineData(SummaryRollupMode.ChildScoreAsRace, false)]
    public void SummaryRollupMode_WhenSet_SetsChildrenSeriesAsSingleRaceForOlderCode(
        SummaryRollupMode mode,
        bool expected)
    {
        var series = new SailScores.Database.Entities.Series
        {
            SummaryRollupMode = SummaryRollupMode.ChildRankAsRace
        };

        series.SummaryRollupMode = mode;

        Assert.Equal(expected, series.ChildrenSeriesAsSingleRace);
    }
}
