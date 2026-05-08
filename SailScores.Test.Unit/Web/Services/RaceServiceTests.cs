using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using SailScores.Core.Model;
using SailScores.Web.Services;
using SailScores.Web.Services.Interfaces;
using System;
using System.Collections.Generic;
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
}
