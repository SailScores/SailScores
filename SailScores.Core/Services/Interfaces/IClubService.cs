using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SailScores.Core.Model;
using SailScores.Core.Model.Summary;
using Entities = SailScores.Database.Entities;

namespace SailScores.Core.Services;

public interface IClubService
{
    Task<IEnumerable<ClubSummary>> GetClubs(bool includeHidden);
    Task<Club> GetFullClubExceptScores(Guid id);
    Task<Club> GetFullClubExceptScores(string clubInitials);
    Task<Club> GetClubForAdmin(Guid id);
    Task<Club> GetClubForAdmin(string clubInitials);
    Task<Club> GetMinimalClub(Guid id);
    Task<Club> GetMinimalClub(string clubInitials);
    Task<Guid> SaveNewClub(Club club);
    Task SaveNewFleet(Fleet fleet);
    Task SaveNewSeason(Season season);
    Task<IList<Fleet>> GetAllFleets(Guid clubId);
    Task<IList<Fleet>> GetActiveFleets(Guid clubId);

    Task UpdateClub(Club club);
    Task<Guid> GetClubId(string initials);
    Task<Guid> CopyClubAsync(Guid copyFromClubId, Club targetClub);
    Task<IEnumerable<BoatClass>> GetAllBoatClasses(Guid clubId);
    Task<bool> DoesClubHaveCompetitors(Guid clubId);
    Task<IList<Fleet>> GetMinimalForSelectedBoatsFleets(Guid clubId);
    Task<IList<Entities.ClubSeasonStats>> GetClubStats(string clubInitials);
    Task<IList<Entities.SiteStats>> GetAllStats();
    Task UpdateStatsDescription(Guid clubId, string statisticsDescription);
    Task<string> GetClubName(string clubInitials);
    Task<bool> HasCompetitorsAsync(Guid id);
    Task SetUseAdvancedFeaturesAsync(Guid clubId, bool enabled);
    Task SetSubscriptionTypeAsync(Guid clubId, string subscriptionType);
}
