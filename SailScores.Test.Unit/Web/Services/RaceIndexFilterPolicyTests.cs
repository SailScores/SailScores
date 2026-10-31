using SailScores.Core.Model;
using SailScores.Web.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SailScores.Test.Unit.Web.Services;

public class RaceIndexFilterPolicyTests
{
    private static readonly DateTime SeasonStart = new(2026, 5, 1);
    private static readonly DateTime SeasonEnd = new(2026, 9, 30);
    private static readonly DateTime MidSeason = new(2026, 8, 15);
    private static readonly Guid LasersId = Guid.NewGuid();
    private static readonly Guid ThistlesId = Guid.NewGuid();

    private static RaceFacet Lasers(int month, int count) => new()
    {
        Year = 2026,
        Month = month,
        FleetId = LasersId,
        FleetShortName = "lasers",
        FleetName = "Lasers",
        Count = count
    };

    private static RaceFacet Thistles(int month, int count) => new()
    {
        Year = 2026,
        Month = month,
        FleetId = ThistlesId,
        FleetShortName = "thistles",
        FleetName = "Thistles",
        Count = count
    };

    private static SailScores.Web.Models.SailScores.RaceIndexFilters Build(
        IList<RaceFacet> facets,
        string month = null,
        string fleet = null,
        DateTime? today = null)
    {
        return RaceIndexFilterPolicy.Build(
            facets,
            month,
            fleet,
            SeasonStart,
            SeasonEnd,
            today ?? MidSeason);
    }

    [Theory]
    [InlineData(24, false)]
    [InlineData(25, true)]
    public void Build_SeasonRaceCount_ShowsChipsFromThreshold(int races, bool expected)
    {
        var filters = Build(new List<RaceFacet> { Lasers(6, races - 1), Thistles(7, 1) });

        Assert.Equal(expected, filters.ShowMonthChips);
        Assert.Equal(expected, filters.ShowFleetChips);
    }

    [Theory]
    [InlineData(99, false)]
    [InlineData(100, true)]
    public void Build_SeasonRaceCount_DefaultsToMonthFromThreshold(int races, bool expected)
    {
        var filters = Build(new List<RaceFacet> { Lasers(6, races - 1), Lasers(7, 1) });

        Assert.Equal(expected, filters.StartDate == new DateTime(2026, 7, 1));
        Assert.False(filters.HasActiveFilter);
        Assert.Null(filters.RequestedMonth);
    }

    [Theory]
    [InlineData(null, 7)]
    [InlineData("2026-06", 6)]
    [InlineData("all", null)]
    public void Build_MonthValue_SetsEffectiveMonthToMonthShown(string month, int? expectedMonth)
    {
        var filters = Build(new List<RaceFacet> { Lasers(6, 60), Lasers(7, 60) }, month);

        var expected = expectedMonth.HasValue
            ? new DateTime(2026, expectedMonth.Value, 1)
            : (DateTime?)null;
        Assert.Equal(expected, filters.EffectiveMonth);
    }

    [Fact]
    public void Build_OneFleet_HidesFleetChips()
    {
        var filters = Build(new List<RaceFacet> { Lasers(6, 20), Lasers(7, 20) });

        Assert.True(filters.ShowMonthChips);
        Assert.False(filters.ShowFleetChips);
    }

    [Fact]
    public void Build_OneMonth_HidesMonthChipsAndShowsWholeSeason()
    {
        var filters = Build(new List<RaceFacet> { Lasers(6, 100), Thistles(6, 100) });

        Assert.False(filters.ShowMonthChips);
        Assert.True(filters.ShowFleetChips);
        Assert.Equal(SeasonStart, filters.StartDate);
        Assert.Equal(SeasonEnd, filters.EndDate);
    }

    [Fact]
    public void Build_SmallSeasonWithActiveFilter_ShowsChipsToClearIt()
    {
        var filters = Build(
            new List<RaceFacet> { Lasers(6, 1), Thistles(7, 1) },
            month: "2026-06");

        Assert.True(filters.ShowMonthChips);
        Assert.True(filters.ShowFleetChips);
    }

    [Fact]
    public void Build_OneFleetAndFleetFilter_ShowsFleetChipsToClearIt()
    {
        var filters = Build(new List<RaceFacet> { Lasers(6, 2) }, fleet: "lasers");

        Assert.True(filters.ShowFleetChips);
    }

