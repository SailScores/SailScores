using Microsoft.Extensions.Caching.Memory;
using SailScores.Core.Model;
using SailScores.Web.Models.SailScores;
using SailScores.Web.Services.Interfaces;

namespace SailScores.Web.Services;

public class ClubService : IClubService
{
    private readonly Core.Services.IClubService _coreClubService;
    private readonly Core.Services.ISeasonService _coreSeasonService;
    private readonly Core.Services.IRaceService _coreRaceService;
    private readonly Core.Services.ISeriesService _coreSeriesService;
    private readonly Core.Services.IRegattaService _coreRegattaService;
    private readonly IMemoryCache _cache;
    private readonly IMapper _mapper;

    public ClubService(
        Core.Services.IClubService clubService,
        Core.Services.ISeasonService seasonService,
        Core.Services.IRaceService raceService,
        Core.Services.ISeriesService seriesService,
        Core.Services.IRegattaService regattaService,
        IMemoryCache cache,
        IMapper mapper)
    {
        _coreClubService = clubService;
        _coreSeasonService = seasonService;
        _coreRaceService = raceService;
        _coreSeriesService = seriesService;
        _coreRegattaService = regattaService;
        _cache = cache;
        _mapper = mapper;
    }

    public async Task<IEnumerable<AllClubStatsViewModel>> GetAllClubStats()
    {
        var coreEnumeration = await _coreClubService.GetAllStats();
        return _mapper.Map<IEnumerable<AllClubStatsViewModel>>(coreEnumeration);
    }


    public async Task<Club> GetClubForClubHome(string clubInitials)
    {
        var clubId = await _coreClubService.GetClubId(clubInitials);
        var club = await _coreClubService.GetMinimalClub(clubId);
        // 10 days back, but view filters down to 8 days back.
        club.Races = await _coreRaceService.GetRecentRacesAsync(clubId, 10);
        club.Series = await _coreSeriesService.GetAllSeriesAsync(clubId, null, false, true);
        club.Regattas = await _coreRegattaService.GetAllRegattasAsync(clubId);
        return club;
    }

    public async Task<ClubStatsViewModel> GetClubStats(string clubInitials)
    {
        var seasonStats = await _coreClubService.GetClubStats(clubInitials);
        var firstSeason = seasonStats.FirstOrDefault();
        var club = await _coreClubService.GetMinimalClub(clubInitials);

        ClubStatsViewModel returnObj;
        if (seasonStats != null && firstSeason != null)
        {
            returnObj = new ClubStatsViewModel
            {
                Initials = firstSeason.ClubInitials,
                Name = firstSeason.ClubName,
                SeasonStats = _mapper.Map<IEnumerable<ClubSeasonStatsViewModel>>(seasonStats)
            };
        } else
        {
            returnObj = new ClubStatsViewModel
            {
                Initials = club.Initials,
                Name = club.Name,
                SeasonStats = new List<ClubSeasonStatsViewModel>()
            };
        }
        return returnObj;
    }

    public Task<System.Guid> GetClubId(string initials)
    {
        return _coreClubService.GetClubId(initials);
    }

    public async Task SetUseAdvancedFeaturesAsync(Guid clubId, bool enabled)
    {
        await _coreClubService.SetUseAdvancedFeaturesAsync(clubId, enabled);
    }

    public async Task SetSubscriptionTypeAsync(Guid clubId, string subscriptionType)
    {
        await _coreClubService.SetSubscriptionTypeAsync(clubId, subscriptionType);
    }

    public async Task<Club> GetClubByIdAsync(Guid clubId)
    {
        return await _coreClubService.GetMinimalClub(clubId);
    }

    public async Task<Club> GetMinimalClubAsync(string clubInitials)
    {
        var cacheKey = $"MinimalClub_{clubInitials}";
        if (_cache.TryGetValue(cacheKey, out Club cachedClub))
        {
            return cachedClub;
        }

        var club = await _coreClubService.GetMinimalClub(clubInitials);
        _cache.Set(cacheKey, club, TimeSpan.FromMinutes(5));
        return club;
    }
}
