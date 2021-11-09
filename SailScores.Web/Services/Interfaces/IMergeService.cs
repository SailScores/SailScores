using SailScores.Core.Model;

namespace SailScores.Web.Services.Interfaces;

public interface IMergeService
{
    Task<IList<Competitor>> GetSourceOptionsFor(Guid? targetCompetitorId);
    Task<int?> GetNumberOfRaces(Guid? competitorId);
    Task<IList<Season>> GetSeasons(Guid? competitorId);
    Task Merge(
        Guid? targetCompetitorId,
        Guid? sourceCompetitorId,
        string userName);
}