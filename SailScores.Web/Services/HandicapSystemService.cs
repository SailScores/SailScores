using SailScores.Web.Models.SailScores;
using SailScores.Web.Services.Interfaces;

namespace SailScores.Web.Services;

public class HandicapSystemService : IHandicapSystemService
{
    private readonly Core.Services.IHandicapService _coreHandicapService;
    private readonly IMapper _mapper;

    public HandicapSystemService(
        Core.Services.IHandicapService coreHandicapService,
        IMapper mapper)
    {
        _coreHandicapService = coreHandicapService;
        _mapper = mapper;
    }

    public async Task<IList<HandicapSystemSummary>> GetBaseSystemsAsync()
    {
        var systems = await _coreHandicapService.GetBaseHandicapSystemsAsync();
        var summaries = _mapper.Map<IList<HandicapSystemSummary>>(systems);
        return summaries.OrderBy(s => s.Name).ToList();
    }

    public async Task<IList<HandicapSystemSummary>> GetClubSystemsAsync(Guid clubId)
    {
        var clubSystems = await _coreHandicapService.GetHandicapSystemsAsync(clubId);
        var baseSystems = await _coreHandicapService.GetBaseHandicapSystemsAsync();
        var baseById = baseSystems.ToDictionary(s => s.Id, s => s.Name);

        var summaries = _mapper.Map<IList<HandicapSystemSummary>>(clubSystems);
        foreach (var summary in summaries)
        {
            if (summary.ParentSystemId.HasValue &&
                baseById.TryGetValue(summary.ParentSystemId.Value, out var parentName))
            {
                summary.ParentSystemName = parentName;
            }
        }

        return summaries.OrderBy(s => s.Name).ToList();
    }

    public async Task<Guid> CreateClubSystemAsync(CreateHandicapSystemViewModel model)
    {
        var created = await _coreHandicapService.CreateClubHandicapSystemAsync(
            model.ClubId,
            model.ParentSystemId,
            model.Name,
            model.Description);

        return created.Id;
    }
}
