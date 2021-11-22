using SailScores.Core.Model;
using SailScores.Web.Models.SailScores;
using SailScores.Web.Services.Interfaces;

namespace SailScores.Web.Services;

public class BoatClassService : IBoatClassService
{
    private readonly Core.Services.IBoatClassService _coreBoatClassService;
    private readonly IMapper _mapper;

    public BoatClassService(
        Core.Services.IBoatClassService coreBoatClassService,
        IMapper mapper)
    {
        _coreBoatClassService = coreBoatClassService;
        _mapper = mapper;
    }

    public Task Delete(Guid boatClassId)
    {
        return _coreBoatClassService.Delete(boatClassId);
    }

    public Task<BoatClass> GetClass(Guid boatClassId)
    {
        return _coreBoatClassService.GetClass(boatClassId);
    }

    public async Task<BoatClassDeleteViewModel> GetClassDeleteViewModel(Guid id)
    {
        var boatClass = await GetClass(id);
        var vm = _mapper.Map<BoatClassDeleteViewModel>(boatClass);
        var deletableInfo = await _coreBoatClassService.GetDeletableInfo(id);
        vm.IsDeletable = deletableInfo.IsDeletable;
        vm.PreventDeleteReason = deletableInfo.Reason;

        return vm;
    }

    public Task SaveNew(BoatClass boatClass)
    {
        return _coreBoatClassService.SaveNew(boatClass);
    }

    public Task Update(BoatClass boatClass)
    {
        return _coreBoatClassService.Update(boatClass);
    }
}