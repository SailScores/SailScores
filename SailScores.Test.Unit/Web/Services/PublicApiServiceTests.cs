using Moq;
using SailScores.Core.FlatModel;
using SailScores.Core.Model;
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
    private readonly PublicApiService _service;

    public PublicApiServiceTests()
    {
        _coreSeriesServiceMock = new Mock<SailScores.Core.Services.ISeriesService>();
        _coreClubServiceMock = new Mock<SailScores.Core.Services.IClubService>();
        _service = new PublicApiService(_coreSeriesServiceMock.Object, _coreClubServiceMock.Object);
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
        _coreSeriesServiceMock.Verify(s => s.GetSeriesDetailsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
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
                Name = "Spring 2026",
                UrlName = seasonUrlName
            },
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
