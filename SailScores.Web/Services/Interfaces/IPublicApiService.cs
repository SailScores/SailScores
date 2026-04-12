using SailScores.Api.Dtos.Public;

namespace SailScores.Web.Services.Interfaces;

public interface IPublicApiService
{
    PublicApiRootResponseDto GetRootResponse();

    Task<PublicSeriesDetailResponseDto> GetSeriesDetailAsync(
        string clubInitials,
        string seasonUrlName,
        string seriesUrlName);
}