    [Fact]
    public void Build_NoRaces_ShowsNoChips()
    {
        var filters = Build(new List<RaceFacet>());

        Assert.True(filters.IsValid);
        Assert.False(filters.ShowMonthChips);
        Assert.False(filters.ShowFleetChips);
    }

    [Fact]
    public void Build_SeasonUnderway_DefaultsToNewestStartedMonth()
    {
        var filters = Build(new List<RaceFacet> { Lasers(6, 60), Lasers(7, 60), Lasers(9, 10) });

        Assert.Equal(new DateTime(2026, 7, 1), filters.StartDate);
        Assert.Equal(new DateTime(2026, 8, 1).AddTicks(-1), filters.EndDate);
    }

    [Fact]
    public void Build_PastSeason_DefaultsToLastMonth()
    {
        var filters = Build(
            new List<RaceFacet> { Lasers(6, 60), Lasers(9, 60) },
            today: new DateTime(2027, 2, 1));

        Assert.Equal(new DateTime(2026, 9, 1), filters.StartDate);
        Assert.Equal(SeasonEnd, filters.EndDate);
    }

    [Fact]
    public void Build_FutureSeason_DefaultsToFirstMonth()
    {
        var filters = Build(
            new List<RaceFacet> { Lasers(6, 60), Lasers(9, 60) },
            today: new DateTime(2026, 1, 1));

        Assert.Equal(new DateTime(2026, 6, 1), filters.StartDate);
    }

    [Fact]
    public void Build_MonthAllInBusySeason_ShowsWholeSeason()
    {
        var filters = Build(
            new List<RaceFacet> { Lasers(6, 60), Lasers(7, 60) },
            month: "all");

        Assert.Equal(SeasonStart, filters.StartDate);
        Assert.Equal(SeasonEnd, filters.EndDate);
        Assert.True(filters.MonthChips[0].IsSelected);
    }

    [Fact]
    public void Build_SmallFleetInBusySeason_ShowsItsWholeSeason()
    {
        var filters = Build(
            new List<RaceFacet> { Lasers(6, 100), Lasers(7, 100), Thistles(6, 5) },
            fleet: "thistles");

        Assert.Equal(SeasonStart, filters.StartDate);
        Assert.Equal(ThistlesId, filters.FleetId);
    }

    [Fact]
    public void Build_BusySeason_DefaultStateChipsLinkToBareUrl()
    {
        var filters = Build(new List<RaceFacet> { Lasers(6, 60), Lasers(7, 60) });

        var july = filters.MonthChips.Single(c => c.IsSelected);
        Assert.Null(july.Month);
        Assert.Equal("all", filters.MonthChips[0].Month);
        Assert.Equal("2026-06", filters.MonthChips[1].Month);
    }

    [Fact]
    public void Build_NoDefaultMonth_AllChipLinksToBareUrl()
    {
        var filters = Build(new List<RaceFacet> { Lasers(6, 15), Lasers(7, 15) });

        Assert.Null(filters.MonthChips[0].Month);
        Assert.True(filters.MonthChips[0].IsSelected);
    }

    [Fact]
    public void Build_FleetSelected_MonthChipsCountThatFleet()
    {
        var filters = Build(
            new List<RaceFacet> { Lasers(6, 10), Thistles(6, 4), Thistles(7, 20) },
            fleet: "lasers");

        var chip = Assert.Single(filters.MonthChips.Where(c => c.Month == "2026-06"));
        Assert.Equal(10, chip.Count);
        Assert.Equal("lasers", chip.Fleet);
        Assert.DoesNotContain(filters.MonthChips, c => c.Month == "2026-07");
    }

    [Fact]
    public void Build_MonthRequested_FleetChipsCountThatMonthAndHideEmptyFleets()
    {
        var filters = Build(
            new List<RaceFacet> { Lasers(6, 10), Lasers(7, 3), Thistles(7, 20) },
            month: "2026-06");

        var chip = Assert.Single(filters.FleetChips.Where(c => c.Fleet == "lasers"));
        Assert.Equal(10, chip.Count);
        Assert.Equal("2026-06", chip.Month);
        Assert.DoesNotContain(filters.FleetChips, c => c.Fleet == "thistles");
    }

