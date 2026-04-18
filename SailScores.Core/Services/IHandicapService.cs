using SailScores.Core.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SailScores.Core.Services
{
    public interface IHandicapService
    {
        // Returns site-wide systems plus any club-specific systems for the given club
        Task<IList<HandicapSystem>> GetHandicapSystemsAsync(Guid clubId);

        Task<HandicapSystem> GetHandicapSystemAsync(Guid id);

        Task<HandicapSystem> SaveHandicapSystemAsync(HandicapSystem system);

        Task DeleteHandicapSystemAsync(Guid id);

        // Returns all handicap ratings for a competitor, across all systems
        Task<IList<CompetitorHandicap>> GetCompetitorHandicapsAsync(Guid competitorId);

        // Returns all handicap ratings for a competitor under a specific system
        Task<IList<CompetitorHandicap>> GetCompetitorHandicapsAsync(Guid competitorId, Guid handicapSystemId);

        Task<CompetitorHandicap> SaveCompetitorHandicapAsync(CompetitorHandicap handicap);

        Task DeleteCompetitorHandicapAsync(Guid id);

        // Resolves the effective HandicapSystem for a series via the Series → Fleet → Club hierarchy.
        // Returns null if no handicap system is configured at any level.
        Task<HandicapSystem> GetEffectiveHandicapSystemAsync(Series series);

        // Builds the lookup dictionary used by HandicapScoringCalculator.
        // Key: (competitorId, raceDate.Date). Value: the effective rating on that date.
        // Only includes competitors that appear in the series.
        Task<IReadOnlyDictionary<(Guid competitorId, DateTime raceDate), decimal>> BuildHandicapLookupAsync(
            Series series,
            Guid handicapSystemId);
    }
}
