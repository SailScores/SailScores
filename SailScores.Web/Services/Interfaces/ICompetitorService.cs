using SailScores.Core.Model;
using SailScores.Web.Models.SailScores;

namespace SailScores.Web.Services.Interfaces;

public interface ICompetitorService
{
    Task DeleteCompetitorAsync(Guid competitorId);
    Task<Competitor> GetCompetitorAsync(Guid competitorId);
    Task<CompetitorWithOptionsViewModel> GetCompetitorWithHistoryAsync(Guid competitorId);
    Task SaveAsync(
        CompetitorWithOptionsViewModel competitor,
        string userName);
    Task SaveAsync(
        MultipleCompetitorsWithOptionsViewModel vm,
        Guid clubId,
        string userName = "");
    Task<Competitor> GetCompetitorAsync(
        string clubInitials,
        string sailNumber);
    Task<CompetitorStatsViewModel> GetCompetitorStatsAsync(
        string clubInitials,
        string sailor);
    Task<IList<PlaceCount>> GetCompetitorSeasonRanksAsync(Guid competitorId, string seasonUrlName);
    Task<Guid?> GetCompetitorIdForSailnumberAsync(
        Guid clubId,
        string sailNumber);
    Task<IList<Competitor>> GetCompetitorsAsync(Guid clubId, bool includeInactive);
    Task<IList<Competitor>> GetCompetitorsAsync(string clubInitials, bool includeInactive);
    Task<IEnumerable<KeyValuePair<string, string>>> GetSaveErrors(Competitor competitor);
    Task<IList<CompetitorIndexViewModel>> GetCompetitorsWithDeletableInfoAsync(
        String clubInitials,
        bool includeInactive);
    Task ClearAltNumbers(Guid clubId);
    Task InactivateSince(Guid clubId, DateTime sinceDate);
    Task<IDictionary<String, IEnumerable<Competitor>>> GetCompetitorsForFleetAsync(
        Guid clubId,
        Guid fleetId,
        bool includeInactive = false);
    Task<IDictionary<String, IEnumerable<Competitor>>> GetCompetitorsForRegattaAsync(
        Guid clubId,
        Guid regattaId,
        bool includeInactive = false);
    Task SetCompetitorActive(
        Guid clubId,
        Guid competitorId,
        bool active,
        string userName = "");
    Task AddCompetitorNote(Guid id, string newNote, string v);
#pragma warning disable CA1054 // Uri parameters should not be strings
    Task<IList<CompetitorWindStats>> GetCompetitorWindStatsAsync(
        Guid competitorId,
        string seasonUrlName = null,
        bool groupByDirection = false);
#pragma warning restore CA1054 // Uri parameters should not be strings
}
