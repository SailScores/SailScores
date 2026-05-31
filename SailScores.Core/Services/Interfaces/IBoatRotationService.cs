using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SailScores.Core.Model;

namespace SailScores.Core.Services;

public interface IBoatRotationService
{
    Task<IList<BoatRotation>> GetRotationsForDateAsync(Guid clubId, DateTime date);
    Task SaveRotationAsync(BoatRotation rotation, string userName = "");
    Task DeleteRotationAsync(Guid rotationId);
}
