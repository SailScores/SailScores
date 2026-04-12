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
    private readonly PublicApiService _service;

    public PublicApiServiceTests()
    {
        _coreSeriesServiceMock = new Mock<SailScores.Core.Services.ISeriesService>();
        _coreClubServiceMock = new Mock<SailScores.Core.Services.IClubService>();
        _coreSeasonServiceMock = new Mock<SailScores.Core.Services.ISeasonService>();
        _service = new PublicApiService(
            _coreSeriesServiceMock.Object,
            _coreClubServiceMock.Object,
            _coreSeasonServiceMock.Object);
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

        var result = await _service.GetSeriesDetailAsync(clubId.ToString(), "2026", seriesId.ToString());

        Assert.NotNull(result);
        Assert.Equal(series.Id, result.Id);
        Assert.Equal("LHYC", result.ClubInitials);
        Assert.Equal("/LHYC/2026/spring-series", result.HtmlUrl);
        _coreSeriesServiceMock.Verify(s => s.GetOneSeriesAsync(seriesId), Times.Once);
        _coreSeriesServiceMock.Verify(
            s => s.GetSeriesDetailsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
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

        return new Series
        {
            Id = seriesId,
            ClubId = clubId,
            Name = "Spring Series",
            UrlName = seriesUrlName,
            Season = new Season
            {
                Id = seasonId,
                Name = $"Season {seasonUrlName}",
                UrlName = seasonUrlName,
                Start = new DateTime(2026, 1, 1)
            },
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
                        UrlName = "skipper-one"
                    }
                ],
                CalculatedScores =
                [
                    new FlatSeriesScore
                    {
                        CompetitorId = competitorId,
                        Rank = 1,
                        TotalScore = 5m
                    }
                ]
            }
        };
    }
}
