using SailScores.Api.Dtos.Public;

namespace SailScores.Web.Services.Interfaces;

public interface IPublicApiService
{
    PublicApiRootResponseDto GetRootResponse();

    Task<PublicListResponseDto<PublicClubListItemDto>> GetClubsAsync();

    Task<PublicClubDetailResponseDto> GetClubAsync(string clubToken);

    Task<PublicListResponseDto<PublicSeasonListItemDto>> GetSeasonsAsync(string clubToken);

    Task<PublicListResponseDto<PublicSeriesListItemDto>> GetSeriesAsync(
        string clubToken,
        string seasonUrlName = null);

    Task<PublicSeriesDetailResponseDto> GetSeriesDetailAsync(
        string clubInitials,
        string seasonUrlName,
        string seriesUrlName);
}
