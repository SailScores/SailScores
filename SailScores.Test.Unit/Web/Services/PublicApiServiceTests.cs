using Moq;
using SailScores.Core.FlatModel;
using SailScores.Core.Model;
using SailScores.Core.Model.Summary;
using SailScores.Web.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace SailScores.Test.Unit.Web.Services;

public class PublicApiServiceTests
{
    private readonly Mock<SailScores.Core.Services.ISeriesService> _coreSeriesServiceMock;
    private readonly Mock<SailScores.Core.Services.IClubService> _coreClubServiceMock;
    private readonly Mock<SailScores.Core.Services.ISeasonService> _coreSeasonServiceMock;
    private readonly Mock<SailScores.Core.Services.IRaceService> _coreRaceServiceMock;
    private readonly PublicApiService _service;

    public PublicApiServiceTests()
    {
        _coreSeriesServiceMock = new Mock<SailScores.Core.Services.ISeriesService>();
        _coreClubServiceMock = new Mock<SailScores.Core.Services.IClubService>();
        _coreSeasonServiceMock = new Mock<SailScores.Core.Services.ISeasonService>();
        _coreRaceServiceMock = new Mock<SailScores.Core.Services.IRaceService>();
        _service = new PublicApiService(
            _coreSeriesServiceMock.Object,
            _coreClubServiceMock.Object,
            _coreSeasonServiceMock.Object,
            _coreRaceServiceMock.Object);
    }

    [Fact]
    public void GetRootResponse_ReturnsVersionAndClubsIndexUrl()
    {
        var result = _service.GetRootResponse();

        Assert.NotNull(result);
        Assert.Equal("v1", result.Version);
        Assert.Equal("/api/public/v1/clubs", result.ClubsIndexUrl);
    }

    [Fact]
    public async Task GetClubsAsync_UsesPublicVisibilityAndSortsAlphabetically()
    {
        _coreClubServiceMock.Setup(s => s.GetClubs(false)).ReturnsAsync(
        [
            new ClubSummary { Id = Guid.NewGuid(), Initials = "ZZZ", Name = "Zeta" },
            new ClubSummary { Id = Guid.NewGuid(), Initials = "ABC", Name = "Alpha" }
        ]);

        var result = await _service.GetClubsAsync();

        Assert.NotNull(result);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal("ABC", result.Items[0].ClubInitials);
        Assert.Equal("/api/public/v1/clubs/ABC", result.Items[0].Url);
        Assert.Equal("ZZZ", result.Items[1].ClubInitials);
        _coreClubServiceMock.Verify(s => s.GetClubs(false), Times.Once);
    }

    [Fact]
    public async Task GetClubAsync_ReturnsClubDetailWithCanonicalUrl()
    {
        var clubId = Guid.NewGuid();
        _coreClubServiceMock.Setup(s => s.GetClubId("MYC")).ReturnsAsync(clubId);
        _coreClubServiceMock.Setup(s => s.GetMinimalClub(clubId)).ReturnsAsync(new Club
        {
            Id = clubId,
            Initials = "MYC",
            Name = "My Club",
            Description = "Club Description"
        });

        var result = await _service.GetClubAsync("MYC");

        Assert.NotNull(result);
        Assert.Equal(clubId, result.Id);
        Assert.Equal("MYC", result.ClubInitials);
        Assert.Equal("/api/public/v1/clubs/MYC", result.Url);
        Assert.Equal("/MYC", result.HtmlUrl);
    }

    [Fact]
    public async Task GetSeasonsAsync_UnknownClub_ReturnsNull()
    {
        var missingClubId = Guid.NewGuid();
        _coreClubServiceMock.Setup(s => s.GetMinimalClub(missingClubId)).ReturnsAsync((Club)null);

        var result = await _service.GetSeasonsAsync(missingClubId.ToString());

        Assert.Null(result);
    }

    [Fact]
    public async Task GetSeriesAsync_WithSeasonFilter_ReturnsOnlyMatchingSeason()
    {
        var clubId = Guid.NewGuid();
        var season1 = Guid.NewGuid();
        var season2 = Guid.NewGuid();
        _coreClubServiceMock.Setup(s => s.GetClubId("MYC")).ReturnsAsync(clubId);
        _coreClubServiceMock.Setup(s => s.GetMinimalClub(clubId)).ReturnsAsync(new Club
        {
            Id = clubId,
            Initials = "MYC"
        });

        _coreSeriesServiceMock.Setup(s => s.GetAllSeriesAsync(clubId, null, false, true)).ReturnsAsync(
        [
            BuildSeries(clubId, Guid.NewGuid(), season1, "2025", "spring"),
            BuildSeries(clubId, Guid.NewGuid(), season2, "2026", "summer")
        ]);

        var result = await _service.GetSeriesAsync("MYC", "2026");

        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal("2026", result.Items[0].SeasonUrlName);
        Assert.Equal("summer", result.Items[0].Url.Split('/').Last());
    }

