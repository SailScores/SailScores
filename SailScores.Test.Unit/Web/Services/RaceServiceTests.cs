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
}
