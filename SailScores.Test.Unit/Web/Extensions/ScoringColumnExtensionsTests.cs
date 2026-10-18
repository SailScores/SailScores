using SailScores.Api.Enumerations;
using SailScores.Core.FlatModel;
using SailScores.Core.Scoring;
using SailScores.Web.Extensions;
using SailScores.Web.Models.SailScores;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SailScores.Test.Unit.Web.Extensions;

public class ScoringColumnExtensionsTests
{
    private static FlatSeriesScore ScoreWith(string key, decimal value, decimal? multiplier = null) => new()
    {
        ColumnValues = new Dictionary<string, ScoringColumnValue>
        {
            [key] = new() { Value = value, Multiplier = multiplier }
        }
    };

    private static FlatResultColumn Column(string key, ScoringColumnFormat format = ScoringColumnFormat.Decimal,
        ColumnVisibility visibility = ColumnVisibility.Always) => new()
    {
        Key = key,
        Header = key,
        Format = format,
        Visibility = visibility
    };

    [Fact]
    public void GetDisplayText_WithMultiplier_ShowsValueTimesMultiplier()
    {
        var text = Column("penalties").GetDisplayText(ScoreWith("penalties", 2m, 1.5m));

        Assert.Equal($"2 × {1.5m:0.###}", text);
    }

    [Fact]
    public void GetDisplayText_Percent_AddsPercentSign()
    {
        var text = Column("participation", ScoringColumnFormat.Percent)
            .GetDisplayText(ScoreWith("participation", 66.666m));

        Assert.Equal($"{66.7m:0.#}%", text);
    }

    [Fact]
    public void GetDisplayText_NoValue_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, Column("penalties").GetDisplayText(new FlatSeriesScore()));
        Assert.Equal(string.Empty, Column("penalties").GetDisplayText(null));
    }

    [Fact]
    public void VisibleScoringColumns_TemplateShowsPreDiscardTotal_OverridesHidden()
    {
        var model = new ResultsTableViewModel
        {
            ShowPreDiscardTotal = true,
            Results = new FlatResults
            {
                Columns = new List<FlatResultColumn>
                {
                    Column(ScoringColumns.PreDiscardTotalKey, visibility: ColumnVisibility.Hidden),
                    Column(ScoringColumns.ParticipationKey, visibility: ColumnVisibility.Hidden)
                }
            }
        };

        var column = Assert.Single(model.VisibleScoringColumns);
        Assert.Equal(ScoringColumns.PreDiscardTotalKey, column.Key);
        Assert.Equal(ColumnVisibility.Always, column.Visibility);
        // The stored results aren't changed.
        Assert.Equal(ColumnVisibility.Hidden, model.Results.Columns[0].Visibility);
    }

    [Fact]
    public void VisibleScoringColumns_TemplateOff_UsesScoringSystemVisibility()
    {
        var model = new ResultsTableViewModel
        {
            Results = new FlatResults
            {
                Columns = new List<FlatResultColumn>
                {
                    Column(ScoringColumns.PreDiscardTotalKey, visibility: ColumnVisibility.Hidden),
                    Column(ScoringColumns.PenaltiesKey, visibility: ColumnVisibility.OnLargerScreens)
                }
            }
        };

        Assert.Equal(new[] { ScoringColumns.PenaltiesKey }, model.VisibleScoringColumns.Select(c => c.Key));
    }
}
