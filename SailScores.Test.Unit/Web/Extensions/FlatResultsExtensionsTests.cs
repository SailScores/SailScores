using SailScores.Core.FlatModel;
using SailScores.Core.Scoring.Eligibility;
using SailScores.Web.Extensions;
using Xunit;

namespace SailScores.Test.Unit.Web.Extensions;

public class FlatResultsExtensionsTests
{
    [Theory]
    [InlineData(19, 50, 10)]
    [InlineData(20, 50, 10)]
    [InlineData(5, 60, 3)]
    [InlineData(7, 33.3, 3)]
    public void RacesRequired_Percent_RoundsUpToWholeRaces(int raceCount, double percent, int expected)
    {
        Assert.Equal(expected, ParticipationPercent.RacesRequired(raceCount, (decimal)percent));
    }

    [Theory]
    [InlineData(10, "50% participation (10 races) required for ranking")]
    [InlineData(1, "50% participation (1 race) required for ranking")]
    [InlineData(null, "50% participation required for ranking")]
    public void ParticipationRequiredText_RaceCount_ShownWhenRecorded(int? racesRequired, string expected)
    {
        var results = new FlatResults { PercentRequired = 50m, RacesRequired = racesRequired };

        Assert.Equal(expected, results.ParticipationRequiredText());
    }
}
