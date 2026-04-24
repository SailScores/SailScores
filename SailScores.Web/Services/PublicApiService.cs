using System.Globalization;
using SailScores.Api.Dtos.Public;
using SailScores.Core.FlatModel;
using SailScores.Web.Services.Interfaces;
using CoreClubService = SailScores.Core.Services.IClubService;
using CoreSeasonService = SailScores.Core.Services.ISeasonService;
using CoreSeriesService = SailScores.Core.Services.ISeriesService;
using CoreRaceService = SailScores.Core.Services.IRaceService;

namespace SailScores.Web.Services;

public class PublicApiService : IPublicApiService
{
    private const string ApiBasePath = "/api/public/v1";
    private const string ClubsIndexPath = "/api/public/v1/clubs";

    private readonly CoreSeriesService _coreSeriesService;
    private readonly CoreClubService _coreClubService;
    private readonly CoreSeasonService _coreSeasonService;
    private readonly CoreRaceService _coreRaceService;

    private sealed class ResolvedSeriesContext
    {
        public Core.Model.Series Series { get; init; }
        public string ClubInitials { get; init; }
    }

    private sealed class ResolvedClubContext
    {
        public Guid ClubId { get; init; }
        public string ClubInitials { get; init; }
        public string Name { get; init; }
        public string Description { get; init; }
    }

    public PublicApiService(
        CoreSeriesService coreSeriesService,
        CoreClubService coreClubService,
        CoreSeasonService coreSeasonService,
        CoreRaceService coreRaceService)
    {
        _coreSeriesService = coreSeriesService;
        _coreClubService = coreClubService;
        _coreSeasonService = coreSeasonService;
        _coreRaceService = coreRaceService;
    }

    public PublicApiRootResponseDto GetRootResponse()
    {
        return new PublicApiRootResponseDto
        {
            Version = "v1",
            ClubsIndexUrl = ClubsIndexPath
        };
    }

    public async Task<PublicListResponseDto<PublicClubListItemDto>> GetClubsAsync(
        int? page = null,
        int? pageSize = null)
    {
        var clubs = await _coreClubService.GetClubs(includeHidden: false);

        var items = new List<PublicClubListItemDto>();
        foreach (var club in clubs
                     .Where(c => !string.IsNullOrWhiteSpace(c.Initials))
                     .OrderBy(c => c.Initials))
        {
            items.Add(new PublicClubListItemDto
            {
                Id = club.Id,
                ClubInitials = club.Initials,
                Name = club.Name,
                Url = BuildClubApiUrl(club.Initials),
                HtmlUrl = BuildClubHtmlUrl(club.Initials),
                UpdatedUtc = await GetMostRecentSeriesUpdateAsync(club.Id)
            });
        }

        return CreatePagedResponse(items, page, pageSize);
    }

    public async Task<PublicClubDetailResponseDto> GetClubAsync(string clubToken)
    {
        var club = await ResolveClubContextAsync(clubToken);
        if (club == null)
        {
            return null;
        }

        return new PublicClubDetailResponseDto
        {
            Id = club.ClubId,
            ClubInitials = club.ClubInitials,
            Name = club.Name,
            Description = club.Description?.Trim(),
            Url = BuildClubApiUrl(club.ClubInitials),
            HtmlUrl = BuildClubHtmlUrl(club.ClubInitials),
            UpdatedUtc = await GetMostRecentSeriesUpdateAsync(club.ClubId)
        };
    }

    public async Task<PublicListResponseDto<PublicSeasonListItemDto>> GetSeasonsAsync(
        string clubToken,
        int? page = null,
        int? pageSize = null)
    {
        var club = await ResolveClubContextAsync(clubToken);
        if (club == null)
        {
            return null;
        }

        var seasons = await _coreSeasonService.GetSeasons(club.ClubId);

        var items = seasons
            .OrderByDescending(s => s.Start)
            .Select(s =>
            {
                var seasonToken = GetRouteToken(s.UrlName, s.Id);
                return new PublicSeasonListItemDto
                {
                    Id = s.Id,
                    ClubInitials = club.ClubInitials,
                    SeasonName = s.Name,
                    SeasonUrlName = seasonToken,
                    Url = BuildSeasonSeriesApiUrl(club.ClubInitials, seasonToken),
                    UpdatedUtc = null
                };
            })
            .ToList();

        return CreatePagedResponse(items, page, pageSize);
    }

