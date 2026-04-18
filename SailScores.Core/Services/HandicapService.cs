using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SailScores.Core.Model;
using SailScores.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Db = SailScores.Database.Entities;

namespace SailScores.Core.Services
{
    public class HandicapService : IHandicapService
    {
        private readonly ISailScoresContext _dbContext;
        private readonly IMapper _mapper;

        public HandicapService(ISailScoresContext dbContext, IMapper mapper)
        {
            _dbContext = dbContext;
            _mapper = mapper;
        }

        public async Task<IList<HandicapSystem>> GetHandicapSystemsAsync(Guid clubId)
        {
            var systems = await _dbContext.HandicapSystems
                .Where(h => h.ClubId == null || h.ClubId == clubId)
                .OrderBy(h => h.ClubId == null ? 0 : 1)
                .ThenBy(h => h.Name)
                .ToListAsync()
                .ConfigureAwait(false);

            return _mapper.Map<IList<HandicapSystem>>(systems);
        }

        public async Task<HandicapSystem> GetHandicapSystemAsync(Guid id)
        {
            var system = await _dbContext.HandicapSystems
                .SingleOrDefaultAsync(h => h.Id == id)
                .ConfigureAwait(false);

            return _mapper.Map<HandicapSystem>(system);
        }

        public async Task<HandicapSystem> SaveHandicapSystemAsync(HandicapSystem system)
        {
            if (system.Id == Guid.Empty)
            {
                var dbSystem = _mapper.Map<Db.HandicapSystem>(system);
                dbSystem.Id = Guid.NewGuid();
                _dbContext.HandicapSystems.Add(dbSystem);
                await _dbContext.SaveChangesAsync().ConfigureAwait(false);
                system.Id = dbSystem.Id;
            }
            else
            {
                var existing = await _dbContext.HandicapSystems
                    .SingleAsync(h => h.Id == system.Id)
                    .ConfigureAwait(false);

                // Site-wide systems (ClubId == null) cannot be modified
                if (existing.ClubId == null)
                    throw new InvalidOperationException("Site-wide handicap systems cannot be modified.");

                existing.Name = system.Name;
                existing.SystemType = (Db.HandicapSystemType)(int)system.SystemType;
                existing.Description = system.Description;
                await _dbContext.SaveChangesAsync().ConfigureAwait(false);
            }

            return system;
        }

        public async Task DeleteHandicapSystemAsync(Guid id)
        {
            var system = await _dbContext.HandicapSystems
                .SingleAsync(h => h.Id == id)
                .ConfigureAwait(false);

            if (system.ClubId == null)
                throw new InvalidOperationException("Site-wide handicap systems cannot be deleted.");

            _dbContext.HandicapSystems.Remove(system);
            await _dbContext.SaveChangesAsync().ConfigureAwait(false);
        }

        public async Task<IList<CompetitorHandicap>> GetCompetitorHandicapsAsync(Guid competitorId)
        {
            var handicaps = await _dbContext.CompetitorHandicaps
                .Include(ch => ch.HandicapSystem)
                .Where(ch => ch.CompetitorId == competitorId)
                .OrderBy(ch => ch.HandicapSystem.Name)
                .ThenBy(ch => ch.EffectiveFrom)
                .ToListAsync()
                .ConfigureAwait(false);

            return _mapper.Map<IList<CompetitorHandicap>>(handicaps);
        }

        public async Task<IList<CompetitorHandicap>> GetCompetitorHandicapsAsync(
            Guid competitorId,
            Guid handicapSystemId)
        {
            var handicaps = await _dbContext.CompetitorHandicaps
                .Where(ch => ch.CompetitorId == competitorId && ch.HandicapSystemId == handicapSystemId)
                .OrderBy(ch => ch.EffectiveFrom)
                .ToListAsync()
                .ConfigureAwait(false);

            return _mapper.Map<IList<CompetitorHandicap>>(handicaps);
        }

