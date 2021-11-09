using SailScores.Core.Model;
using SailScores.Web.Models.SailScores;
using SailScores.Web.Services.Interfaces;

namespace SailScores.Web.Services;

public class FleetService : IFleetService
{
    private readonly Core.Services.IClubService _coreClubService;
    private readonly Core.Services.IFleetService _coreFleetService;
    private readonly Core.Services.ICompetitorService _coreCompetitorService;
    private readonly IRegattaService _regattaService;
    private readonly IMapper _mapper;

    public FleetService(
        Core.Services.IClubService clubService,
        Core.Services.IFleetService coreFleetService,
        Core.Services.ICompetitorService coreCompetitorService,
        IRegattaService regattaService,
        IMapper mapper)
    {
        _coreClubService = clubService;
        _coreFleetService = coreFleetService;
        _coreCompetitorService = coreCompetitorService;
        _regattaService = regattaService;
        _mapper = mapper;
    }

    public async Task<IList<FleetSummary>> GetAllFleetSummary(string clubInitials)
    {
        var clubId = await _coreClubService.GetClubId(clubInitials);
        var coreFleets = await _coreFleetService.GetAllFleetsForClub(clubId);

        return _mapper.Map<IList<FleetSummary>>(coreFleets);
    }

    public async Task<FleetSummary> GetFleet(string clubInitials, string fleetShortName)
    {
        var clubId = await _coreClubService.GetClubId(clubInitials);
        var allFleets = await _coreFleetService.GetAllFleetsForClub(clubId);

        var retFleet = _mapper.Map<FleetSummary>(allFleets.First(f => f.ShortName == fleetShortName));

        var series = await _coreFleetService.GetSeriesForFleet(retFleet.Id);
        retFleet.Series = series.ToList();

        return retFleet;
    }
    public async Task<Fleet> GetFleet(Guid fleetId)
    {
        var coreObject = await _coreFleetService.Get(fleetId);
        return coreObject;
    }

    public async Task Delete(Guid fleetId)
    {
        await _coreFleetService.Delete(fleetId);
    }

    public async Task SaveNew(FleetWithOptionsViewModel fleet)
    {
        var coreModel = _mapper.Map<Fleet>(fleet);
        if (fleet.FleetType == Api.Enumerations.FleetType.SelectedClasses
            && fleet.BoatClassIds != null)
        {
            coreModel.BoatClasses =
                (await _coreClubService.GetAllBoatClasses(fleet.ClubId))
                .Where(c => fleet.BoatClassIds.Contains(c.Id))
                .ToList();
        }
        else if (fleet.FleetType == Api.Enumerations.FleetType.SelectedBoats
                 && fleet.CompetitorIds != null)
        {
            coreModel.Competitors =
                (await _coreCompetitorService.GetCompetitorsAsync(fleet.ClubId, null, false))
                .Where(c => fleet.CompetitorIds.Contains(c.Id))
                .ToList();
        }
        var fleetId = await _coreFleetService.SaveNew(coreModel);
        if (fleet.RegattaId.HasValue)
        {
            await _regattaService.AddFleetToRegattaAsync(fleetId, fleet.RegattaId.Value);
        }
    }

    public async Task Update(FleetWithOptionsViewModel fleet)
    {
        var coreModel = _mapper.Map<Fleet>(fleet);
        if (fleet.FleetType == Api.Enumerations.FleetType.SelectedClasses
            && fleet.BoatClassIds != null)
        {
            coreModel.BoatClasses =
                (await _coreClubService.GetAllBoatClasses(fleet.ClubId))
                .Where(c => fleet.BoatClassIds.Contains(c.Id))
                .ToList();
        }
        else if (fleet.FleetType == Api.Enumerations.FleetType.SelectedBoats
                 && fleet.CompetitorIds != null)
        {
            coreModel.Competitors =
                (await _coreCompetitorService.GetCompetitorsAsync(fleet.ClubId, null, false))
                .Where(c => fleet.CompetitorIds.Contains(c.Id))
                .ToList();
        }
        await _coreFleetService.Update(coreModel);
    }

    public async Task<FleetWithOptionsViewModel> GetBlankFleetWithOptionsAsync(
        string clubInitials,
        Guid? regattaId)
    {
        var clubId = await _coreClubService.GetClubId(clubInitials);
        var vm = new FleetWithOptionsViewModel();
        vm.ClubId = clubId;
        vm.BoatClassOptions = await _coreClubService.GetAllBoatClasses(clubId);
        vm.CompetitorBoatClassOptions = vm.BoatClassOptions.OrderBy(c => c.Name);
        vm.CompetitorOptions =
            await _coreCompetitorService.GetCompetitorsAsync(clubId, null, true);

        vm.RegattaId = regattaId;
        vm.IsActive = true;
        if (regattaId.HasValue)
        {
            var regatta = await _regattaService.GetRegattaAsync(regattaId.Value);
            vm.Regatta = _mapper.Map<RegattaSummaryViewModel>(regatta);
        }
        return vm;
    }
}