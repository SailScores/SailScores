using Microsoft.Extensions.Caching.Memory;
using SailScores.Web.Models.SailScores;
using SailScores.Web.Services.Interfaces;
using Newtonsoft.Json;
using AutoMapper;
using SailScores.Core.Model;

namespace SailScores.Web.Services;

public class WebSiteAdminService : IWebSiteAdminService
{
    private readonly IMemoryCache _cache;
    private readonly IMapper _mapper;
    private readonly CoreServices.ISeriesService _seriesService;
    private readonly CoreServices.ISiteAdminService _coreSiteAdminService;

    public WebSiteAdminService(
        IMemoryCache cache,
        IMapper mapper,
        CoreServices.ISeriesService seriesService,
        CoreServices.ISiteAdminService coreSiteAdminService)
    {
        _cache = cache;
        _mapper = mapper;
        _seriesService = seriesService;
        _coreSiteAdminService = coreSiteAdminService;
    }

    public async Task<SiteAdminIndexViewModel> GetAllClubsAsync()
    {
        var clubsWithDates = await _coreSiteAdminService.GetAllClubsWithDatesAsync();

        var clubSummaries = clubsWithDates.Select(c => new SiteAdminClubSummary
        {
            Id = c.club.Id,
            Name = c.club.Name,
            Initials = c.club.Initials,
            IsHidden = c.club.IsHidden,
            LatestSeriesUpdate = c.latestSeriesUpdate,
            LatestRaceDate = c.latestRaceDate
        }).ToList();

        return new SiteAdminIndexViewModel
        {
            Clubs = clubSummaries
        };
    }

    public async Task<SiteAdminClubDetailsViewModel> GetClubDetailsAsync(string clubInitials)
    {
        var club = await _coreSiteAdminService.GetClubDetailsAsync(clubInitials);

        if (club == null)
        {
            return null;
        }

        var vm = new SiteAdminClubDetailsViewModel
        {
            Id = club.Id,
            Name = club.Name,
            Initials = club.Initials,
            IsHidden = club.IsHidden,
            Series = club.Series,
            LatestSeriesUpdate = club.Series?
                .OrderByDescending(s => s.UpdatedDate)
                .Select(s => s.UpdatedDate)
                .FirstOrDefault(),
            LatestRaceDate = club.Races?
                .OrderByDescending(r => r.Date)
                .Select(r => r.Date)
                .FirstOrDefault(),
            RaceCount = club.Races?.Count ?? 0
        };

        return vm;
    }

    public Task ResetClubInitialsCacheAsync()
    {
        // IMemoryCache doesn't support selective removal. Full cache clear required.
        if (_cache is MemoryCache memoryCache)
        {
            memoryCache.Compact(1.0);
        }
        return Task.CompletedTask;
    }

    public async Task<string> BackupClubAsync(Guid clubId)
    {
        var club = await _coreSiteAdminService.GetFullClubForBackupAsync(clubId);

        if (club == null)
        {
            return null;
        }

        // Serialize to JSON
        var settings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Formatting = Formatting.Indented
        };

        return JsonConvert.SerializeObject(club, settings);
    }

    public async Task ResetClubAsync(Guid clubId)
    {
        await _coreSiteAdminService.ResetClubAsync(clubId);

        // Clear cache for this club  
        // Note: We need the club initials to clear the specific cache entry
        // For now, clear entire cache
        if (_cache is MemoryCache memoryCache)
        {
            memoryCache.Compact(1.0);
        }
    }

    public async Task RecalculateSeriesAsync(Guid seriesId, string updatedBy)
    {
        await _seriesService.UpdateSeriesResults(seriesId, updatedBy);
    }
}