        public async Task<CompetitorHandicap> SaveCompetitorHandicapAsync(CompetitorHandicap handicap)
        {
            if (handicap.Id == Guid.Empty)
            {
                var dbHandicap = _mapper.Map<Db.CompetitorHandicap>(handicap);
                dbHandicap.Id = Guid.NewGuid();
                _dbContext.CompetitorHandicaps.Add(dbHandicap);
                await _dbContext.SaveChangesAsync().ConfigureAwait(false);
                handicap.Id = dbHandicap.Id;
            }
            else
            {
                var existing = await _dbContext.CompetitorHandicaps
                    .SingleAsync(ch => ch.Id == handicap.Id)
                    .ConfigureAwait(false);

                existing.Value = handicap.Value;
                existing.EffectiveFrom = handicap.EffectiveFrom;
                existing.EffectiveTo = handicap.EffectiveTo;
                existing.Notes = handicap.Notes;
                await _dbContext.SaveChangesAsync().ConfigureAwait(false);
            }

            return handicap;
        }

        public async Task DeleteCompetitorHandicapAsync(Guid id)
        {
            var handicap = await _dbContext.CompetitorHandicaps
                .SingleAsync(ch => ch.Id == id)
                .ConfigureAwait(false);

            _dbContext.CompetitorHandicaps.Remove(handicap);
            await _dbContext.SaveChangesAsync().ConfigureAwait(false);
        }

        public async Task<HandicapSystem> GetEffectiveHandicapSystemAsync(Series series)
        {
            // Most-specific wins: Series → Fleet → Club
            var handicapSystemId = series.HandicapSystemId;

            if (!handicapSystemId.HasValue && series.Fleet?.DefaultHandicapSystemId.HasValue == true)
                handicapSystemId = series.Fleet.DefaultHandicapSystemId;

            if (!handicapSystemId.HasValue)
            {
                // Load club to check its default
                var club = await _dbContext.Clubs
                    .Where(c => c.Id == series.ClubId)
                    .Select(c => new { c.DefaultHandicapSystemId })
                    .SingleOrDefaultAsync()
                    .ConfigureAwait(false);

                handicapSystemId = club?.DefaultHandicapSystemId;
            }

            if (!handicapSystemId.HasValue)
                return null;

            return await GetHandicapSystemAsync(handicapSystemId.Value).ConfigureAwait(false);
        }

        public async Task<IReadOnlyDictionary<(Guid competitorId, DateTime raceDate), decimal>> BuildHandicapLookupAsync(
            Series series,
            Guid handicapSystemId)
        {
            // Collect all competitor IDs that appear in the series
            var competitorIds = series.Races?
                .Where(r => r.Scores != null)
                .SelectMany(r => r.Scores)
                .Select(s => s.CompetitorId)
                .Distinct()
                .ToList() ?? new List<Guid>();

            // Collect all distinct race dates
            var raceDates = series.Races?
                .Where(r => r.Date.HasValue)
                .Select(r => r.Date!.Value.Date)
                .Distinct()
                .ToList() ?? new List<DateTime>();

            if (!competitorIds.Any() || !raceDates.Any())
                return new Dictionary<(Guid, DateTime), decimal>();

            // Batch-load all relevant handicap rows
            var rows = await _dbContext.CompetitorHandicaps
                .Where(ch => ch.HandicapSystemId == handicapSystemId
                          && competitorIds.Contains(ch.CompetitorId))
                .ToListAsync()
                .ConfigureAwait(false);

            var lookup = new Dictionary<(Guid competitorId, DateTime raceDate), decimal>();

            foreach (var competitorId in competitorIds)
            {
                var competitorRows = rows
                    .Where(r => r.CompetitorId == competitorId)
                    .ToList();

                foreach (var raceDate in raceDates)
                {
                    var effective = competitorRows
                        .Where(r => (r.EffectiveFrom == null || r.EffectiveFrom.Value.Date <= raceDate)
                                 && (r.EffectiveTo == null || r.EffectiveTo.Value.Date >= raceDate))
                        .OrderByDescending(r => r.EffectiveFrom)
                        .FirstOrDefault();

                    if (effective != null)
                        lookup[(competitorId, raceDate)] = effective.Value;
                }
            }

            return lookup;
        }
    }
}
