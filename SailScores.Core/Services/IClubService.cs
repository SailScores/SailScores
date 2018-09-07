using System.Collections.Generic;

namespace SailScores.Core.Services
{
    public interface IClubService
    {
        IEnumerable<string> GetClubs();
    }
}