    public async Task<PublicListResponseDto<PublicSeriesListItemDto>> GetSeriesAsync(
        string clubToken,
        string seasonUrlName = null,
        int? page = null,
        int? pageSize = null)
    {
        var club = await ResolveClubContextAsync(clubToken);
        if (club == null)
        {
            return null;
        }

        var allSeries = await _coreSeriesService.GetAllSeriesAsync(
            club.ClubId,
            null,
            includeRegatta: false,
            includeSummary: true);

        var filteredSeries = allSeries
            .Where(s => s.Season != null)
            .Where(s => string.IsNullOrWhiteSpace(seasonUrlName)
                || MatchesIdOrUrlName(s.Season.Id, s.Season.UrlName, seasonUrlName));

        var items = filteredSeries
            .OrderByDescending(s => s.UpdatedDate)
            .ThenByDescending(s => s.Season.Start)
            .ThenBy(s => s.Name)
            .Select(s =>
            {
                var seasonToken = GetRouteToken(s.Season.UrlName, s.Season.Id);
                var seriesToken = GetRouteToken(s.UrlName, s.Id);
                return new PublicSeriesListItemDto
                {
                    Id = s.Id,
                    ClubInitials = club.ClubInitials,
                    SeasonName = s.Season.Name,
                    SeasonUrlName = seasonToken,
                    SeriesName = s.Name,
                    SeriesUrlName = seriesToken,
                    Url = BuildSeriesApiUrl(club.ClubInitials, seasonToken, seriesToken),
                    HtmlUrl = BuildSeriesHtmlUrl(club.ClubInitials, seasonToken, seriesToken),
                    UpdatedUtc = GetUtcOffset(s.UpdatedDate)
                };
            })
            .ToList();

        return CreatePagedResponse(items, page, pageSize);
    }

    public async Task<PublicRaceDetailResponseDto> GetRaceDetailAsync(
        string clubInitials,
        Guid raceId)
    {
        if (string.IsNullOrWhiteSpace(clubInitials))
        {
            return null;
        }

        var club = await ResolveClubContextAsync(clubInitials);
        if (club == null)
        {
            return null;
        }

        var race = await _coreRaceService.GetRaceAsync(raceId);
        if (race == null || race.ClubId != club.ClubId)
        {
            return null;
        }

        return new PublicRaceDetailResponseDto
        {
            Id = race.Id,
            ClubInitials = club.ClubInitials,
            Name = race.Name ?? string.Empty,
            DateUtc = GetUtcOffset(race.Date),
            Order = race.Order,
            State = race.State,
            Description = race.Description,
            HtmlUrl = BuildRaceHtmlUrl(club.ClubInitials, race.Id),
            WindSpeed = race.Weather?.WindSpeedString,
            WindSpeedUnits = race.Weather?.WindSpeedString == null ? null : string.Empty,
            WindDirectionDegrees = race.Weather?.WindDirectionDegrees,
            WeatherIcon = race.Weather?.Icon,
            UpdatedUtc = GetUtcOffset(race.UpdatedDate),
            UpdatedBy = race.UpdatedBy,
            CompetitorResults = (race.Scores ?? [])
                .OrderBy(s => s.Place ?? int.MaxValue)
                .Select(s => new PublicRaceCompetitorResultDto
                {
                    CompetitorId = s.CompetitorId,
                    CompetitorName = s.Competitor?.Name,
                    Place = s.Place,
                    Code = s.Code,
                    CodePoints = s.CodePoints,
                    FinishTimeUtc = GetUtcOffset(s.FinishTime),
                    ElapsedTime = s.ElapsedTime
                })
                .ToList()
        };
    }

    public async Task<PublicSeriesDetailResponseDto> GetSeriesDetailAsync(
        string clubInitials,
        string seasonUrlName,
        string seriesUrlName,
        bool includeCompetitors = false,
        bool includeRaces = false)
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
        var competitorRouteTokens = (series.Competitors ?? [])
            .GroupBy(c => c.Id)
            .ToDictionary(
                g => g.Key,
                g => GetCompetitorRouteToken(g.First().UrlName, g.First().UrlId));

