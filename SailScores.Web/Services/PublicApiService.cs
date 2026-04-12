using SailScores.Api.Dtos.Public;
using SailScores.Core.FlatModel;
using SailScores.Web.Services.Interfaces;
using CoreClubService = SailScores.Core.Services.IClubService;
using CoreSeriesService = SailScores.Core.Services.ISeriesService;

namespace SailScores.Web.Services;

public class PublicApiService : IPublicApiService
{
    private const string ClubsIndexPath = "/api/public/v1/clubs";

    private readonly CoreSeriesService _coreSeriesService;
    private readonly CoreClubService _coreClubService;

    private sealed class ResolvedSeriesContext
    {
        public Core.Model.Series Series { get; init; }
        public string ClubInitials { get; init; }
    }

    private sealed class ResolvedClubContext
    {
        public Guid ClubId { get; init; }
        public string ClubInitials { get; init; }
    }

    public PublicApiService(
        CoreSeriesService coreSeriesService,
        CoreClubService coreClubService)
    {
        _coreSeriesService = coreSeriesService;
        _coreClubService = coreClubService;
    }

    public PublicApiRootResponseDto GetRootResponse()
    {
        return new PublicApiRootResponseDto
        {
            Version = "v1",
            ClubsIndexUrl = ClubsIndexPath
        };
    }

    public async Task<PublicSeriesDetailResponseDto> GetSeriesDetailAsync(
        string clubInitials,
        string seasonUrlName,
        string seriesUrlName)
    {
        var resolved = await ResolveSeriesAsync(clubInitials, seasonUrlName, seriesUrlName);
        if (resolved == null)
        {
            return null;
        }

        var series = resolved.Series;
        var resolvedClubInitials = resolved.ClubInitials;

        var seasonToken = GetRouteToken(series.Season?.UrlName, series.Season?.Id);
        var seriesToken = GetRouteToken(series.UrlName, series.Id);

        var htmlSeriesUrl = BuildSeriesHtmlUrl(resolvedClubInitials, seasonToken, seriesToken);

        return new PublicSeriesDetailResponseDto
        {
            Id = series.Id,
            Name = series.Name,
            UrlName = series.UrlName,
            HtmlUrl = htmlSeriesUrl,
            ClubInitials = resolvedClubInitials,
            SeasonName = series.Season?.Name ?? seasonUrlName,
            SeasonUrlName = seasonToken,
            UpdatedUtc = GetUtcOffset(series.UpdatedDate),
            Competitors = MapCompetitors(
                series.FlatResults?.Competitors,
                series.FlatResults?.CalculatedScores,
                resolvedClubInitials)
        };
    }

    private async Task<ResolvedSeriesContext> ResolveSeriesAsync(
        string clubToken,
        string seasonUrlName,
        string seriesUrlName)
    {
        if (string.IsNullOrWhiteSpace(clubToken)
            || string.IsNullOrWhiteSpace(seasonUrlName)
            || string.IsNullOrWhiteSpace(seriesUrlName))
        {
            return null;
        }

        try
        {
            var club = await ResolveClubContextAsync(clubToken);
            if (club == null)
            {
                return null;
            }

            Core.Model.Series series;
            if (TryParseGuid(seriesUrlName, out var seriesId))
            {
                series = await _coreSeriesService.GetOneSeriesAsync(seriesId);
                if (series == null
                    || series.ClubId != club.ClubId
                    || series.Season == null
                    || !MatchesIdOrUrlName(series.Season.Id, series.Season.UrlName, seasonUrlName))
                {
                    return null;
                }
            }
            else
            {
                series = await _coreSeriesService.GetSeriesDetailsAsync(
                    club.ClubInitials,
                    seasonUrlName,
                    seriesUrlName);
                if (series == null)
                {
                    return null;
                }
            }

            return new ResolvedSeriesContext
            {
                Series = series,
                ClubInitials = club.ClubInitials
            };
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private async Task<ResolvedClubContext> ResolveClubContextAsync(string clubToken)
    {
        var clubId = TryParseGuid(clubToken, out var parsedClubId)
            ? parsedClubId
            : await _coreClubService.GetClubId(clubToken);

        var club = await _coreClubService.GetMinimalClub(clubId);
        if (club == null || string.IsNullOrWhiteSpace(club.Initials))
        {
            return null;
        }

        return new ResolvedClubContext
        {
            ClubId = club.Id,
            ClubInitials = club.Initials
        };
    }

    private static bool TryParseGuid(string token, out Guid parsedGuid)
    {
        return Guid.TryParse(token, out parsedGuid);
    }

    private static bool MatchesIdOrUrlName(Guid id, string urlName, string routeToken)
    {
        if (TryParseGuid(routeToken, out var parsedId))
        {
            return parsedId == id;
        }

        return string.Equals(urlName, routeToken, StringComparison.OrdinalIgnoreCase);
    }

    private static string GetRouteToken(string urlName, Guid? id)
    {
        if (!string.IsNullOrWhiteSpace(urlName))
        {
            return urlName;
        }

        return id?.ToString() ?? string.Empty;
    }

    private static List<PublicSeriesCompetitorDto> MapCompetitors(
        IEnumerable<FlatCompetitor> competitors,
        IEnumerable<FlatSeriesScore> scores,
        string clubInitials)
    {
        var flatCompetitors = competitors?.ToList() ?? [];
        var scoreByCompetitorId = (scores ?? [])
            .GroupBy(s => s.CompetitorId)
            .ToDictionary(g => g.Key, g => g.First());

        return flatCompetitors
            .Select((competitor, index) =>
            {
                scoreByCompetitorId.TryGetValue(competitor.Id, out var score);

                return new PublicSeriesCompetitorDto
                {
                    Id = competitor.Id,
                    Rank = score?.Rank ?? index + 1,
                    CompetitorName = competitor.Name,
                    BoatName = competitor.BoatName ?? string.Empty,
                    SailNumber = competitor.SailNumber,
                    TotalPoints = score?.TotalScore?.ToString("0.##", CultureInfo.InvariantCulture),
                    Url = BuildCompetitorHtmlUrl(clubInitials, competitor.UrlName)
                };
            })
            .OrderBy(c => c.Rank ?? int.MaxValue)
            .ToList();
    }

    private static DateTimeOffset? GetUtcOffset(DateTime? value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        var utcDate = value.Value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
            : value.Value.ToUniversalTime();

        return new DateTimeOffset(utcDate);
    }

    private static string BuildSeriesHtmlUrl(string clubInitials, string seasonUrlName, string seriesUrlName)
    {
        return $"/{Escape(clubInitials)}/{Escape(seasonUrlName)}/{Escape(seriesUrlName)}";
    }

    private static string BuildCompetitorHtmlUrl(string clubInitials, string competitorUrlName)
    {
        return $"/{Escape(clubInitials)}/Competitor/{Escape(competitorUrlName)}";
    }

    private static string Escape(string value)
    {
        return Uri.EscapeDataString(value ?? string.Empty);
    }
}
