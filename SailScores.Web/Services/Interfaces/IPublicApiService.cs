using SailScores.Api.Dtos.Public;

namespace SailScores.Web.Services.Interfaces;

public interface IPublicApiService
{
    PublicApiRootResponseDto GetRootResponse();

    Task<PublicListResponseDto<PublicClubListItemDto>> GetClubsAsync(
        int? page = null,
        int? pageSize = null);

    Task<PublicClubDetailResponseDto> GetClubAsync(string clubToken);

    Task<PublicListResponseDto<PublicSeasonListItemDto>> GetSeasonsAsync(
        string clubToken,
        int? page = null,
        int? pageSize = null);

    Task<PublicListResponseDto<PublicSeriesListItemDto>> GetSeriesAsync(
        string clubToken,
        string seasonUrlName = null,
        int? page = null,
        int? pageSize = null);

    Task<PublicSeriesDetailResponseDto> GetSeriesDetailAsync(
        string clubInitials,
        string seasonUrlName,
        string seriesUrlName);
}