        return new PublicSeriesDetailResponseDto
        {
            Id = series.Id,
            Name = series.Name,
            UrlName = series.UrlName,
            Description = series.Description,
            SeriesType = series.Type == Core.Model.SeriesType.Unknown
                ? Core.Model.SeriesType.Standard.ToString()
                : series.Type.ToString(),
            FleetName = series.Fleet == null
                ? string.Empty
                : series.GetEffectiveFleetName(),
            TrendOption = series.TrendOption?.ToString(),
            PreferAlternativeSailNumbers = series.PreferAlternativeSailNumbers ?? false,
            HideDncDiscards = series.HideDncDiscards,
            IsPreliminary = series.FlatResults?.IsPreliminary,
            NumberOfSailedRaces = series.FlatResults?.NumberOfSailedRaces ?? 0,
            NumberOfDiscards = series.FlatResults?.NumberOfDiscards ?? 0,
            CompetitorCount = series.FlatResults?.Competitors?.Count() ?? 0,
            ScoringSystemName = series.FlatResults?.ScoringSystemName,
            PercentRequired = series.FlatResults?.IsPercentSystem == true
                ? series.FlatResults.PercentRequired
                : null,
            UpdatedBy = series.UpdatedBy,
            HtmlUrl = htmlSeriesUrl,
            ClubInitials = resolvedClubInitials,
            SeasonName = series.Season?.Name ?? seasonUrlName,
            SeasonUrlName = seasonToken,
            UpdatedUtc = GetUtcOffset(series.UpdatedDate),
            Competitors = includeCompetitors
                ? MapCompetitors(
                    series.FlatResults?.Competitors,
                    series.FlatResults?.CalculatedScores,
                    competitorRouteTokens,
                    resolvedClubInitials,
                    (series.ShowCompetitorClub ?? false)
                        || (series.FlatResults?.Competitors?.Any(c => !string.IsNullOrWhiteSpace(c.HomeClubName))
                            ?? false),
                    series.PreferAlternativeSailNumbers ?? false)
                : null,
            Races = includeRaces
                ? MapRaces(
                    series.FlatResults?.Races,
                    series.FlatResults?.CalculatedScores,
                    resolvedClubInitials,
                    includeCompetitors)
                : null,
            ScoreCodesUsed = includeRaces
                ? MapScoreCodes(series.FlatResults?.ScoreCodesUsed)
                : null
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
            ClubInitials = club.Initials,
            Name = club.Name,
            Description = club.Description
        };
    }

    private async Task<DateTimeOffset?> GetMostRecentSeriesUpdateAsync(Guid clubId)
    {
        var allSeries = await _coreSeriesService.GetAllSeriesAsync(clubId, null, includeRegatta: true, includeSummary: true)
            ?? [];
        var latest = allSeries
            .Where(s => s.UpdatedDate.HasValue)
            .Select(s => s.UpdatedDate)
            .Max();

        return GetUtcOffset(latest);
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
        IDictionary<Guid, string> competitorRouteTokens,
        string clubInitials,
        bool showCompetitorClub,
        bool preferAlternativeSailNumbers,
        bool includeRaceResults)
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
                    Trend = score?.Trend,
                    CompetitorName = competitor.Name,
                    BoatName = competitor.BoatName ?? string.Empty,
                    SailNumber = competitor.SailNumber,
                    AlternativeSailNumber = preferAlternativeSailNumbers
                        ? competitor.AlternativeSailNumber
                        : null,
                    HomeClubName = showCompetitorClub
                        ? competitor.HomeClubName
                        : null,
                    TotalPoints = score?.TotalScore?.ToString("0.##", CultureInfo.InvariantCulture),
                    Url = BuildCompetitorHtmlUrl(
                        clubInitials,
                        competitorRouteTokens.TryGetValue(competitor.Id, out var competitorRouteToken)
                            ? competitorRouteToken
                            : null),
                };
            })
            .OrderBy(c => c.Rank ?? int.MaxValue)
            .ToList();
    }

    private static List<PublicSeriesCompetitorRaceResultDto> MapCompetitorRaceResults(
        IEnumerable<FlatCalculatedScore> scores)
    {
        return (scores ?? [])
            .OrderBy(s => s.RaceId)
            .Select(s => new PublicSeriesCompetitorRaceResultDto
            {
                RaceId = s.RaceId,
                Place = s.Place,
                Code = s.Code,
                ScoreValue = s.ScoreValue,
                PerfectScoreValue = s.PerfectScoreValue,
                Discard = s.Discard,
                ElapsedTime = s.ElapsedTime
            })
            .ToList();
    }

    private static List<PublicSeriesRaceItemDto> MapRaces(
        IEnumerable<FlatRace> races,
        IEnumerable<FlatSeriesScore> scores,
        string clubInitials,
        bool includeCompetitorResults)
    {
        var scoresByRaceId = (scores ?? [])
            .SelectMany(s => (s.Scores ?? [])
                .Select(r => new { s.CompetitorId, Result = r }))
            .GroupBy(x => x.Result.RaceId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(x => x.Result.Place ?? int.MaxValue)
                    .Select(x => new PublicSeriesRaceCompetitorResultDto
                    {
                        CompetitorId = x.CompetitorId,
                        Place = x.Result.Place,
                        Code = x.Result.Code,
                        ScoreValue = x.Result.ScoreValue,
                        PerfectScoreValue = x.Result.PerfectScoreValue,
                        Discard = x.Result.Discard,
                        ElapsedTime = x.Result.ElapsedTime,
                        FinishTimeUtc = null
                    })
                    .ToList());

        return (races ?? [])
            .OrderBy(r => r.Date)
            .ThenBy(r => r.Order)
            .Select(r => new PublicSeriesRaceItemDto
            {
                Id = r.Id,
                DateUtc = GetUtcOffset(r.Date),
                Order = r.Order,
                State = r.State,
                WindSpeed = r.WindSpeed,
                WindSpeedUnits = r.WindSpeedUnits,
                WindDirectionDegrees = r.WindDirectionDegrees,
                WeatherIcon = r.WeatherIcon,
                Name = r.Name ?? string.Empty,
                Url = BuildRaceApiUrl(clubInitials, r.Id),
                HtmlUrl = BuildRaceHtmlUrl(clubInitials, r.Id),
                CompetitorResults = includeCompetitorResults
                    ? (scoresByRaceId.TryGetValue(r.Id, out var raceResults)
                        ? raceResults
                        : [])
                    : null
            })
            .ToList();
    }

    private static List<PublicSeriesScoreCodeDto> MapScoreCodes(
        IDictionary<string, Core.Scoring.ScoreCodeSummary> scoreCodes)
    {
        return (scoreCodes ?? new Dictionary<string, Core.Scoring.ScoreCodeSummary>())
            .OrderBy(c => c.Key)
            .Select(c => new PublicSeriesScoreCodeDto
            {
                Code = c.Key,
                Description = c.Value?.Description,
                Formula = c.Value?.Formula
            })
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

    private static string BuildClubApiUrl(string clubInitials)
    {
        return $"{ApiBasePath}/clubs/{Escape(clubInitials)}";
    }

    private static string BuildClubSeasonsApiUrl(string clubInitials)
    {
        return $"{ApiBasePath}/clubs/{Escape(clubInitials)}/seasons";
    }

    private static string BuildSeasonSeriesApiUrl(string clubInitials, string seasonUrlName)
    {
        return $"{ApiBasePath}/clubs/{Escape(clubInitials)}/seasons/{Escape(seasonUrlName)}/series";
    }

    private static string BuildSeriesApiUrl(string clubInitials, string seasonUrlName, string seriesUrlName)
    {
        return $"{ApiBasePath}/clubs/{Escape(clubInitials)}/seasons/{Escape(seasonUrlName)}/series/"
            + $"{Escape(seriesUrlName)}";
    }

    private static string BuildRaceApiUrl(string clubInitials, Guid raceId)
    {
        return $"{ApiBasePath}/clubs/{Escape(clubInitials)}/races/{raceId}";
    }

    private static string BuildClubHtmlUrl(string clubInitials)
    {
        return $"/{Escape(clubInitials)}";
    }

    private static string BuildSeriesHtmlUrl(string clubInitials, string seasonUrlName, string seriesUrlName)
    {
        return $"/{Escape(clubInitials)}/{Escape(seasonUrlName)}/{Escape(seriesUrlName)}";
    }

    private static string BuildRaceHtmlUrl(string clubInitials, Guid raceId)
    {
        return $"/{Escape(clubInitials)}/Race/Details/{raceId}";
    }

    private static string GetCompetitorRouteToken(string competitorUrlName, string competitorUrlId)
    {
        return !string.IsNullOrWhiteSpace(competitorUrlName)
            ? competitorUrlName
            : competitorUrlId;
    }

    private static string BuildCompetitorHtmlUrl(string clubInitials, string competitorRouteToken)
    {
        return $"/{Escape(clubInitials)}/Competitor/{Escape(competitorRouteToken)}";
    }

    private static string Escape(string value)
    {
        return Uri.EscapeDataString(value ?? string.Empty);
    }

    private static PublicListResponseDto<T> CreatePagedResponse<T>(IList<T> items, int? page, int? pageSize)
    {
        if (!page.HasValue || !pageSize.HasValue)
        {
            return new PublicListResponseDto<T>
            {
                Items = items
            };
        }

        var totalCount = items.Count;
        var skip = (page.Value - 1) * pageSize.Value;
        if (skip < 0)
        {
            skip = 0;
        }

        var pagedItems = items
            .Skip(skip)
            .Take(pageSize.Value)
            .ToList();

        return new PublicListResponseDto<T>
        {
            Items = pagedItems,
            Pagination = new PublicPaginationDto
            {
                Page = page.Value,
                PageSize = pageSize.Value,
                TotalCount = totalCount
            }
        };
    }
}