    [Fact]
    public async Task GetSeriesDetailAsync_GuidSeriesToken_UsesGetOneSeriesAsync()
    {
        var clubId = Guid.NewGuid();
        var seriesId = Guid.NewGuid();
        var seasonId = Guid.NewGuid();
        var club = new Club { Id = clubId, Initials = "LHYC" };
        var series = BuildSeries(clubId, seriesId, seasonId, "2026", "spring-series");

        _coreClubServiceMock.Setup(s => s.GetMinimalClub(clubId)).ReturnsAsync(club);
        _coreSeriesServiceMock.Setup(s => s.GetOneSeriesAsync(seriesId)).ReturnsAsync(series);

        var result = await _service.GetSeriesDetailAsync(
            clubId.ToString(),
            "2026",
            seriesId.ToString(),
            includeCompetitors: true,
            includeRaces: true);

        Assert.NotNull(result);
        Assert.Equal(series.Id, result.Id);
        Assert.Equal("LHYC", result.ClubInitials);
        Assert.Equal("/LHYC/2026/spring-series", result.HtmlUrl);
        Assert.Equal("Spring Series description", result.Description);
        Assert.Equal("Summary", result.SeriesType);
        Assert.Equal(string.Empty, result.FleetName);
        Assert.Equal("PreviousRace", result.TrendOption);
        Assert.True(result.PreferAlternativeSailNumbers);
        Assert.True(result.HideDncDiscards);
        Assert.True(result.IsPreliminary);
        Assert.Equal(3, result.NumberOfSailedRaces);
        Assert.Equal(1, result.NumberOfDiscards);
        Assert.Equal(1, result.CompetitorCount);
        Assert.Equal("Appendix A", result.ScoringSystemName);
        Assert.Equal(60m, result.PercentRequired);
        Assert.Equal("scorekeeper", result.UpdatedBy);
        Assert.Single(result.Competitors);
        Assert.Single(result.Races);
        Assert.Single(result.ScoreCodesUsed);
        Assert.Single(result.Races[0].CompetitorResults);
        Assert.Equal(TimeSpan.FromMinutes(42), result.Races[0].CompetitorResults[0].ElapsedTime);
        Assert.Equal(2, result.Competitors[0].Trend);
        Assert.Equal("ALT-101", result.Competitors[0].AlternativeSailNumber);
        Assert.Equal("Home YC", result.Competitors[0].HomeClubName);
        Assert.Equal("/LHYC/Competitor/skipper-one", result.Competitors[0].Url);
        Assert.Equal(SailScores.Api.Enumerations.RaceState.Raced, result.Races[0].State);
        Assert.Equal(180m, result.Races[0].WindDirectionDegrees);
        Assert.Equal("day-sunny", result.Races[0].WeatherIcon);
        Assert.Equal("DNC", result.ScoreCodesUsed[0].Code);
        Assert.Equal("Did not come", result.ScoreCodesUsed[0].Description);
        Assert.Equal("N+1", result.ScoreCodesUsed[0].Formula);
        _coreSeriesServiceMock.Verify(s => s.GetOneSeriesAsync(seriesId), Times.Once);
        _coreSeriesServiceMock.Verify(
            s => s.GetSeriesDetailsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task GetSeriesDetailAsync_WithCompetitorsInclude_ReturnsCompetitorsWithoutRaces()
    {
        var clubId = Guid.NewGuid();
        var seasonId = Guid.NewGuid();
        var club = new Club { Id = clubId, Initials = "MYC" };
        var series = BuildSeries(clubId, Guid.NewGuid(), seasonId, "2025", "fall");

        _coreClubServiceMock.Setup(s => s.GetClubId("MYC")).ReturnsAsync(clubId);
        _coreClubServiceMock.Setup(s => s.GetMinimalClub(clubId)).ReturnsAsync(club);
        _coreSeriesServiceMock.Setup(s => s.GetSeriesDetailsAsync("MYC", "2025", "fall")).ReturnsAsync(series);

        var result = await _service.GetSeriesDetailAsync(
            "MYC",
            "2025",
            "fall",
            includeCompetitors: true);

        Assert.NotNull(result);
        Assert.Single(result.Competitors);
        Assert.Null(result.Races);
        Assert.Null(result.ScoreCodesUsed);
    }

    [Fact]
    public async Task GetSeriesDetailAsync_WithRacesInclude_ReturnsRacesWithoutCompetitorResults()
    {
        var clubId = Guid.NewGuid();
        var seasonId = Guid.NewGuid();
        var club = new Club { Id = clubId, Initials = "MYC" };
        var series = BuildSeries(clubId, Guid.NewGuid(), seasonId, "2025", "fall");

        _coreClubServiceMock.Setup(s => s.GetClubId("MYC")).ReturnsAsync(clubId);
        _coreClubServiceMock.Setup(s => s.GetMinimalClub(clubId)).ReturnsAsync(club);
        _coreSeriesServiceMock.Setup(s => s.GetSeriesDetailsAsync("MYC", "2025", "fall")).ReturnsAsync(series);

        var result = await _service.GetSeriesDetailAsync(
            "MYC",
            "2025",
            "fall",
            includeRaces: true);

        Assert.NotNull(result);
        Assert.Null(result.Competitors);
        Assert.Single(result.Races);
        Assert.Null(result.Races[0].CompetitorResults);
        Assert.Single(result.ScoreCodesUsed);
    }

    [Fact]
    public async Task GetSeriesDetailAsync_WithCompetitorUrlNameMissing_UsesCurrentUrlIdInCompetitorUrl()
    {
        var clubId = Guid.NewGuid();
        var seasonId = Guid.NewGuid();
        var club = new Club { Id = clubId, Initials = "MYC" };
        var series = BuildSeries(clubId, Guid.NewGuid(), seasonId, "2025", "fall");
        series.Competitors[0].UrlName = null;
        series.Competitors[0].UrlId = "comp-42";

        _coreClubServiceMock.Setup(s => s.GetClubId("MYC")).ReturnsAsync(clubId);
        _coreClubServiceMock.Setup(s => s.GetMinimalClub(clubId)).ReturnsAsync(club);
        _coreSeriesServiceMock.Setup(s => s.GetSeriesDetailsAsync("MYC", "2025", "fall")).ReturnsAsync(series);

        var result = await _service.GetSeriesDetailAsync(
            "MYC",
            "2025",
            "fall",
            includeCompetitors: true);

        Assert.NotNull(result);
        Assert.Single(result.Competitors);
        Assert.Equal("/MYC/Competitor/comp-42", result.Competitors[0].Url);
    }

    [Fact]
    public async Task GetRaceDetailAsync_ValidClubAndRace_ReturnsRaceDetail()
    {
        var clubId = Guid.NewGuid();
        var raceId = Guid.NewGuid();
        _coreClubServiceMock.Setup(s => s.GetClubId("MYC")).ReturnsAsync(clubId);
        _coreClubServiceMock.Setup(s => s.GetMinimalClub(clubId)).ReturnsAsync(new Club
        {
            Id = clubId,
            Initials = "MYC"
        });
        _coreRaceServiceMock.Setup(s => s.GetRaceAsync(raceId)).ReturnsAsync(new Race
        {
            Id = raceId,
            ClubId = clubId,
            Name = "Race A",
            Date = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
            Order = 2,
            State = SailScores.Api.Enumerations.RaceState.Raced,
            Description = "Race description",
            UpdatedDate = new DateTime(2026, 5, 2, 0, 0, 0, DateTimeKind.Utc),
            UpdatedBy = "tester",
            Scores =
            [
                new Score
                {
                    CompetitorId = Guid.NewGuid(),
                    Competitor = new Competitor { Name = "Skipper One" },
                    Place = 1,
                    Code = "",
                    CodePoints = null,
                    FinishTime = new DateTime(2026, 5, 1, 15, 42, 0, DateTimeKind.Utc),
                    ElapsedTime = TimeSpan.FromMinutes(42)
                }
            ],
            Weather = new Weather
            {
                Icon = "day-cloudy",
                WindDirectionDegrees = 210,
                WindSpeedString = "11"
            }
        });

        var result = await _service.GetRaceDetailAsync("MYC", raceId);

        Assert.NotNull(result);
        Assert.Equal(raceId, result.Id);
        Assert.Equal("MYC", result.ClubInitials);
        Assert.Equal("Race A", result.Name);
        Assert.Equal(2, result.Order);
        Assert.Equal(SailScores.Api.Enumerations.RaceState.Raced, result.State);
        Assert.Equal("Race description", result.Description);
        Assert.Equal("/MYC/Race/Details/" + raceId, result.HtmlUrl);
        Assert.Equal("day-cloudy", result.WeatherIcon);
        Assert.Equal(210m, result.WindDirectionDegrees);
        Assert.Equal("11", result.WindSpeed);
        Assert.Equal("tester", result.UpdatedBy);
        Assert.Single(result.CompetitorResults);
        Assert.Equal("Skipper One", result.CompetitorResults[0].CompetitorName);
        Assert.Equal(TimeSpan.FromMinutes(42), result.CompetitorResults[0].ElapsedTime);
        Assert.Equal(
            new DateTimeOffset(new DateTime(2026, 5, 1, 15, 42, 0, DateTimeKind.Utc)),
            result.CompetitorResults[0].FinishTimeUtc);
    }

    [Fact]
    public async Task GetRaceDetailAsync_RaceFromDifferentClub_ReturnsNull()
    {
        var requestedClubId = Guid.NewGuid();
        var otherClubId = Guid.NewGuid();
        var raceId = Guid.NewGuid();
        _coreClubServiceMock.Setup(s => s.GetClubId("MYC")).ReturnsAsync(requestedClubId);
        _coreClubServiceMock.Setup(s => s.GetMinimalClub(requestedClubId)).ReturnsAsync(new Club
        {
            Id = requestedClubId,
            Initials = "MYC"
        });
        _coreRaceServiceMock.Setup(s => s.GetRaceAsync(raceId)).ReturnsAsync(new Race
        {
            Id = raceId,
            ClubId = otherClubId,
            Name = "Other club race"
        });

        var result = await _service.GetRaceDetailAsync("MYC", raceId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetSeriesDetailAsync_UrlSeriesToken_UsesGetSeriesDetailsAsync()
    {
        var clubId = Guid.NewGuid();
        var seasonId = Guid.NewGuid();
        var club = new Club { Id = clubId, Initials = "MYC" };
        var series = BuildSeries(clubId, Guid.NewGuid(), seasonId, "2025", "fall");

        _coreClubServiceMock.Setup(s => s.GetClubId("MYC")).ReturnsAsync(clubId);
        _coreClubServiceMock.Setup(s => s.GetMinimalClub(clubId)).ReturnsAsync(club);
        _coreSeriesServiceMock.Setup(s => s.GetSeriesDetailsAsync("MYC", "2025", "fall")).ReturnsAsync(series);

        var result = await _service.GetSeriesDetailAsync("MYC", "2025", "fall");

        Assert.NotNull(result);
        Assert.Equal("MYC", result.ClubInitials);
        Assert.Equal("/MYC/2025/fall", result.HtmlUrl);
        Assert.Null(result.Competitors);
        Assert.Null(result.Races);
        Assert.Null(result.ScoreCodesUsed);
        _coreSeriesServiceMock.Verify(s => s.GetSeriesDetailsAsync("MYC", "2025", "fall"), Times.Once);
        _coreSeriesServiceMock.Verify(s => s.GetOneSeriesAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task GetClubsAsync_SetsUpdatedUtcFromMostRecentSeriesUpdate()
    {
        var clubId = Guid.NewGuid();
        _coreClubServiceMock.Setup(s => s.GetClubs(false)).ReturnsAsync(
        [
            new ClubSummary { Id = clubId, Initials = "ABC", Name = "Alpha" }
        ]);

        _coreSeriesServiceMock.Setup(s => s.GetAllSeriesAsync(clubId, null, true, true)).ReturnsAsync(
        [
            new Series { Id = Guid.NewGuid(), ClubId = clubId, UpdatedDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new Series { Id = Guid.NewGuid(), ClubId = clubId, UpdatedDate = new DateTime(2025, 2, 1, 0, 0, 0, DateTimeKind.Utc) }
        ]);

        var result = await _service.GetClubsAsync();

        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal(new DateTimeOffset(new DateTime(2025, 2, 1, 0, 0, 0, DateTimeKind.Utc)), result.Items[0].UpdatedUtc);
    }

    [Fact]
    public async Task GetClubAsync_SetsUpdatedUtcFromMostRecentSeriesUpdate()
    {
        var clubId = Guid.NewGuid();
        _coreClubServiceMock.Setup(s => s.GetClubId("MYC")).ReturnsAsync(clubId);
        _coreClubServiceMock.Setup(s => s.GetMinimalClub(clubId)).ReturnsAsync(new Club
        {
            Id = clubId,
            Initials = "MYC",
            Name = "My Club",
            Description = "Club Description"
        });

        _coreSeriesServiceMock.Setup(s => s.GetAllSeriesAsync(clubId, null, true, true)).ReturnsAsync(
        [
            new Series { Id = Guid.NewGuid(), ClubId = clubId, UpdatedDate = new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc) },
            new Series { Id = Guid.NewGuid(), ClubId = clubId, UpdatedDate = new DateTime(2026, 3, 12, 0, 0, 0, DateTimeKind.Utc) }
        ]);

        var result = await _service.GetClubAsync("MYC");

        Assert.NotNull(result);
        Assert.Equal(new DateTimeOffset(new DateTime(2026, 3, 12, 0, 0, 0, DateTimeKind.Utc)), result.UpdatedUtc);
    }

    [Fact]
    public async Task GetSeriesAsync_WithPaging_ReturnsPagedItemsAndPaginationMetadata()
    {
        var clubId = Guid.NewGuid();
        _coreClubServiceMock.Setup(s => s.GetClubId("MYC")).ReturnsAsync(clubId);
        _coreClubServiceMock.Setup(s => s.GetMinimalClub(clubId)).ReturnsAsync(new Club
        {
            Id = clubId,
            Initials = "MYC"
        });

        _coreSeriesServiceMock.Setup(s => s.GetAllSeriesAsync(clubId, null, false, true)).ReturnsAsync(
        [
            BuildSeries(clubId, Guid.NewGuid(), Guid.NewGuid(), "2024", "s1"),
            BuildSeries(clubId, Guid.NewGuid(), Guid.NewGuid(), "2024", "s2"),
            BuildSeries(clubId, Guid.NewGuid(), Guid.NewGuid(), "2024", "s3")
        ]);

        var result = await _service.GetSeriesAsync("MYC", null, page: 2, pageSize: 1);

        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.NotNull(result.Pagination);
        Assert.Equal(2, result.Pagination.Page);
        Assert.Equal(1, result.Pagination.PageSize);
        Assert.Equal(3, result.Pagination.TotalCount);
    }

    private static Series BuildSeries(Guid clubId, Guid seriesId, Guid seasonId, string seasonUrlName, string seriesUrlName)
    {
        var competitorId = Guid.NewGuid();
        var raceId = Guid.NewGuid();

        return new Series
        {
            Id = seriesId,
            ClubId = clubId,
            Name = "Spring Series",
            UrlName = seriesUrlName,
            Description = "Spring Series description",
            Type = SeriesType.Summary,
            TrendOption = SailScores.Api.Enumerations.TrendOption.PreviousRace,
            PreferAlternativeSailNumbers = true,
            HideDncDiscards = true,
            UpdatedBy = "scorekeeper",
            Season = new Season
            {
                Id = seasonId,
                Name = $"Season {seasonUrlName}",
                UrlName = seasonUrlName,
                Start = new DateTime(2026, 1, 1)
            },
            Competitors =
            [
                new Competitor
                {
                    Id = competitorId,
                    ClubId = clubId,
                    Name = "Skipper One",
                    SailNumber = "101",
                    AlternativeSailNumber = "ALT-101",
                    HomeClubName = "Home YC",
                    UrlName = "skipper-one",
                    UrlId = "comp-101"
                }
            ],
            UpdatedDate = new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc),
            FlatResults = new FlatResults
            {
                Competitors =
                [
                    new FlatCompetitor
                    {
                        Id = competitorId,
                        Name = "Skipper One",
                        SailNumber = "101",
                        AlternativeSailNumber = "ALT-101",
                        HomeClubName = "Home YC"
                    }
                ],
                Races =
                [
                    new FlatRace
                    {
                        Id = raceId,
                        Name = "Race 1",
                        Date = new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc),
                        Order = 1,
                        State = SailScores.Api.Enumerations.RaceState.Raced,
                        WindDirectionDegrees = 180,
                        WeatherIcon = "day-sunny"
                    }
                ],
                CalculatedScores =
                [
                    new FlatSeriesScore
                    {
                        CompetitorId = competitorId,
                        Rank = 1,
                        Trend = 2,
                        TotalScore = 5m,
                        Scores =
                        [
                            new FlatCalculatedScore
                            {
                                RaceId = raceId,
                                Place = 1,
                                Code = "",
                                ScoreValue = 1m,
                                PerfectScoreValue = 1m,
                                Discard = false,
                                ElapsedTime = TimeSpan.FromMinutes(42)
                            }
                        ]
                    }
                ],
                IsPreliminary = true,
                NumberOfSailedRaces = 3,
                NumberOfDiscards = 1,
                IsPercentSystem = true,
                PercentRequired = 60m,
                ScoringSystemName = "Appendix A",
                ScoreCodesUsed = new Dictionary<string, SailScores.Core.Scoring.ScoreCodeSummary>
                {
                    ["DNC"] = new SailScores.Core.Scoring.ScoreCodeSummary
                    {
                        Name = "DNC",
                        Description = "Did not come",
                        Formula = "N+1"
                    }
                }
            }
        };
    }
}
