using Microsoft.Extensions.Caching.Memory;
using SailScores.Api.Enumerations;
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
    private readonly CoreServices.IScoringService _coreScoringService;

    public WebSiteAdminService(
        IMemoryCache cache,
        CoreServices.ISeriesService seriesService,
        CoreServices.ISiteAdminService coreSiteAdminService,
        CoreServices.IClubService coreClubService,
        IBackupService backupService,
        CoreServices.IScoringService coreScoringService)
    {
        _coreScoringService = coreScoringService;
        _cache = cache;
        _seriesService = seriesService;
        _coreSiteAdminService = coreSiteAdminService;
        _coreClubService = coreClubService;
        _backupService = backupService;
    }

    public async Task<SiteAdminIndexViewModel> GetAllClubsAsync(string? sortOrder = null)
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

        var currentSort = string.IsNullOrWhiteSpace(sortOrder)
            ? SiteAdminSortOrders.UpdateDesc
            : sortOrder;

        var viewModel = new SiteAdminIndexViewModel
        {
            CurrentSort = currentSort,
            NameSortParm = currentSort == SiteAdminSortOrders.NameAsc ? SiteAdminSortOrders.NameDesc : SiteAdminSortOrders.NameAsc,
            UpdateSortParm = currentSort == SiteAdminSortOrders.UpdateAsc ? SiteAdminSortOrders.UpdateDesc : SiteAdminSortOrders.UpdateAsc,
            RaceSortParm = currentSort == SiteAdminSortOrders.RaceAsc ? SiteAdminSortOrders.RaceDesc : SiteAdminSortOrders.RaceAsc,
            Clubs = clubSummaries
        };

        viewModel.Clubs = currentSort switch
        {
            SiteAdminSortOrders.NameAsc => viewModel.Clubs.OrderBy(c => c.Name).ToList(),
            SiteAdminSortOrders.NameDesc => viewModel.Clubs.OrderByDescending(c => c.Name).ToList(),
            SiteAdminSortOrders.UpdateAsc => viewModel.Clubs.OrderBy(c => c.LatestSeriesUpdate).ToList(),
            SiteAdminSortOrders.UpdateDesc => viewModel.Clubs.OrderByDescending(c => c.LatestSeriesUpdate).ToList(),
            SiteAdminSortOrders.RaceAsc => viewModel.Clubs.OrderBy(c => c.LatestRaceDate).ToList(),
            SiteAdminSortOrders.RaceDesc => viewModel.Clubs.OrderByDescending(c => c.LatestRaceDate).ToList(),
            _ => viewModel.Clubs.OrderByDescending(c => c.LatestSeriesUpdate).ToList(),
        };

        return viewModel;
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

    public async Task<SiteAdminScoringSystemsViewModel> GetScoringSystemsAsync()
    {
        var systems = await _coreScoringService.GetSharedScoringSystemAccessAsync();
        var clubs = (await _coreClubService.GetClubs(true))
            .ToDictionary(c => c.Id);

        return new SiteAdminScoringSystemsViewModel
        {
            Systems = systems.Select(s => new SiteAdminScoringSystemSummary
            {
                Id = s.Id,
                Name = s.Name,
                ParentSystemName = s.ParentSystemName,
                CalculatorName = GetCalculatorName(s.CalculatorType),
                IsUsedForNewClubs = s.IsUsedForNewClubs,
                IsRestrictedToClubs = s.IsRestrictedToClubs,
                ClubNames = s.ClubIds
                    .Where(clubs.ContainsKey)
                    .Select(id => $"{clubs[id].Name} ({clubs[id].Initials})")
                    .OrderBy(n => n)
                    .ToList()
            }).ToList()
        };
    }

    public async Task<SiteAdminScoringSystemAccessViewModel> GetScoringSystemAccessAsync(Guid scoringSystemId)
    {
        var system = (await _coreScoringService.GetSharedScoringSystemAccessAsync())
            .FirstOrDefault(s => s.Id == scoringSystemId);
        if (system == null)
        {
            return null;
        }

        var clubs = await _coreClubService.GetClubs(true);
        return new SiteAdminScoringSystemAccessViewModel
        {
            Id = system.Id,
            Name = system.Name,
            ParentSystemName = system.ParentSystemName,
            CalculatorName = GetCalculatorName(system.CalculatorType),
            IsUsedForNewClubs = system.IsUsedForNewClubs,
            IsRestrictedToClubs = system.IsRestrictedToClubs,
            SelectedClubIds = system.ClubIds.ToList(),
            ClubOptions = clubs
                .OrderBy(c => c.Name)
                .Select(c => new SiteAdminClubOption
                {
                    Id = c.Id,
                    Name = c.Name,
                    Initials = c.Initials,
                    IsHidden = c.IsHidden
                }).ToList()
        };
    }

    public Task SaveScoringSystemAccessAsync(SiteAdminScoringSystemAccessViewModel model)
    {
        return _coreScoringService.SaveSharedScoringSystemAccessAsync(
            model.Id,
            model.IsRestrictedToClubs,
            model.SelectedClubIds ?? new List<Guid>());
    }

    private static string? GetCalculatorName(ScoringCalculatorType? calculatorType)
    {
        return calculatorType.HasValue
            ? EnumHelper<ScoringCalculatorType>.GetDisplayValue(calculatorType.Value)
            : null;
    }
}
