using System;
using System.Threading.Tasks;

namespace SailScores.Core.Services
{
    public interface IIndexNowService
    {
        Task NotifySeriesUpdate(
            Guid clubId,
            string clubInitials,
            string seasonUrlName,
            string seriesUrlName,
            bool isClubHidden);
    }
}
