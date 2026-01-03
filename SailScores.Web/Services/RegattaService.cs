using Microsoft.Extensions.Caching.Memory;
using SailScores.Core.Model;
using SailScores.Web.Models.SailScores;
using SailScores.Web.Services.Interfaces;

namespace SailScores.Web.Services;

public class RegattaService : IRegattaService
{
    private readonly Core.Services.IClubService _clubService;
    private readonly Core.Services.IRegattaService _coreRegattaService;
    private readonly Core.Services.IScoringService _coreScoringService;
    private readonly Core.Services.ISeasonService _coreSeasonService;
    private readonly Core.Services.IFleetService _coreFleetService;
    private readonly IMemoryCache _cache;
    private readonly IMapper _mapper;

    public RegattaService(
        Core.Services.IClubService clubService,
        Core.Services.IRegattaService coreRegattaService,
        Core.Services.IScoringService coreScoringService,
        Core.Services.ISeasonService coreSeasonService,
        Core.Services.IFleetService coreFleetService,
        IMemoryCache cache,
        IMapper mapper)
    {
        _clubService = clubService;
        _coreRegattaService = coreRegattaService;
        _coreScoringService = coreScoringService;
        _coreSeasonService = coreSeasonService;
        _coreFleetService = coreFleetService;
        _cache = cache;
        _mapper = mapper;
    }

    public async Task<IEnumerable<RegattaSummaryViewModel>> GetAllRegattaSummaryAsync(string clubInitials)
    {
        var clubId = await _clubService.GetClubId(clubInitials);
        var regattas = await _coreRegattaService.GetAllRegattasAsync(clubId);
        var orderedRegattas = regattas
            .OrderByDescending(s => s.Season.Start)
            .ThenBy(s => s.StartDate)
            .ThenBy(s => s.Name);
        return _mapper.Map<IList<RegattaSummaryViewModel>>(orderedRegattas);
    }

    public async Task<IEnumerable<RegattaSummaryViewModel>> GetCurrentRegattas()
    {
        if (_cache.TryGetValue("CurrentRegattas", out IEnumerable<RegattaSummaryViewModel> cachedRegattas))
        {
            return cachedRegattas;
        }
        var start = DateTime.Today.AddDays(-7);
        var end = DateTime.Today.AddDays(7);

        var coreRegattas = await _coreRegattaService.GetRegattasDuringSpanAsync(start, end)
            .ConfigureAwait(false);
        var filteredRegattas = coreRegattas
            .Where(r => r.HideFromFrontPage == false)
            // include only regattas that last for less than 8 days
            .Where(r =>
                r.StartDate.HasValue &&
                (!r.EndDate.HasValue ||
                r.EndDate.Value < r.StartDate.Value.AddDays(8)))
            .OrderBy(s => s.StartDate)
            .ThenBy(s => s.Name);
        var vm = _mapper.Map<IList<RegattaSummaryViewModel>>(filteredRegattas);
        var regattasToRemove = new List<RegattaSummaryViewModel>();
        foreach (var regatta in vm)
        {
            var club = await _clubService.GetMinimalClub(regatta.ClubId);
            if (club.IsHidden)
            {
                regattasToRemove.Add(regatta);
            }
            regatta.ClubInitials = club.Initials;
            regatta.ClubName = club.Name;
            regatta.ClubLogoFileId = club.LogoFileId;
        }
        var returnObject = vm.Except(regattasToRemove);
        _cache.Set("CurrentRegattas", returnObject, TimeSpan.FromMinutes(5));
        return returnObject;
    }

    public async Task<Regatta> GetRegattaAsync(Guid regattaId)
    {
        return await _coreRegattaService.GetRegattaAsync(regattaId);
    }

    public async Task<Regatta> GetRegattaAsync(string clubInitials, string season, string regattaName)
    {
        return await _coreRegattaService.GetRegattaAsync(clubInitials, season, regattaName);
    }

    public async Task<Guid> SaveNewAsync(RegattaWithOptionsViewModel model)
    {
        await PrepRegattaVmAsync(model);
        return await _coreRegattaService.SaveNewRegattaAsync(model);
    }

    private async Task PrepRegattaVmAsync(RegattaWithOptionsViewModel model)
    {
        if (model.StartDate.HasValue)
        {
            var seasons = await _coreSeasonService.GetSeasons(model.ClubId);
            model.Season = seasons.Single(s =>
                s.Start <= model.StartDate.Value
                && s.End >= model.StartDate.Value);
        }
        if (model.ScoringSystemId == Guid.Empty)
        {
            model.ScoringSystemId = null;
        }
        model.Fleets = new List<Fleet>();
        var fleets = await _coreFleetService.GetAllFleetsForClub(model.ClubId);
        if (model.FleetIds != null)
        {
            foreach (var fleetId in model.FleetIds)
            {
                model.Fleets.Add(fleets
                    .Single(f => f.Id == fleetId));
            }
        }
    }

    public async Task<Guid> UpdateAsync(RegattaWithOptionsViewModel model)
    {
        await PrepRegattaVmAsync(model);
        return await _coreRegattaService.UpdateAsync(model);
    }

    public async Task DeleteAsync(Guid regattaId)
    {
        await _coreRegattaService.DeleteAsync(regattaId);
    }

    public async Task AddFleetToRegattaAsync(Guid fleetId, Guid regattaId)
    {
        await _coreRegattaService.AddFleetToRegattaAsync(fleetId, regattaId);
    }

    public async Task<RegattaWithOptionsViewModel> GetBlankRegattaWithOptions(Guid clubId)
    {
        var vm = new RegattaWithOptionsViewModel
        {
            SeasonOptions = await _coreSeasonService.GetSeasons(clubId),
            FleetOptions = await _coreFleetService.GetAllFleetsForClub(clubId)
        };
        var scoringSystemOptions = await _coreScoringService.GetScoringSystemsAsync(clubId, false);
        scoringSystemOptions.Add(new ScoringSystem
        {
            Id = Guid.Empty,
            Name = "<Use Club Default>"
        });
        vm.ScoringSystemOptions = scoringSystemOptions.OrderBy(s => s.Name).ToList();

        return vm;

    }

    public async Task<Regatta> GetRegattaForFleet(Guid fleetId)
    {
        return await _coreRegattaService.GetRegattaForFleet(fleetId);
    }

    public async Task<bool> ClubHasRegattasAsync(string clubInitials)
    {
        var cacheKey = $"ClubHasRegattas_{clubInitials}";
        if (_cache.TryGetValue(cacheKey, out bool hasRegattas))
        {
            return hasRegattas;
        }

        var clubId = await _clubService.GetClubId(clubInitials);
        if (clubId == Guid.Empty)
        {
            return false;
        }

        hasRegattas = await _coreRegattaService.ClubHasRegattasAsync(clubId);
        
        _cache.Set(cacheKey, hasRegattas, TimeSpan.FromMinutes(5));
        return hasRegattas;
    }
}
