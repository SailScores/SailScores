using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using SailScores.Core.Model;
using SailScores.Web.Models.SailScores;
using SailScores.Web.Services;
using SailScores.Web.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace SailScores.Test.Unit.Web.Services;

public class RaceServiceTests
{
    private readonly Mock<SailScores.Core.Services.IClubService> _clubServiceMock;
    private readonly Mock<SailScores.Core.Services.IRaceService> _coreRaceServiceMock;
    private readonly Mock<SailScores.Core.Services.ISeriesService> _coreSeriesServiceMock;
    private readonly Mock<SailScores.Core.Services.IScoringService> _coreScoringServiceMock;
    private readonly Mock<SailScores.Core.Services.IRegattaService> _coreRegattaServiceMock;
    private readonly Mock<SailScores.Core.Services.ISeasonService> _coreSeasonServiceMock;
    private readonly Mock<SailScores.Core.Services.ICompetitorService> _coreCompetitorServiceMock;
    private readonly Mock<SailScores.Core.Services.IHandicapService> _coreHandicapServiceMock;
    private readonly Mock<IWeatherService> _weatherServiceMock;
    private readonly Mock<ISpeechService> _speechServiceMock;
    private readonly Mock<ILogger<RaceService>> _loggerMock;
    private readonly IMapper _mapper;

    private readonly RaceService _service;

    public RaceServiceTests()
    {
        _clubServiceMock = new Mock<SailScores.Core.Services.IClubService>();
        _coreRaceServiceMock = new Mock<SailScores.Core.Services.IRaceService>();
        _coreSeriesServiceMock = new Mock<SailScores.Core.Services.ISeriesService>();
        _coreScoringServiceMock = new Mock<SailScores.Core.Services.IScoringService>();
        _coreRegattaServiceMock = new Mock<SailScores.Core.Services.IRegattaService>();
        _coreSeasonServiceMock = new Mock<SailScores.Core.Services.ISeasonService>();
        _coreCompetitorServiceMock = new Mock<SailScores.Core.Services.ICompetitorService>();
        _coreHandicapServiceMock = new Mock<SailScores.Core.Services.IHandicapService>();
        _weatherServiceMock = new Mock<IWeatherService>();
        _speechServiceMock = new Mock<ISpeechService>();
        _loggerMock = new Mock<ILogger<RaceService>>();
        _mapper = Utilities.MapperBuilder.GetSailScoresMapper();

        _service = new RaceService(
            _clubServiceMock.Object,
            _coreRaceServiceMock.Object,
            _coreSeriesServiceMock.Object,
            _coreScoringServiceMock.Object,
            _coreRegattaServiceMock.Object,
            _coreSeasonServiceMock.Object,
            _coreCompetitorServiceMock.Object,
            _coreHandicapServiceMock.Object,
            _weatherServiceMock.Object,
            _speechServiceMock.Object,
            _mapper,
            _loggerMock.Object);
    }

    [Theory]
    [InlineData("summer-2026")]
    [InlineData("Summer 2026")]
    public async Task GetAllRaceSummariesAsync_SeasonUrlNameOrName_UsesSeason(string seasonName)
    {
        var season = SetupRaceSummaries(new List<RaceSummary>());

        var result = await _service.GetAllRaceSummariesAsync("TEST", seasonName, null, null, true, true);

        Assert.Same(season, result.CurrentSeason);
        _coreRaceServiceMock.Verify(r => r.GetRaceSummariesAsync(
            It.IsAny<Guid>(), season.Start, season.End, true, true, null));
    }

    [Fact]
    public async Task GetAllRaceSummariesAsync_MonthAndFleet_NarrowsRaceQuery()
    {
        var season = SetupRaceSummaries(new List<RaceSummary>());
        var fleetId = Guid.NewGuid();
        var june = new DateTime(2026, 6, 1);
        _coreRaceServiceMock
            .Setup(r => r.GetRaceFacetsAsync(It.IsAny<Guid>(), season.Start, season.End, true, true))
            .ReturnsAsync(new List<RaceFacet>
            {
                new() { Year = 2026, Month = 6, FleetId = fleetId, FleetShortName = "lasers", Count = 1 }
            });
        _coreRaceServiceMock
            .Setup(r => r.GetRaceSummariesAsync(
                It.IsAny<Guid>(), june, june.AddMonths(1).AddTicks(-1), true, true, fleetId))
            .ReturnsAsync(new List<RaceSummary> { MakeSummary(new List<RaceSummaryScore>()) });

        var result = await _service.GetAllRaceSummariesAsync(
            "TEST", "summer-2026", "2026-06", "lasers", true, true);

        Assert.Single(result.Races);
        Assert.Equal(fleetId, result.Filters.FleetId);
    }

