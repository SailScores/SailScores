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
                .Where(h => h.ClubId == clubId)
                .OrderBy(h => h.Name)
                .ToListAsync()
                .ConfigureAwait(false);

            return _mapper.Map<IList<HandicapSystem>>(systems);
        }

        public async Task<IList<HandicapSystem>> GetBaseHandicapSystemsAsync()
        {
            var systems = await _dbContext.HandicapSystems
                .Where(h => h.ClubId == null)
                .OrderBy(h => h.Name)
                .ToListAsync()
                .ConfigureAwait(false);

            return _mapper.Map<IList<HandicapSystem>>(systems);
        }

        public async Task<HandicapSystem> CreateClubHandicapSystemAsync(
            Guid clubId,
            Guid baseSystemId,
            string name,
            string description)
        {
            var baseSystem = await _dbContext.HandicapSystems
                .SingleOrDefaultAsync(h => h.Id == baseSystemId)
                .ConfigureAwait(false);

            if (baseSystem == null)
                throw new InvalidOperationException("Base handicap system was not found.");

            if (baseSystem.ClubId.HasValue)
                throw new InvalidOperationException("Only site-wide base handicap systems can be used as a parent.");

            var dbSystem = new Db.HandicapSystem
            {
                Id = Guid.NewGuid(),
                ClubId = clubId,
                ParentSystemId = baseSystem.Id,
                Name = name,
                Description = description,
                SystemType = baseSystem.SystemType
            };

            _dbContext.HandicapSystems.Add(dbSystem);
            await _dbContext.SaveChangesAsync().ConfigureAwait(false);

            return _mapper.Map<HandicapSystem>(dbSystem);
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
                if (handicap.EffectiveFrom == null)
                {
                    var nullStartExists = await _dbContext.CompetitorHandicaps
                        .AnyAsync(ch => ch.CompetitorId == handicap.CompetitorId
                                     && ch.HandicapSystemId == handicap.HandicapSystemId
                                     && ch.EffectiveFrom == null)
                        .ConfigureAwait(false);
                    if (nullStartExists)
                        throw new InvalidOperationException(
                            "A rating with no start date already exists for this system. Please enter a start date for the new rating.");
                }

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

                if (handicap.EffectiveFrom == null)
                {
                    var nullStartExists = await _dbContext.CompetitorHandicaps
                        .AnyAsync(ch => ch.CompetitorId == handicap.CompetitorId
                                     && ch.HandicapSystemId == handicap.HandicapSystemId
                                     && ch.EffectiveFrom == null
                                     && ch.Id != handicap.Id)
                        .ConfigureAwait(false);
                    if (nullStartExists)
                        throw new InvalidOperationException(
                            "Another rating with no start date already exists for this system. Please enter a start date.");
                }

                existing.Value = handicap.Value;
                existing.EffectiveFrom = handicap.EffectiveFrom;
                existing.Notes = handicap.Notes;
                await _dbContext.SaveChangesAsync().ConfigureAwait(false);
            }

            await RebuildCompetitorHandicapChainAsync(handicap.CompetitorId, handicap.HandicapSystemId)
                .ConfigureAwait(false);

            return handicap;
        }

        public async Task DeleteCompetitorHandicapAsync(Guid id)
        {
            var handicap = await _dbContext.CompetitorHandicaps
                .SingleAsync(ch => ch.Id == id)
                .ConfigureAwait(false);

            var competitorId = handicap.CompetitorId;
            var systemId = handicap.HandicapSystemId;

            _dbContext.CompetitorHandicaps.Remove(handicap);
            await _dbContext.SaveChangesAsync().ConfigureAwait(false);

            await RebuildCompetitorHandicapChainAsync(competitorId, systemId).ConfigureAwait(false);
        }

        private async Task RebuildCompetitorHandicapChainAsync(Guid competitorId, Guid handicapSystemId)
        {
            var all = await _dbContext.CompetitorHandicaps
                .Where(ch => ch.CompetitorId == competitorId && ch.HandicapSystemId == handicapSystemId)
                .ToListAsync()
                .ConfigureAwait(false);

            RebuildChain(all.Select(r => new CompetitorHandicapEntry(r)).Cast<IHandicapEntry>().ToList());
            await _dbContext.SaveChangesAsync().ConfigureAwait(false);
        }

        public async Task<IList<ClassHandicap>> GetClassHandicapsAsync(Guid boatClassId)
        {
            var handicaps = await _dbContext.ClassHandicaps
                .Include(ch => ch.HandicapSystem)
                .Where(ch => ch.BoatClassId == boatClassId)
                .OrderBy(ch => ch.HandicapSystem.Name)
                .ThenBy(ch => ch.EffectiveFrom)
                .ToListAsync()
                .ConfigureAwait(false);

            return _mapper.Map<IList<ClassHandicap>>(handicaps);
        }

        public async Task<ClassHandicap> SaveClassHandicapAsync(ClassHandicap handicap)
        {
            if (handicap.Id == Guid.Empty)
            {
                if (handicap.EffectiveFrom == null)
                {
                    var nullStartExists = await _dbContext.ClassHandicaps
                        .AnyAsync(ch => ch.BoatClassId == handicap.BoatClassId
                                     && ch.HandicapSystemId == handicap.HandicapSystemId
                                     && ch.EffectiveFrom == null)
                        .ConfigureAwait(false);
                    if (nullStartExists)
                        throw new InvalidOperationException(
                            "A rating with no start date already exists for this system. Please enter a start date for the new rating.");
                }

                var dbHandicap = _mapper.Map<Db.ClassHandicap>(handicap);
                dbHandicap.Id = Guid.NewGuid();
                _dbContext.ClassHandicaps.Add(dbHandicap);
                await _dbContext.SaveChangesAsync().ConfigureAwait(false);
                handicap.Id = dbHandicap.Id;
            }
            else
            {
                var existing = await _dbContext.ClassHandicaps
                    .SingleAsync(ch => ch.Id == handicap.Id)
                    .ConfigureAwait(false);

                if (handicap.EffectiveFrom == null)
                {
                    var nullStartExists = await _dbContext.ClassHandicaps
                        .AnyAsync(ch => ch.BoatClassId == handicap.BoatClassId
                                     && ch.HandicapSystemId == handicap.HandicapSystemId
                                     && ch.EffectiveFrom == null
                                     && ch.Id != handicap.Id)
                        .ConfigureAwait(false);
                    if (nullStartExists)
                        throw new InvalidOperationException(
                            "Another rating with no start date already exists for this system. Please enter a start date.");
                }

                existing.Value = handicap.Value;
                existing.EffectiveFrom = handicap.EffectiveFrom;
                existing.Notes = handicap.Notes;
                await _dbContext.SaveChangesAsync().ConfigureAwait(false);
            }

            await RebuildClassHandicapChainAsync(handicap.BoatClassId, handicap.HandicapSystemId)
                .ConfigureAwait(false);

            return handicap;
        }

        public async Task DeleteClassHandicapAsync(Guid id)
        {
            var handicap = await _dbContext.ClassHandicaps
                .SingleAsync(ch => ch.Id == id)
                .ConfigureAwait(false);

            var classId = handicap.BoatClassId;
            var systemId = handicap.HandicapSystemId;

            _dbContext.ClassHandicaps.Remove(handicap);
            await _dbContext.SaveChangesAsync().ConfigureAwait(false);

            await RebuildClassHandicapChainAsync(classId, systemId).ConfigureAwait(false);
        }

        private async Task RebuildClassHandicapChainAsync(Guid boatClassId, Guid handicapSystemId)
        {
            var all = await _dbContext.ClassHandicaps
                .Where(ch => ch.BoatClassId == boatClassId && ch.HandicapSystemId == handicapSystemId)
                .ToListAsync()
                .ConfigureAwait(false);

            RebuildChain(all.Select(r => new ClassHandicapEntry(r)).Cast<IHandicapEntry>().ToList());
            await _dbContext.SaveChangesAsync().ConfigureAwait(false);
        }

        private interface IHandicapEntry
        {
            DateTime? EffectiveFrom { get; }
            DateTime? EffectiveTo { get; set; }
        }

        private sealed class CompetitorHandicapEntry : IHandicapEntry
        {
            private readonly Db.CompetitorHandicap _row;
            public CompetitorHandicapEntry(Db.CompetitorHandicap row) => _row = row;
            public DateTime? EffectiveFrom => _row.EffectiveFrom;
            public DateTime? EffectiveTo { get => _row.EffectiveTo; set => _row.EffectiveTo = value; }
        }

        private sealed class ClassHandicapEntry : IHandicapEntry
        {
            private readonly Db.ClassHandicap _row;
            public ClassHandicapEntry(Db.ClassHandicap row) => _row = row;
            public DateTime? EffectiveFrom => _row.EffectiveFrom;
            public DateTime? EffectiveTo { get => _row.EffectiveTo; set => _row.EffectiveTo = value; }
        }

        private static void RebuildChain(IList<IHandicapEntry> entries)
        {
            // Sort: null EffectiveFrom (oldest) first, then ascending by date
            var sorted = entries
                .OrderBy(e => e.EffectiveFrom.HasValue ? 1 : 0)
                .ThenBy(e => e.EffectiveFrom)
                .ToList();

            for (int i = 0; i < sorted.Count; i++)
            {
                if (i < sorted.Count - 1)
                    sorted[i].EffectiveTo = sorted[i + 1].EffectiveFrom!.Value.AddDays(-1);
                else
                    sorted[i].EffectiveTo = null;
            }
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
            var competitorIds = series.Races?
                .Where(r => r.Scores != null)
                .SelectMany(r => r.Scores)
                .Select(s => s.CompetitorId)
                .Distinct()
                .ToList() ?? new List<Guid>();

            var raceDates = series.Races?
                .Where(r => r.Date.HasValue)
                .Select(r => r.Date!.Value.Date)
                .Distinct()
                .ToList() ?? new List<DateTime>();

            if (!competitorIds.Any() || !raceDates.Any())
                return new Dictionary<(Guid, DateTime), decimal>();

            var competitorRows = await _dbContext.CompetitorHandicaps
                .Where(ch => ch.HandicapSystemId == handicapSystemId
                          && competitorIds.Contains(ch.CompetitorId))
                .ToListAsync()
                .ConfigureAwait(false);

            // Load each competitor's BoatClassId for class-level fallback
            var classIdByCompetitor = await _dbContext.Competitors
                .Where(c => competitorIds.Contains(c.Id))
                .Select(c => new { c.Id, c.BoatClassId })
                .ToDictionaryAsync(c => c.Id, c => c.BoatClassId)
                .ConfigureAwait(false);

            var classIds = classIdByCompetitor.Values.Distinct().ToList();

            var classRows = classIds.Any()
                ? await _dbContext.ClassHandicaps
                    .Where(ch => ch.HandicapSystemId == handicapSystemId
                              && classIds.Contains(ch.BoatClassId))
                    .ToListAsync()
                    .ConfigureAwait(false)
                : new List<Db.ClassHandicap>();

            var lookup = new Dictionary<(Guid competitorId, DateTime raceDate), decimal>();

            foreach (var competitorId in competitorIds)
            {
                var compRows = competitorRows.Where(r => r.CompetitorId == competitorId).ToList();
                var boatClassId = classIdByCompetitor.TryGetValue(competitorId, out var cid) ? cid : (Guid?)null;
                var clsRows = boatClassId.HasValue
                    ? classRows.Where(r => r.BoatClassId == boatClassId.Value).ToList()
                    : new List<Db.ClassHandicap>();

                foreach (var raceDate in raceDates)
                {
                    var effective = compRows
                        .Where(r => (r.EffectiveFrom == null || r.EffectiveFrom.Value.Date <= raceDate)
                                 && (r.EffectiveTo == null || r.EffectiveTo.Value.Date >= raceDate))
                        .OrderByDescending(r => r.EffectiveFrom)
                        .FirstOrDefault();

                    if (effective != null)
                    {
                        lookup[(competitorId, raceDate)] = effective.Value;
                        continue;
                    }

                    // Fall back to class-level rating
                    var classEffective = clsRows
                        .Where(r => (r.EffectiveFrom == null || r.EffectiveFrom.Value.Date <= raceDate)
                                 && (r.EffectiveTo == null || r.EffectiveTo.Value.Date >= raceDate))
                        .OrderByDescending(r => r.EffectiveFrom)
                        .FirstOrDefault();

                    if (classEffective != null)
                        lookup[(competitorId, raceDate)] = classEffective.Value;
                }
            }

            return lookup;
        }
    }
}
