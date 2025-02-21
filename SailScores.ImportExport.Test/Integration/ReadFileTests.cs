using System.Linq;
using Xunit;

namespace SailScores.ImportExport.Sailwave.Test.Integration;

public class ReadFileTests
{
    private readonly string _simpleFilePath = @"../../../SailwaveFiles/SimpleSeries.blw";
    private readonly string _lhycFilePath = @"../../../SailwaveFiles/LHYCSeries.blw";

    [Fact]
    public void BasicReadFile()
    {
        var reader = new SailwaveFileReader(_simpleFilePath);

        Assert.NotNull(reader.Series);
    }



    [Fact]
    public void SimpleFile_HasTwoCompetitors()
    {
        var reader = new SailwaveFileReader(_simpleFilePath);

        Assert.Equal(2, reader.Series.Competitors.Count);
        Assert.Single(reader.Series.Competitors, c => c.Id == 3);
    }

    [Fact]
    public void LhycFile_HasManyCompetitors()
    {
        var reader = new SailwaveFileReader(_lhycFilePath);

        Assert.True(reader.Series.Competitors.Count > 40);
    }
}
