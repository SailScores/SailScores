using Microsoft.EntityFrameworkCore;
using SailScores.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Db = SailScores.Database.Entities;

namespace SailScores.Core.Services;

// The score codes a club can score with: those in its own scoring systems and in the shared
// systems it uses directly (club, season, series or regatta), plus their parents. Shared
// systems the club doesn't use are left out.
internal static class ClubScoreCodes
{
    // One code per name, ignoring case. A code from the club's own systems wins over a
    // shared one of the same name, then one with a description.
    public static async Task<IList<Db.ScoreCode>> GetAsync(ISailScoresContext dbContext, Guid clubId)
    {
        var systems = await dbContext.ScoringSystems
            .Where(s => s.ClubId == clubId || s.ClubId == null)
            .Select(s => new { s.Id, s.ParentSystemId, s.ClubId })
            .ToDictionaryAsync(s => s.Id)
            .ConfigureAwait(false);

        var systemIds = new HashSet<Guid>();
        foreach (var rootId in await GetUsedSystemIdsAsync(dbContext, clubId).ConfigureAwait(false))
        {
            // The visited check also stops a parent loop.
            for (Guid? id = rootId;
                id.HasValue && systems.ContainsKey(id.Value) && systemIds.Add(id.Value);)
            {
                id = systems[id.Value].ParentSystemId;
            }
        }

        var codes = await dbContext.ScoreCodes
            .AsNoTracking()
            .Where(c => systemIds.Contains(c.ScoringSystemId) && c.Name != null && c.Name != "")
            .ToListAsync()
            .ConfigureAwait(false);

        return codes
            .GroupBy(c => c.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => g
                .OrderBy(c => systems[c.ScoringSystemId].ClubId == null)
                .ThenBy(c => string.IsNullOrWhiteSpace(c.Description))
                .First())
            .ToList();
    }

    private static async Task<IList<Guid>> GetUsedSystemIdsAsync(
        ISailScoresContext dbContext,
        Guid clubId)
    {
        var ids = await dbContext.ScoringSystems
            .Where(s => s.ClubId == clubId)
            .Select(s => s.Id)
            .ToListAsync()
            .ConfigureAwait(false);
        ids.AddRange(await dbContext.Clubs
            .Where(c => c.Id == clubId && c.DefaultScoringSystemId != null)
            .Select(c => c.DefaultScoringSystemId.Value)
            .ToListAsync()
            .ConfigureAwait(false));
        ids.AddRange(await dbContext.Seasons
            .Where(s => s.ClubId == clubId && s.DefaultScoringSystemId != null)
            .Select(s => s.DefaultScoringSystemId.Value)
            .Distinct()
            .ToListAsync()
            .ConfigureAwait(false));
        ids.AddRange(await dbContext.Series
            .Where(s => s.ClubId == clubId && s.ScoringSystemId != null)
            .Select(s => s.ScoringSystemId.Value)
            .Distinct()
            .ToListAsync()
            .ConfigureAwait(false));
        ids.AddRange(await dbContext.Regattas
            .Where(r => r.ClubId == clubId && r.ScoringSystemId != null)
            .Select(r => r.ScoringSystemId.Value)
            .Distinct()
            .ToListAsync()
            .ConfigureAwait(false));
        return ids;
    }
}
