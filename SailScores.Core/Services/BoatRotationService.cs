using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SailScores.Core.Model;

namespace SailScores.Core.Services;

public class BoatRotationService : IBoatRotationService
{
    // Stub implementation - to be completed when boat rotation feature is fully designed

    public Task<IList<BoatRotation>> GetRotationsForDateAsync(Guid clubId, DateTime date)
    {
        // TODO: Implement when boat rotation feature is ready
        return Task.FromResult<IList<BoatRotation>>(new List<BoatRotation>());
    }

    public Task SaveRotationAsync(BoatRotation rotation, string userName = "")
    {
        // TODO: Implement when boat rotation feature is ready
        return Task.CompletedTask;
    }

    public Task DeleteRotationAsync(Guid rotationId)
    {
        // TODO: Implement when boat rotation feature is ready
        return Task.CompletedTask;
    }
}