    [Fact]
    public void Build_DefaultMonth_KeepsFleetsThatDidNotRaceInIt()
    {
        var filters = Build(new List<RaceFacet> { Lasers(6, 100), Thistles(7, 20) });

        Assert.Equal(new DateTime(2026, 7, 1), filters.StartDate);
        Assert.Equal(100, filters.FleetChips.Single(c => c.Fleet == "lasers").Count);
    }

    [Fact]
    public void Build_MonthAll_FleetChipOmitsMonthUnlessFleetDefaultsToOne()
    {
        var filters = Build(
            new List<RaceFacet> { Lasers(6, 60), Lasers(7, 60), Thistles(6, 10) },
            month: "all");

        Assert.Equal("all", filters.FleetChips[0].Month);
        Assert.Equal("all", filters.FleetChips.Single(c => c.Fleet == "lasers").Month);
        Assert.Null(filters.FleetChips.Single(c => c.Fleet == "thistles").Month);
    }

    [Fact]
    public void Build_RequestedMonthIsDefault_ClearingFleetLinksToBareUrl()
    {
        var filters = Build(
            new List<RaceFacet> { Lasers(6, 60), Lasers(7, 60), Thistles(7, 5), Thistles(6, 5) },
            month: "2026-07",
            fleet: "thistles");

        Assert.Null(filters.FleetChips[0].Month);
        Assert.Null(filters.FleetChips.Single(c => c.Fleet == "lasers").Month);
    }

    [Fact]
    public void Build_ValidCombinationWithNoRaces_KeepsSelectedChips()
    {
        var filters = Build(
            new List<RaceFacet> { Lasers(6, 10), Thistles(7, 20) },
            month: "2026-07",
            fleet: "lasers");

        Assert.True(filters.IsValid);
        Assert.True(filters.IsFleetAndMonth);
        Assert.Equal(0, filters.MonthChips.Single(c => c.IsSelected).Count);
        Assert.Equal(0, filters.FleetChips.Single(c => c.IsSelected).Count);
    }

    [Fact]
    public void Build_SeasonMonthWithNoRaces_AddsSelectedChip()
    {
        var filters = Build(new List<RaceFacet> { Lasers(6, 30) }, month: "2026-08");

        Assert.True(filters.IsValid);
        Assert.Equal(0, filters.MonthChips.Single(c => c.IsSelected).Count);
    }

    [Fact]
    public void Build_SeasonCrossesNewYear_LabelsChipsWithYear()
    {
        var filters = RaceIndexFilterPolicy.Build(
            new List<RaceFacet>
            {
                new() { Year = 2026, Month = 12, Count = 20 },
                new() { Year = 2027, Month = 1, Count = 20 }
            },
            null,
            null,
            new DateTime(2026, 11, 1),
            new DateTime(2027, 3, 1),
            new DateTime(2027, 1, 15));

        Assert.Contains("2026", filters.MonthChips[1].Label);
        Assert.Contains("2027", filters.MonthChips[2].Label);
    }

    [Theory]
    [InlineData("July")]
    [InlineData("2026-13")]
    [InlineData("2026-7")]
    [InlineData("ALL")]
    [InlineData("2026-04")]
    [InlineData("2026-10")]
    public void Build_BadMonth_IsInvalid(string month)
    {
        var filters = Build(new List<RaceFacet> { Lasers(6, 1) }, month: month);

        Assert.False(filters.IsValid);
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("LASERS")]
    public void Build_UnknownFleet_IsInvalid(string fleet)
    {
        var filters = Build(new List<RaceFacet> { Lasers(6, 1) }, fleet: fleet);

        Assert.False(filters.IsValid);
    }

    [Fact]
    public void Build_PartialFirstMonth_KeepsFilterInsideSeason()
    {
        var filters = RaceIndexFilterPolicy.Build(
            new List<RaceFacet> { Lasers(5, 1) },
            "2026-05",
            null,
            new DateTime(2026, 5, 15),
            SeasonEnd,
            MidSeason);

        Assert.True(filters.IsValid);
        Assert.Equal(new DateTime(2026, 5, 15), filters.StartDate);
    }

    [Fact]
    public void Build_RaceWithNoFleet_GetsNoFleetChip()
    {
        var filters = Build(new List<RaceFacet>
        {
            new() { Year = 2026, Month = 6, Count = 30 },
            Lasers(6, 1)
        });

        Assert.Equal(new[] { null, "lasers" }, filters.FleetChips.Select(c => c.Fleet));
    }
}
