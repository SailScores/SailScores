using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SailScores.Core.Model;
using SailScores.Core.Model.Summary;

namespace SailScores.Core.Services;

public interface ISiteAdminService
{
    Task<IEnumerable<(ClubSummary club, DateTime? latestSeriesUpdate, DateTime? latestRaceDate)>> GetAllClubsWithDatesAsync();
    Task<Club> GetClubDetailsAsync(string clubInitials);
    Task<Club> GetFullClubForBackupAsync(Guid clubId);
    Task ResetClubAsync(Guid clubId);
}
