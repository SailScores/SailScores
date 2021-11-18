using SailScores.Core.Model;
using SailScores.Web.Models.SailScores;
using SailScores.Core.Services;
using ICompetitorService = SailScores.Web.Services.Interfaces.ICompetitorService;

namespace SailScores.Web.Services;

public class CompetitorService : ICompetitorService
{
    private readonly Core.Services.IClubService _coreClubService;
    private readonly Core.Services.ICompetitorService _coreCompetitorService;
    private readonly IMapper _mapper;

    public CompetitorService(
        Core.Services.IClubService clubService,
        Core.Services.ICompetitorService competitorService,
        IMapper mapper)
    {
        _coreClubService = clubService;
        _coreCompetitorService = competitorService;
        _mapper = mapper;
    }

    public async Task DeleteCompetitorAsync(Guid competitorId)
    {
        await _coreCompetitorService.DeleteCompetitorAsync(competitorId);
    }

    public Task<Competitor> GetCompetitorAsync(Guid competitorId)
    {
        var comp = _coreCompetitorService.GetCompetitorAsync(competitorId);
        return comp;
    }

    public async Task<Competitor> GetCompetitorAsync(string clubInitials, string sailNumber)
    {
        var clubId = await _coreClubService.GetClubId(clubInitials);
        var comps = await _coreCompetitorService.GetCompetitorsAsync(clubId, null, false);
        return comps.FirstOrDefault(c => String.Equals(c.SailNumber, sailNumber, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<CompetitorStatsViewModel> GetCompetitorStatsAsync(
        string clubInitials,
        string sailor)
    {
        // sailor will usually be sailNumber but falls back to name if no number.
        var clubId = await _coreClubService.GetClubId(clubInitials);
        var comps = await _coreCompetitorService.GetCompetitorsAsync(clubId, null, true);
        IList<Competitor> inactiveCompetitors;

        var comp = comps.Where(c => c.IsActive)
            .FirstOrDefault(c => String.Equals(c.SailNumber, sailor, StringComparison.OrdinalIgnoreCase));
        if (comp == null)
        {
            // check for inactive.
            comp = comps.Where(c => !c.IsActive)
                .FirstOrDefault(c =>
                    String.Equals(c.SailNumber, sailor, StringComparison.OrdinalIgnoreCase));
        }
        comp ??= comps.FirstOrDefault(c =>
            String.Equals(UrlUtility.GetUrlName(c.Name), sailor, StringComparison.OrdinalIgnoreCase));
        if (comp == null)
        {
            return null;
        }

        var vm = _mapper.Map<CompetitorStatsViewModel>(comp);

        vm.SeasonStats = await _coreCompetitorService.GetCompetitorStatsAsync(clubId, comp.Id);

        return vm;
    }

    public async Task<IList<PlaceCount>> GetCompetitorSeasonRanksAsync(
        Guid competitorId,
        string seasonUrlName)
    {

        var seasonStats = await _coreCompetitorService.GetCompetitorSeasonRanksAsync(
            competitorId,
            seasonUrlName);

        var vm = seasonStats;

        return vm;
    }

    public async Task SaveAsync(
        MultipleCompetitorsWithOptionsViewModel vm,
        Guid clubId)
    {
        var coreCompetitors = new List<Core.Model.Competitor>();
        var fleets = (await _coreClubService.GetMinimalForSelectedBoatsFleets(clubId))
            .OrderBy(f => f.Name);
        foreach (var comp in vm.Competitors)
        {
            // if they didn't give a name or sail, skip this row.
            if (String.IsNullOrWhiteSpace(comp.Name)
                && String.IsNullOrWhiteSpace(comp.SailNumber)
            )
            {
                continue;
            }
            var currentComp = _mapper.Map<Core.Model.Competitor>(comp);
            currentComp.ClubId = clubId;
            currentComp.Fleets = new List<Fleet>();
            currentComp.BoatClassId = vm.BoatClassId;

            if (vm.FleetIds != null)
            {
                foreach (var fleetId in vm.FleetIds)
                {
                    currentComp.Fleets.Add(fleets.Single(f => f.Id == fleetId));
                }
            }
            coreCompetitors.Add(currentComp);
        }

        foreach (var comp in coreCompetitors)
        {
            await _coreCompetitorService.SaveAsync(comp);
        }
    }

    public async Task SaveAsync(CompetitorWithOptionsViewModel competitor)
    {

        if (competitor.Fleets == null)
        {
            competitor.Fleets = new List<Fleet>();
        }
        if (competitor.FleetIds == null)
        {
            competitor.FleetIds = new List<Guid>();
        }

        var fleets = (await _coreClubService.GetMinimalForSelectedBoatsFleets(
                competitor.ClubId))
            .Where(f => f.FleetType == Api.Enumerations.FleetType.SelectedBoats);

        foreach (var fleetId in competitor.FleetIds)
        {
            var fleet = fleets.SingleOrDefault(f => f.Id == fleetId);
            if (fleet != null && !competitor.Fleets.Any(f => f.Id == fleet.Id))
            {
                competitor.Fleets.Add(fleet);
            }
        }
        await _coreCompetitorService.SaveAsync(competitor);
    }

    public async Task<Guid?> GetCompetitorIdForSailnumberAsync(Guid clubId, string sailNumber)
    {
        var competitor = await _coreCompetitorService.GetCompetitorBySailNumberAsync(clubId, sailNumber);
        return competitor?.Id;
    }

    public async Task<IList<Competitor>> GetCompetitorsAsync(Guid clubId, bool includeInactive)
    {
        var comps = await _coreCompetitorService.GetCompetitorsAsync(clubId, null, includeInactive);
        
        return comps;
    }

    public async Task<IList<Competitor>> GetCompetitorsAsync(string clubInitials, bool includeInactive)
    {
        var clubId = await _coreClubService.GetClubId(clubInitials);
        return await GetCompetitorsAsync(clubId, includeInactive);

    }


    public async Task<IList<CompetitorIndexViewModel>> GetCompetitorsWithDeletableInfoAsync(String clubInitials, bool includeInactive)
    {
        var clubId = await _coreClubService.GetClubId(clubInitials);
        var list = await GetCompetitorsAsync(clubId, includeInactive);
        var vmList = _mapper.Map<IList<CompetitorIndexViewModel>>(list);
        if (includeInactive)
        {
            await PopulateDeletableFields(clubId, vmList);
        }
        return vmList;
    }

    private async Task PopulateDeletableFields(Guid clubId, IEnumerable<CompetitorIndexViewModel> vmList)
    {
        var deletableReasons = await _coreCompetitorService.GetDeletableInfo(clubId);

        foreach (var info in deletableReasons)
        {
            var curComp = vmList.SingleOrDefault(c => c.Id == info.Id);
            if (curComp != null) { }
                curComp.IsDeletable = info.IsDeletable;
                curComp.PreventDeleteReason = info.Reason;
            }
        }
    }

    public async Task<IEnumerable<KeyValuePair<string, string>>> GetSaveErrors(Competitor competitor)
    {
        //These errors will prevent the competitor from being saved.
        var errors = new List<KeyValuePair<string, string>>();
        var existingCompetitors = await GetCompetitorsAsync(competitor.ClubId, true);
        var existingThisCompetitor = existingCompetitors.SingleOrDefault(c => c.Id == competitor.Id);
        var otherCompetitors = existingCompetitors.Where(c => c.Id != competitor.Id);

        // If this competitor is active, they can't have a non-empty primary
        // sail number that matches another in the same class.
        // but we allow it if the sailnumber is not changing.
        if (!string.IsNullOrWhiteSpace(competitor.SailNumber)
            && competitor.IsActive)
        {
            if (otherCompetitors.Any(c => c.SailNumber == competitor.SailNumber
                                          && c.BoatClassId == competitor.BoatClassId
                                          && c.IsActive)
                && competitor.SailNumber != existingThisCompetitor.SailNumber)
            {
                errors.Add(new KeyValuePair<string, string>("SailNumber",
                    "Sail number is already assigned to an active competitor in this class."));
            }
        }

        if(otherCompetitors.Any(c => c.SailNumber == competitor.SailNumber
                                     && c.AlternativeSailNumber == competitor.AlternativeSailNumber
                                     && c.Name == competitor.Name
                                     && c.BoatClassId == competitor.BoatClassId
                                     && c.Notes == competitor.Notes))
        {
            errors.Add(new KeyValuePair<string, string>(String.Empty,
                "Duplicate competitors within a class are not allowed. " +
                " Change the Sail Number, Alternative Sail Number, or" +
                " competitor name to be unique."));
        }

        return errors;
    }
}