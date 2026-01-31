using Microsoft.Extensions.Caching.Memory;
using SailScores.Core.Model;
using SailScores.Web.Models.SailScores;
using SailScores.Web.Services.Interfaces;
using AutoMapper;

namespace SailScores.Web.Services;

public class WebSiteAdminService : IWebSiteAdminService
{
    private readonly IMemoryCache _cache;
    private readonly CoreServices.ISeriesService _seriesService;
    private readonly CoreServices.ISiteAdminService _coreSiteAdminService;
    private readonly CoreServices.IClubService _coreClubService;
    private readonly IBackupService _backupService;

    public WebSiteAdminService(
        IMemoryCache cache,
        CoreServices.ISeriesService seriesService,
        CoreServices.ISiteAdminService coreSiteAdminService,
        CoreServices.IClubService coreClubService,
        IBackupService backupService)
    {
        _cache = cache;
        _seriesService = seriesService;
        _coreSiteAdminService = coreSiteAdminService;
        _coreClubService = coreClubService;
        _backupService = backupService;
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
            Series = club.Series?
                .OrderByDescending(s => s.Season?.Start)
                .ThenBy(s => s.Name)
                .ToList(),
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

    public async Task<(byte[] Data, string FileName)> BackupClubAsync(string clubInitials, string createdBy)
    {
        return await _backupService.CreateBackupFileAsync(clubInitials, createdBy);
    }

    public async Task ResetClubAsync(Guid clubId, ResetLevel resetLevel)
    {
        await _coreClubService.ResetClubAsync(clubId, resetLevel);

        // Clear cache after reset
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