    [Fact]
    public async Task GetAllRaceSummariesAsync_InvalidFilter_ReturnsNull()
    {
        SetupRaceSummaries(new List<RaceSummary>());

        var result = await _service.GetAllRaceSummariesAsync(
            "TEST", "summer-2026", "1999-01", null, true, true);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllRaceSummariesAsync_UnknownSeason_ReturnsNull()
    {
        SetupRaceSummaries(new List<RaceSummary>());

        var result = await _service.GetAllRaceSummariesAsync("TEST", "other", null, null, true, true);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllRaceSummariesAsync_MixedScoreCodes_CountsBoatsThatCameToStart()
    {
        SetupRaceSummaries(new List<RaceSummary>
        {
            MakeSummary(new List<RaceSummaryScore>
            {
                new() { Place = 1 },
                new() { Code = "DNF" },
                new() { Code = "DNC" },
                new() { Place = 0 }
            })
        });

        var result = await _service.GetAllRaceSummariesAsync("TEST", "summer-2026", null, null, true, true);

        Assert.Equal(2, Assert.Single(result.Races).CompetitorCount);
    }

    [Fact]
    public async Task GetAllRaceSummariesAsync_RaceInExcludedSeries_MarksRaceExcluded()
    {
        var excluded = MakeSummary(new List<RaceSummaryScore>());
        var included = MakeSummary(new List<RaceSummaryScore>());
        SetupRaceSummaries(new List<RaceSummary> { excluded, included }, excluded.Id);

        var result = await _service.GetAllRaceSummariesAsync("TEST", "summer-2026", null, null, true, true);

        Assert.True(result.Races.Single(r => r.Id == excluded.Id).ExcludeFromCompStats);
        Assert.False(result.Races.Single(r => r.Id == included.Id).ExcludeFromCompStats);
    }

    [Fact]
    public async Task GetAllRaceSummariesAsync_RaceWithSeries_CarriesSeriesAndSeason()
    {
        var race = MakeSummary(new List<RaceSummaryScore>());
        race.Series.Add(new RaceSummarySeries { UrlName = "tuesdays", Name = "Tuesdays" });
        var season = SetupRaceSummaries(new List<RaceSummary> { race });

        var result = await _service.GetAllRaceSummariesAsync("TEST", "summer-2026", null, null, true, true);

        var summary = Assert.Single(result.Races);
        Assert.Same(season, summary.Season);
        var series = Assert.Single(summary.SeriesUrlAndNames);
        Assert.Equal("tuesdays", series.Key);
        Assert.Equal("Tuesdays", series.Value);
    }

    private static RaceSummary MakeSummary(IList<RaceSummaryScore> scores)
    {
        return new RaceSummary
        {
            Id = Guid.NewGuid(),
            Date = new DateTime(2026, 7, 1),
            Scores = scores,
            Series = new List<RaceSummarySeries>()
        };
    }

    private Season SetupRaceSummaries(IList<RaceSummary> races, params Guid[] excludedRaces)
    {
        var clubId = Guid.NewGuid();
        var season = new Season
        {
            Id = Guid.NewGuid(),
            Name = "Summer 2026",
            UrlName = "summer-2026",
            Start = new DateTime(2026, 5, 1),
            End = new DateTime(2026, 9, 30)
        };
        IEnumerable<ScoreCode> scoreCodes = new List<ScoreCode>
        {
            new() { Name = "DNF", CameToStart = true },
            new() { Name = "DNC", CameToStart = false }
        };

        _clubServiceMock.Setup(c => c.GetMinimalClub("TEST"))
            .ReturnsAsync(new Club { Id = clubId });
        _coreSeasonServiceMock.Setup(s => s.GetSeasons(clubId))
            .ReturnsAsync(new List<Season> { season });
        _coreScoringServiceMock.Setup(s => s.GetScoreCodesAsync(clubId))
            .ReturnsAsync(scoreCodes);
        _coreRaceServiceMock.Setup(r => r.GetStatsExcludedRaces(clubId, season.Id))
            .ReturnsAsync(excludedRaces.ToList());
        _coreRaceServiceMock.Setup(r => r.GetRaceFacetsAsync(clubId, season.Start, season.End, true, true))
            .ReturnsAsync(new List<RaceFacet>());
        _coreRaceServiceMock.Setup(r => r.GetRaceSummariesAsync(
                clubId, season.Start, season.End, true, true, null))
            .ReturnsAsync(races);

        return season;
    }

    [Fact]
    public async Task GetSingleRaceDetailsAsync_TrackTimesAndOneSystem_ShowsCorrectedTime()
    {
        var clubId = Guid.NewGuid();
        var competitorId = Guid.NewGuid();
        var handicapSystemId = Guid.NewGuid();

        var race = new Race
        {
            Id = Guid.NewGuid(),
            ClubId = clubId,
            Date = DateTime.Today,
            TrackTimes = true,
            CourseDistance = 1.0m,
            Series = new List<Series>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    ClubId = clubId,
                    HandicapSystemId = handicapSystemId
                }
            },
            Scores = new List<Score>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    CompetitorId = competitorId,
                    ElapsedTime = TimeSpan.FromMinutes(60)
                }
            }
        };

        _coreRaceServiceMock.Setup(s => s.GetRaceAsync(race.Id)).ReturnsAsync(race);
        _coreRaceServiceMock.Setup(s => s.GetRaceHandicapSystemsAsync(race.Id)).ReturnsAsync(new List<HandicapSystem>
        {
            new() { Id = handicapSystemId, SystemType = HandicapSystemType.Portsmouth }
        });
        _coreScoringServiceMock.Setup(s => s.GetScoreCodesAsync(clubId)).ReturnsAsync(new List<ScoreCode>());
        _coreHandicapServiceMock
            .Setup(s => s.BuildHandicapLookupAsync(
                handicapSystemId,
                It.IsAny<IReadOnlyCollection<Guid>>(),
                It.IsAny<IReadOnlyCollection<DateTime>>()))
            .ReturnsAsync(new Dictionary<(Guid competitorId, DateTime raceDate), decimal>
            {
                [(competitorId, race.Date.Value.Date)] = 1000m
            });
        _weatherServiceMock
            .Setup(s => s.ConvertToLocalizedWeather(It.IsAny<Weather>(), clubId))
            .ReturnsAsync((SailScores.Web.Models.SailScores.WeatherViewModel)null);
        _coreRegattaServiceMock
            .Setup(s => s.GetRegattaForRace(race.Id))
            .ReturnsAsync((Regatta)null);

        var result = await _service.GetSingleRaceDetailsAsync("TEST", race.Id);

        Assert.True(result.ShowCorrectedTime);
        Assert.Null(result.CorrectedTimeNote);
        Assert.True(result.Scores[0].CorrectedTime.HasValue);
    }

    [Fact]
    public async Task GetSingleRaceDetailsAsync_TrackTimesAndMultipleSystems_ShowsNoteOnly()
    {
        var clubId = Guid.NewGuid();

        var race = new Race
        {
            Id = Guid.NewGuid(),
            ClubId = clubId,
            Date = DateTime.Today,
            TrackTimes = true,
            Scores = new List<Score>()
        };

        _coreRaceServiceMock.Setup(s => s.GetRaceAsync(race.Id)).ReturnsAsync(race);
        _coreRaceServiceMock.Setup(s => s.GetRaceHandicapSystemsAsync(race.Id)).ReturnsAsync(new List<HandicapSystem>
        {
            new() { Id = Guid.NewGuid(), SystemType = HandicapSystemType.PhrfToD },
            new() { Id = Guid.NewGuid(), SystemType = HandicapSystemType.Portsmouth }
        });
        _coreScoringServiceMock.Setup(s => s.GetScoreCodesAsync(clubId)).ReturnsAsync(new List<ScoreCode>());
        _weatherServiceMock
            .Setup(s => s.ConvertToLocalizedWeather(It.IsAny<Weather>(), clubId))
            .ReturnsAsync((SailScores.Web.Models.SailScores.WeatherViewModel)null);
        _coreRegattaServiceMock
            .Setup(s => s.GetRegattaForRace(race.Id))
            .ReturnsAsync((Regatta)null);

        var result = await _service.GetSingleRaceDetailsAsync("TEST", race.Id);

        Assert.False(result.ShowCorrectedTime);
        Assert.False(string.IsNullOrWhiteSpace(result.CorrectedTimeNote));
    }

    [Fact]
    public async Task AddOptionsToRace_RaceUsesInactiveFleet_IncludesThatFleetInOptions()
    {
        var clubId = Guid.NewGuid();
        var activeFleet = new Fleet { Id = Guid.NewGuid(), ShortName = "Active", IsActive = true };
        var selectedInactiveFleet = new Fleet { Id = Guid.NewGuid(), ShortName = "Selected Inactive", IsActive = false };
        var otherInactiveFleet = new Fleet { Id = Guid.NewGuid(), ShortName = "Other Inactive", IsActive = false };

        _clubServiceMock.Setup(s => s.GetMinimalClub(clubId)).ReturnsAsync(new Club { Id = clubId });
        _clubServiceMock.Setup(s => s.GetAllFleets(clubId)).ReturnsAsync(new List<Fleet>
        {
            activeFleet, selectedInactiveFleet, otherInactiveFleet
        });
        _clubServiceMock.Setup(s => s.GetAllBoatClasses(clubId)).ReturnsAsync(new List<BoatClass>());
        _coreSeriesServiceMock
            .Setup(s => s.GetAllSeriesAsync(clubId, It.IsAny<DateTime>(), true, false))
            .ReturnsAsync(new List<Series>());
        _coreScoringServiceMock.Setup(s => s.GetScoreCodesAsync(clubId)).ReturnsAsync(new List<ScoreCode>());
        _weatherServiceMock.Setup(s => s.GetWeatherIconOptions()).Returns(new List<KeyValuePair<string, string>>());

        var raceWithOptions = new RaceWithOptionsViewModel
        {
            ClubId = clubId,
            FleetId = selectedInactiveFleet.Id
        };

        await _service.AddOptionsToRace(raceWithOptions);

        Assert.Contains(raceWithOptions.FleetOptions, f => f.Id == selectedInactiveFleet.Id);
        Assert.Contains(raceWithOptions.FleetOptions, f => f.Id == activeFleet.Id);
        Assert.DoesNotContain(raceWithOptions.FleetOptions, f => f.Id == otherInactiveFleet.Id);
    }

    [Fact]
    public async Task AddOptionsToRace_IncludeInactiveTrue_IncludesAllInactiveFleets()
    {
        var clubId = Guid.NewGuid();
        var activeFleet = new Fleet { Id = Guid.NewGuid(), ShortName = "Active", IsActive = true };
        var inactiveFleet = new Fleet { Id = Guid.NewGuid(), ShortName = "Inactive", IsActive = false };
        var anotherInactiveFleet = new Fleet { Id = Guid.NewGuid(), ShortName = "Another Inactive", IsActive = false };

        _clubServiceMock.Setup(s => s.GetMinimalClub(clubId)).ReturnsAsync(new Club { Id = clubId });
        _clubServiceMock.Setup(s => s.GetAllFleets(clubId)).ReturnsAsync(new List<Fleet>
        {
            activeFleet, inactiveFleet, anotherInactiveFleet
        });
        _clubServiceMock.Setup(s => s.GetAllBoatClasses(clubId)).ReturnsAsync(new List<BoatClass>());
        _coreSeriesServiceMock
            .Setup(s => s.GetAllSeriesAsync(clubId, It.IsAny<DateTime>(), true, false))
            .ReturnsAsync(new List<Series>());
        _coreScoringServiceMock.Setup(s => s.GetScoreCodesAsync(clubId)).ReturnsAsync(new List<ScoreCode>());
        _weatherServiceMock.Setup(s => s.GetWeatherIconOptions()).Returns(new List<KeyValuePair<string, string>>());

        var raceWithOptions = new RaceWithOptionsViewModel
        {
            ClubId = clubId,
            FleetId = inactiveFleet.Id
        };

        await _service.AddOptionsToRace(raceWithOptions, includeInactive: true);

        Assert.Contains(raceWithOptions.FleetOptions, f => f.Id == activeFleet.Id);
        Assert.Contains(raceWithOptions.FleetOptions, f => f.Id == inactiveFleet.Id);
        Assert.Contains(raceWithOptions.FleetOptions, f => f.Id == anotherInactiveFleet.Id);
    }

    [Fact]
    public async Task GetBlankRaceWithOptions_DateRestrictedSeriesWithRaces_DefaultsToLastRaceDate()
    {
        var lastRaceDate = DateTime.Today.AddDays(-3);
        var series = MakeDateRestrictedSeries(
            DateTime.Today.AddDays(-10),
            DateTime.Today.AddDays(10),
            DateTime.Today.AddDays(-5),
            lastRaceDate);

        var result = await GetBlankSeriesRace(series);

        Assert.Equal(lastRaceDate, result.Date);
        Assert.False(result.NeedsLocalDate);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(-1)]
    public async Task GetBlankRaceWithOptions_DateRestrictedSeriesIncludesToday_UsesClubDefault(
        int? defaultRaceDateOffset)
    {
        var series = MakeDateRestrictedSeries(
            DateTime.Today.AddDays(-10),
            DateTime.Today.AddDays(10));

        var result = await GetBlankSeriesRace(series, defaultRaceDateOffset);

        Assert.Equal(
            defaultRaceDateOffset.HasValue ? DateTime.Today.AddDays(defaultRaceDateOffset.Value) : null,
            result.Date);
        Assert.Equal(defaultRaceDateOffset, result.DefaultRaceDateOffset);
        Assert.True(result.NeedsLocalDate);
    }

    [Fact]
    public async Task GetBlankRaceWithOptions_DateRestrictedSeriesInFuture_DefaultsToSeriesStart()
    {
        var start = DateTime.Today.AddDays(5);
        var series = MakeDateRestrictedSeries(start, DateTime.Today.AddDays(10));

        var result = await GetBlankSeriesRace(series);

        Assert.Equal(start, result.Date);
    }

    [Fact]
    public async Task GetBlankRaceWithOptions_DateRestrictedSeriesInPast_DefaultsToSeriesEnd()
    {
        var end = DateTime.Today.AddDays(-5);
        var series = MakeDateRestrictedSeries(DateTime.Today.AddDays(-10), end);

        var result = await GetBlankSeriesRace(series);

        Assert.Equal(end, result.Date);
    }

    private static Series MakeDateRestrictedSeries(
        DateTime start,
        DateTime end,
        params DateTime[] raceDates)
    {
        return new Series
        {
            Id = Guid.NewGuid(),
            DateRestricted = true,
            EnforcedStartDate = DateOnly.FromDateTime(start),
            EnforcedEndDate = DateOnly.FromDateTime(end),
            Season = new Season
            {
                Start = DateTime.Today.AddYears(-1),
                End = DateTime.Today.AddYears(1)
            },
            Races = raceDates.Select(d => new Race { Id = Guid.NewGuid(), Date = d }).ToList()
        };
    }

    // By default the club has no default race date, so any date comes from the series.
    private async Task<RaceWithOptionsViewModel> GetBlankSeriesRace(
        Series series,
        int? defaultRaceDateOffset = null)
    {
        var clubId = Guid.NewGuid();
        var club = new Club { Id = clubId, Initials = "TEST", DefaultRaceDateOffset = defaultRaceDateOffset };
        series.ClubId = clubId;

        _clubServiceMock.Setup(s => s.GetMinimalClub("TEST")).ReturnsAsync(club);
        _clubServiceMock.Setup(s => s.GetAllFleets(clubId)).ReturnsAsync(new List<Fleet>());
        _clubServiceMock.Setup(s => s.GetActiveFleets(clubId, false)).ReturnsAsync(new List<Fleet>());
        _clubServiceMock.Setup(s => s.GetAllBoatClasses(clubId)).ReturnsAsync(new List<BoatClass>());
        _coreSeriesServiceMock
            .Setup(s => s.GetAllSeriesAsync(clubId, It.IsAny<DateTime>(), false, false))
            .ReturnsAsync(new List<Series>());
        _coreSeriesServiceMock.Setup(s => s.GetOneSeriesAsync(series.Id)).ReturnsAsync(series);
        _coreScoringServiceMock.Setup(s => s.GetScoreCodesAsync(clubId)).ReturnsAsync(new List<ScoreCode>());
        _weatherServiceMock.Setup(s => s.GetWeatherIconOptions()).Returns(new List<KeyValuePair<string, string>>());

        return await _service.GetBlankRaceWithOptions("TEST", null, series.Id);
    }
}
