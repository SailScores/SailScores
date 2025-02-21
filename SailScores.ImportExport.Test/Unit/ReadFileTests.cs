using System.Linq;
using SailScores.ImportExport.Sailwave.Elements;
using SailScores.ImportExport.Sailwave.Parsers;
using Xunit;

namespace SailScores.ImportExport.Sailwave.Test.Unit;

public class ReadFileTests
{

    [Fact]
    public void BasicReadFile()
    {
        var series = SeriesParser.GetSeries(Utilities.SimpleFile.GetStream());

        Assert.NotNull(series);
    }

    [Fact]
    public void SimpleFile_HasTwoCompetitors()
    {
        var series = SeriesParser.GetSeries(Utilities.SimpleFile.GetStream());

        Assert.Equal(2, series.Competitors.Count);
        Assert.Equal(1, series.Competitors.Count(c => c.Id == 3));
    }

    [Fact]
    public void SimpleFile_HasTwoRaces()
    {
        var series = SeriesParser.GetSeries(Utilities.SimpleFile.GetStream());

        Assert.Equal(2, series.Races.Count);
        Assert.Equal(1, series.Races.Count(r => r.Id == 1));
    }


    [Fact]
    public void SimpleFile_FirstRaceHasTwoResults()
    {
        var series = SeriesParser.GetSeries(Utilities.SimpleFile.GetStream());

        Assert.Equal(2, series.Races[0].Results.Count());
    }

    [Fact]
    public void SimpleFile_HasOneScoringSystem()
    {
        var series = SeriesParser.GetSeries(Utilities.SimpleFile.GetStream());
        Assert.Single(series.ScoringSystems);
    }

    [Fact]
    public void SimpleFile_ScoringSystemHasNonZeroId()
    {
        var series = SeriesParser.GetSeries(Utilities.SimpleFile.GetStream());
        Assert.NotEqual(0, series.ScoringSystems.FirstOrDefault()?.Id);
    }


    [Fact]
    public void SimpleFile_ScoringSystemHas16Codes()
    {
        var series = SeriesParser.GetSeries(Utilities.SimpleFile.GetStream());
        Assert.Equal(16, series.ScoringSystems[0].Codes.Count);
    }


    [Fact]
    public void SimpleFile_ScoringSystemUiField()
    {
        var series = SeriesParser.GetSeries(Utilities.SimpleFile.GetStream());
        Assert.NotNull(series.UserInterface);
    }


    [Fact]
    public void SimpleFile_HasColumns()
    {
        var series = SeriesParser.GetSeries(Utilities.SimpleFile.GetStream());
        Assert.NotNull(series.Columns);
        Assert.True(series.Columns.Count() > 200);
    }

    [Fact]
    public void SimpleFile_HasOneRaceColumns()
    {
        var series = SeriesParser.GetSeries(Utilities.SimpleFile.GetStream());
        Assert.NotNull(series.Columns);
        Assert.True(series.Columns.Where(c => c.Type == ColumnType.Races).Count() == 1);
    }
}
