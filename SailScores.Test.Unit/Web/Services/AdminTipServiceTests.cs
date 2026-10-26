using SailScores.Web.Models.SailScores;
using SailScores.Web.Services;
using System;
using System.Collections.Generic;
using Xunit;

namespace SailScores.Test.Unit.Web.Services;

public class AdminTipServiceTests
{
    private static readonly DateTime Today = new(2027, 3, 15);

    private readonly AdminTipService _service = new(new FixedTimeProvider(Today));

    [Fact]
    public void AddTips_SeasonCoversToday_NoSeasonTip()
    {
        var vm = MakeSetUpClub(new DateTime(2027, 1, 1), new DateTime(2027, 12, 31));

        _service.AddTips(ref vm);

        Assert.Null(vm.SeasonTip);
    }

    [Fact]
    public void AddTips_SeasonEndsToday_NoSeasonTip()
    {
        var vm = MakeSetUpClub(new DateTime(2026, 3, 16), Today);

        _service.AddTips(ref vm);

        Assert.Null(vm.SeasonTip);
    }

    [Fact]
    public void AddTips_UpcomingSeason_NoSeasonTip()
    {
        var vm = MakeSetUpClub(new DateTime(2027, 4, 1), new DateTime(2027, 10, 31));

        _service.AddTips(ref vm);

        Assert.Null(vm.SeasonTip);
    }

    [Fact]
    public void AddTips_OnlyPastSeasons_AddsSeasonTip()
    {
        var vm = MakeSetUpClub(new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));

        _service.AddTips(ref vm);

        Assert.NotNull(vm.SeasonTip);
        Assert.Equal("Season", vm.SeasonTip.Link.Controller);
        Assert.Equal("Create", vm.SeasonTip.Link.Action);
        Assert.Null(vm.Tips);
    }

    [Fact]
    public void AddTips_ClubNotSetUp_ShowsSetupListWithoutSeasonTip()
    {
        var vm = MakeSetUpClub(new DateTime(2026, 1, 1), new DateTime(2026, 12, 31));
        vm.HasRaces = false;

        _service.AddTips(ref vm);

        Assert.Null(vm.SeasonTip);
        Assert.NotEmpty(vm.Tips);
    }

    private static AdminViewModel MakeSetUpClub(DateTime seasonStart, DateTime seasonEnd)
    {
        return new AdminViewModel
        {
            HasRaces = true,
            BoatClasses = new List<BoatClassDeleteViewModel>(),
            Series = new List<SailScores.Core.Model.Series>(),
            Seasons = new List<SeasonDeleteViewModel>
            {
                new() { Name = "Season", Start = seasonStart, End = seasonEnd }
            }
        };
    }

    private sealed class FixedTimeProvider(DateTime localNow) : TimeProvider
    {
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

        public override DateTimeOffset GetUtcNow() =>
            new(DateTime.SpecifyKind(localNow, DateTimeKind.Utc));
    }
}
