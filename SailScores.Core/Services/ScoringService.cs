using AutoMapper;
using SailScores.Api.Enumerations;
using Microsoft.EntityFrameworkCore;
using SailScores.Database;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using SailScores.Core.Model;
using Db = SailScores.Database.Entities;
using Microsoft.Extensions.Caching.Memory;

namespace SailScores.Core.Services
{
    public class ScoringService : IScoringService
    {
        private readonly ISailScoresContext _dbContext;
        private readonly IMemoryCache _cache;
        private readonly IMapper _mapper;

        private const string BaseRegattaSystemName = "Appendix A For Regatta";

        public ScoringService(
            ISailScoresContext dbContext,
            IMemoryCache cache,
            IMapper mapper)
        {
            _dbContext = dbContext;
            _cache = cache;
            _mapper = mapper;
        }

        // Codes usable in the club's own scoring systems, including those inherited from
        // parent systems. Shared systems the club doesn't build on are left out.
        public async Task<IEnumerable<Model.ScoreCode>> GetScoreCodesAsync(Guid clubId)
        {
            var systemIds = await _dbContext
                .ScoringSystems
                .Where(s => s.ClubId == clubId)
                .Select(s => s.Id)
                .ToListAsync()
                .ConfigureAwait(false);

            var allScoreCodes = new List<Model.ScoreCode>();
            foreach (var systemId in systemIds)
            {
                allScoreCodes.AddRange(await GetAllCodesAsync(systemId).ConfigureAwait(false));
            }

            return allScoreCodes
                .GroupBy(s => s.Name, (key, g) => g.First())
                .ToList();
        }

        public async Task<IList<Model.ScoringSystem>> GetScoringSystemsAsync(
            Guid clubId,
            bool includeBaseSystems)
        {
            var dbObjects = await _dbContext
                .ScoringSystems
                .Where(s => s.ClubId == clubId ||
                (includeBaseSystems && s.ClubId == null
                    && (!s.IsRestrictedToClubs
                        || s.ClubAccess.Any(a => a.ClubId == clubId))))
                .Include(s => s.ScoreCodes)
                .OrderBy(s => s.Name)
                .ToListAsync()
                .ConfigureAwait(false);
            var known = dbObjects.ToDictionary(s => s.Id);
            var models = new List<Model.ScoringSystem>();
            foreach (var dbObject in dbObjects)
            {
                var model = _mapper.Map<Model.ScoringSystem>(dbObject);
                var ancestors = await GetAncestorSystemsAsync(dbObject.ParentSystemId, known)
                    .ConfigureAwait(false);
                ApplyInheritedValues(model, dbObject, ancestors);
                models.Add(model);
            }
            return models;
        }


        public async Task<Model.ScoringSystem> GetSiteDefaultSystemAsync()
        {
            var dbObject = await _dbContext
                .ScoringSystems
                .Where(s => s.ClubId == null
                && (s.IsSiteDefault ?? false))
                .Include(s => s.ScoreCodes)
                .FirstOrDefaultAsync()
                .ConfigureAwait(false);
            return await MapWithInheritedValuesAsync(dbObject).ConfigureAwait(false);
        }

        public async Task<Model.ScoringSystem> GetBaseRegattaSystemAsync()
        {
            var dbObject = await _dbContext
                .ScoringSystems
                .Where(s => s.ClubId == null
                   && s.Name == BaseRegattaSystemName)
                .Include(s => s.ScoreCodes)
                .FirstOrDefaultAsync()
                .ConfigureAwait(false);
            return await MapWithInheritedValuesAsync(dbObject).ConfigureAwait(false);
        }

        public async Task<IList<SharedScoringSystemAccess>> GetSharedScoringSystemAccessAsync()
        {
            var systems = await _dbContext
                .ScoringSystems
                .Where(s => s.ClubId == null)
                .OrderBy(s => s.Name)
                .Select(s => new
                {
                    s.Id,
                    s.Name,
                    s.ParentSystemId,
                    s.CalculatorType,
                    s.IsSiteDefault,
                    s.IsRestrictedToClubs,
                    ClubIds = s.ClubAccess.Select(a => a.ClubId).ToList()
                })
                .ToListAsync()
                .ConfigureAwait(false);
            var names = systems.ToDictionary(s => s.Id, s => s.Name);

            return systems.Select(s => new SharedScoringSystemAccess
            {
                Id = s.Id,
                Name = s.Name,
                ParentSystemId = s.ParentSystemId,
                ParentSystemName = s.ParentSystemId.HasValue
                    ? names.GetValueOrDefault(s.ParentSystemId.Value)
                    : null,
                CalculatorType = s.CalculatorType,
                IsUsedForNewClubs = IsUsedForNewClubs(s.IsSiteDefault, s.Name),
                IsRestrictedToClubs = s.IsRestrictedToClubs,
                ClubIds = s.ClubIds
            }).ToList();
        }

        // Unrestricting clears the club list, so no hidden access is left behind.
        public async Task SaveSharedScoringSystemAccessAsync(
            Guid scoringSystemId,
            bool restrictToClubs,
            IEnumerable<Guid> clubIds)
        {
            var system = await _dbContext.ScoringSystems
                .Include(s => s.ClubAccess)
                .SingleOrDefaultAsync(s => s.Id == scoringSystemId)
                .ConfigureAwait(false);
            if (system == null || system.ClubId != null)
            {
                throw new InvalidOperationException(
                    "Only shared scoring systems can be restricted to clubs.");
            }
            if (restrictToClubs && IsUsedForNewClubs(system.IsSiteDefault, system.Name))
            {
                throw new InvalidOperationException(
                    "New clubs are set up from this scoring system, so it must stay available to all clubs.");
            }

            var requested = restrictToClubs
                ? (clubIds ?? Enumerable.Empty<Guid>()).Distinct().ToList()
                : new List<Guid>();
            var wanted = (await _dbContext.Clubs
                .Where(c => requested.Contains(c.Id))
                .Select(c => c.Id)
                .ToListAsync()
                .ConfigureAwait(false))
                .ToHashSet();

            system.IsRestrictedToClubs = restrictToClubs;
            system.ClubAccess ??= new List<Db.ScoringSystemClubAccess>();
            foreach (var access in system.ClubAccess.Where(a => !wanted.Contains(a.ClubId)).ToList())
            {
                system.ClubAccess.Remove(access);
                _dbContext.ScoringSystemClubAccess.Remove(access);
            }
            var existing = system.ClubAccess.Select(a => a.ClubId).ToHashSet();
            foreach (var clubId in wanted.Where(id => !existing.Contains(id)))
            {
                system.ClubAccess.Add(new Db.ScoringSystemClubAccess
                {
                    ScoringSystemId = system.Id,
                    ClubId = clubId
                });
            }
            await _dbContext.SaveChangesAsync().ConfigureAwait(false);
        }

        private static bool IsUsedForNewClubs(bool? isSiteDefault, string name)
        {
            return (isSiteDefault ?? false) || name == BaseRegattaSystemName;
        }

        private async Task<Model.ScoringSystem> MapWithInheritedValuesAsync(Db.ScoringSystem dbObject)
        {
            if (dbObject == null)
            {
                return null;
            }
            var model = _mapper.Map<Model.ScoringSystem>(dbObject);
            var ancestors = await GetAncestorSystemsAsync(dbObject.ParentSystemId)
                .ConfigureAwait(false);
            ApplyInheritedValues(model, dbObject, ancestors);
            return model;
        }

        // Sets the system's own Settings and the values resolved along the parent chain:
        // CalculatorType from the root, EffectiveSettings merged property by property
        // (nearest non-null wins), EffectiveDiscardPattern and EffectiveParticipationPercent
        // (nearest non-blank), and EffectiveDiscardMethod (nearest non-null, else RaceCountPattern).
        // ancestors: nearest parent first, root last.
        public static void ApplyInheritedValues(
            Model.ScoringSystem model,
            Db.ScoringSystem self,
            IList<Db.ScoringSystem> ancestors)
        {
            ancestors ??= new List<Db.ScoringSystem>();
            var root = ancestors.Count > 0 ? ancestors[^1] : self;
            model.CalculatorType = root.CalculatorType;

            model.Settings = ScoringSystemSettings.FromJson(self.Settings);
            var effective = model.Settings;
            foreach (var ancestor in ancestors)
            {
                effective = effective.MergeOver(ScoringSystemSettings.FromJson(ancestor.Settings));
            }
            model.EffectiveSettings = effective.MergeOver(null);

            var chain = new[] { self }.Concat(ancestors).ToList();
            model.EffectiveDiscardPattern = chain
                .Select(s => s.DiscardPattern)
                .FirstOrDefault(p => !String.IsNullOrWhiteSpace(p));
            model.EffectiveParticipationPercent = chain
                .Select(s => s.ParticipationPercent)
                .FirstOrDefault(p => p.HasValue);

            model.DiscardMethod = self.DiscardMethod;
            model.EffectiveDiscardMethod = self.DiscardMethod
                ?? ancestors.Select(a => a.DiscardMethod).FirstOrDefault(m => m.HasValue)
                ?? DiscardMethod.RaceCountPattern;
        }

        // This is a big one: returns a scoring system, including inherited ScoreCodes.
        // So all score codes that will work in this scoring system will be returned.
        public Task<ScoringSystem> GetScoringSystemAsync(Guid scoringSystemId)
        {
            return GetScoringSystemAsync(scoringSystemId, true);
        }

        public async Task<ScoringSystem> GetScoringSystemAsync(Guid scoringSystemId,
            bool skipCache)
        {
            var cacheKey = $"ScoringSystem-{scoringSystemId}";
            if (!skipCache)
            {
                if (_cache.TryGetValue(cacheKey, out ScoringSystem cachedSystem))
                {
                    return cachedSystem;
                }
            }

            var requestedDbSystem = await _dbContext
                .ScoringSystems
                .Include(s => s.ScoreCodes)
                .Include(s => s.ScoreCodeGroups)
                    .ThenInclude(g => g.Codes)
                .AsSplitQuery()
                .SingleAsync(s => s.Id == scoringSystemId)
                .ConfigureAwait(false);

            var requestedSystem = _mapper.Map<Model.ScoringSystem>(requestedDbSystem);

            var ancestors = await GetAncestorSystemsAsync(requestedDbSystem.ParentSystemId)
                .ConfigureAwait(false);
            ApplyInheritedValues(requestedSystem, requestedDbSystem, ancestors);

            // Should inherited codes include the codes that are overriden in this system?
            // For now, going to say no.  but the call below does include those, so
            // need to remove them
            var allInheritedCodes = await GetAllCodesAsync(requestedSystem.ParentSystemId)
                .ConfigureAwait(false);
            requestedSystem.InheritedScoreCodes = allInheritedCodes
                .Where(c => !requestedSystem.ScoreCodes.Any(ec => ec.Name == c.Name));

            // Populate inherited score code groups
            var allInheritedGroups = await GetAllCodeGroupsAsync(requestedSystem.ParentSystemId)
                .ConfigureAwait(false);
            requestedSystem.InheritedScoreCodeGroups = allInheritedGroups;

            _cache.Set(cacheKey, requestedSystem, TimeSpan.FromMinutes(2));

            return requestedSystem;

        }

        public async Task<ScoringSystem> GetScoringSystemFromCacheAsync(Series series)
        {
            var systems = await GetScoringSystemsFromCacheAsync(new List<Series> { series })
                .ConfigureAwait(false);
            if (!systems.TryGetValue(series.Id, out var system))
            {
                throw new InvalidOperationException("Scoring system for series not found and default scoring system not found.");
            }
            return system;
        }

        // Series id => the system that scores it: the regatta's, else the series', else the
        // season default, else the club default. Series with none are left out. Uses a fixed
        // number of queries however many series are passed.
        public async Task<IDictionary<Guid, ScoringSystem>> GetScoringSystemsFromCacheAsync(
            IList<Series> seriesList)
        {
            var result = new Dictionary<Guid, ScoringSystem>();
            var misses = new List<Series>();
            foreach (var series in seriesList)
            {
                if (_cache.TryGetValue($"ScoringSystem-{series.Id}", out ScoringSystem cachedSystem))
                {
                    result[series.Id] = cachedSystem;
                }
                else
                {
                    misses.Add(series);
                }
            }
            if (misses.Count == 0)
            {
                return result;
            }

            var seriesIds = misses.Select(s => s.Id).ToList();
            var regattaRows = await _dbContext.Regattas
                .SelectMany(r => r.RegattaSeries
                    .Where(rs => seriesIds.Contains(rs.SeriesId))
                    .Select(rs => new { rs.SeriesId, r.ScoringSystemId }))
                .ToListAsync()
                .ConfigureAwait(false);
            var systemIds = new Dictionary<Guid, Guid?>();
            foreach (var series in misses)
            {
                var regattaRow = regattaRows.FirstOrDefault(r => r.SeriesId == series.Id);
                systemIds[series.Id] = regattaRow?.ScoringSystemId ?? series.ScoringSystemId;
            }

            var needSeasonDefault = misses.Where(s => systemIds[s.Id] == null)
                .Select(s => s.Id).ToList();
            if (needSeasonDefault.Count > 0)
            {
                var seasonDefaults = await _dbContext.Series
                    .Where(s => needSeasonDefault.Contains(s.Id))
                    .Select(s => new { s.Id, s.Season.DefaultScoringSystemId })
                    .ToListAsync()
                    .ConfigureAwait(false);
                foreach (var row in seasonDefaults)
                {
                    systemIds[row.Id] = row.DefaultScoringSystemId;
                }
            }

            var needClubDefault = misses.Where(s => systemIds[s.Id] == null).ToList();
            if (needClubDefault.Count > 0)
            {
                var clubIds = needClubDefault.Select(s => s.ClubId).Distinct().ToList();
                var clubDefaults = await _dbContext.Clubs
                    .Where(c => clubIds.Contains(c.Id))
                    .ToDictionaryAsync(c => c.Id, c => c.DefaultScoringSystemId)
                    .ConfigureAwait(false);
                foreach (var series in needClubDefault)
                {
                    systemIds[series.Id] = clubDefaults.GetValueOrDefault(series.ClubId);
                }
            }

            foreach (var series in misses)
            {
                var systemId = systemIds[series.Id];
                if (systemId == null)
                {
                    continue;
                }
                // Cached by system id, so series sharing a system load it once.
                var system = await GetScoringSystemAsync(systemId.Value, false)
                    .ConfigureAwait(false);
                _cache.Set($"ScoringSystem-{series.Id}", system, TimeSpan.FromMinutes(1));
                result[series.Id] = system;
            }
            return result;
        }

        public async Task SaveScoringSystemAsync(ScoringSystem scoringSystem)
        {
            if (scoringSystem.Id == Guid.Empty)
            {
                scoringSystem.Id = Guid.NewGuid();
            }
            var dbObject = await _dbContext.ScoringSystems
                .Where(s => s.Id == scoringSystem.Id).SingleOrDefaultAsync()
                .ConfigureAwait(false);
            if (dbObject == null)
            {
                dbObject = new Db.ScoringSystem { Id = scoringSystem.Id };
                _dbContext.ScoringSystems.Add(dbObject);
            }
            // Only scalar fields are copied. The context may already be tracking this
            // system's parent and score codes (e.g. after GetScoringSystemsAsync), and
            // overwriting those navigations would clear ParentSystemId and delete the codes.
            // Score codes are saved through SaveScoreCodeAsync; CalculatorType is only set
            // by migrations and scripts.
            dbObject.ClubId = scoringSystem.ClubId;
            dbObject.ParentSystemId = scoringSystem.ParentSystemId;
            dbObject.Name = scoringSystem.Name;
            dbObject.DiscardPattern = scoringSystem.DiscardPattern;
            dbObject.ParticipationPercent = scoringSystem.ParticipationPercent;
            dbObject.DiscardMethod = scoringSystem.DiscardMethod;
            dbObject.Settings = ScoringSystemSettings.ToJson(scoringSystem.Settings);
            await _dbContext.SaveChangesAsync().ConfigureAwait(false);
            InvalidateScoringSystemCache(scoringSystem.Id);
        }

        public async Task DeleteScoringSystemAsync(Guid systemId)
        {
            var scoreCodes = _dbContext.ScoreCodes
                .Where(c => c.ScoringSystemId == systemId);
            _dbContext.ScoreCodes.RemoveRange(scoreCodes);
            var system = await _dbContext.ScoringSystems
                .SingleAsync(s => s.Id == systemId)
                .ConfigureAwait(false);
            _dbContext.ScoringSystems.Remove(system);
            await _dbContext.SaveChangesAsync()
                .ConfigureAwait(false);
            InvalidateScoringSystemCache(systemId);
        }

        public async Task<ScoreCode> GetScoreCodeAsync(Guid id)
        {
            var dbObj = await _dbContext.ScoreCodes
                .Where(sc => sc.Id == id).FirstOrDefaultAsync()
                .ConfigureAwait(false);
            return _mapper.Map<ScoreCode>(dbObj);
        }

        public async Task SaveScoreCodeAsync(ScoreCode scoreCode)
        {
            Guid? scoringSystemId = null;
            if (scoreCode.Id != default)
            {
                Db.ScoreCode dbObject = await _dbContext
                    .ScoreCodes
                    .Where(sc => sc.Id == scoreCode.Id)
                    .SingleAsync()
                    .ConfigureAwait(false);
                scoringSystemId = dbObject.ScoringSystemId;
                _mapper.Map(scoreCode, dbObject);
            }
            else
            {
                Db.ScoreCode dbObject = _mapper.Map<Db.ScoreCode>(scoreCode);
                dbObject.Id = Guid.NewGuid();
                scoringSystemId = dbObject.ScoringSystemId;
                _dbContext.ScoreCodes.Add(dbObject);
            }
            await _dbContext.SaveChangesAsync()
                .ConfigureAwait(false);
            if (scoringSystemId.HasValue)
            {
                InvalidateScoringSystemCache(scoringSystemId.Value);
            }
        }

        public async Task DeleteScoreCodeAsync(Guid id)
        {
            var scoreCode = await _dbContext.ScoreCodes
                .SingleAsync(s => s.Id == id)
                .ConfigureAwait(false);
            var scoringSystemId = scoreCode.ScoringSystemId;
            _dbContext.ScoreCodes.Remove(scoreCode);
            await _dbContext.SaveChangesAsync()
                .ConfigureAwait(false);
            InvalidateScoringSystemCache(scoringSystemId);
        }

        public async Task<bool> IsScoringSystemInUseAsync(
            Guid scoringSystemId)
        {
            return (await _dbContext.Series.AnyAsync(s =>
                    s.ScoringSystemId == scoringSystemId)
                       .ConfigureAwait(false))
                || (await _dbContext.Clubs.AnyAsync(c =>
                    c.DefaultScoringSystemId == scoringSystemId)
                    .ConfigureAwait(false))
                || (await _dbContext.Seasons.AnyAsync(s =>
                    s.DefaultScoringSystemId == scoringSystemId)
                    .ConfigureAwait(false))
                || (await _dbContext.ScoringSystems.AnyAsync(s =>
                    s.ParentSystemId == scoringSystemId)
                    .ConfigureAwait(false));
        }

        // Returns the parent chain starting at parentId, nearest parent first, root last.
        // Systems already in `known` are not reloaded.
        private async Task<IList<Db.ScoringSystem>> GetAncestorSystemsAsync(
            Guid? parentId,
            IDictionary<Guid, Db.ScoringSystem> known = null)
        {
            var ancestors = new List<Db.ScoringSystem>();
            var visited = new HashSet<Guid>();
            while (parentId.HasValue && visited.Add(parentId.Value))
            {
                var currentId = parentId.Value;
                if (known == null || !known.TryGetValue(currentId, out var parent))
                {
                    parent = await _dbContext.ScoringSystems
                        .AsNoTracking()
                        .SingleAsync(s => s.Id == currentId)
                        .ConfigureAwait(false);
                    known?.Add(currentId, parent);
                }
                ancestors.Add(parent);
                parentId = parent.ParentSystemId;
            }
            return ancestors;
        }

        private async Task<IEnumerable<ScoreCode>> GetAllCodesAsync(
            Guid? systemId)
        {
            if (systemId == null)
            {
                return new List<Model.ScoreCode>();
            }
            var requestedDbSystem = await _dbContext
                .ScoringSystems
                .Include(s => s.ScoreCodes)
                .SingleAsync(s => s.Id == systemId)
                .ConfigureAwait(false);
            var thisSystemCodes = _mapper.Map<IList<ScoreCode>>(requestedDbSystem.ScoreCodes);

            var parentCodes = await GetAllCodesAsync(requestedDbSystem.ParentSystemId)
                .ConfigureAwait(false);
            var parentCodesToUse = parentCodes
                    .Where(pc => !requestedDbSystem.ScoreCodes.Any(cc => cc.Name == pc.Name))
                    .ToList();

            var returnCodes = thisSystemCodes.Concat(parentCodesToUse);

            return returnCodes;
        }

        public static IEnumerable<string> GetDiscardSequenceErrors(string discardSequence)
        {
            var discardCounts = discardSequence.Split(',');
            var raceCounter = 1;

            bool numberTooHigh = false;
            bool numberNotInt = false;

            foreach(var discardCount in discardCounts)
            {
                int parsed = 0;
                if(!int.TryParse(discardCount, out parsed))
                {
                    numberNotInt = true;
                } else
                {
                    if (parsed >= raceCounter)
                    {
                        numberTooHigh = true;
                    }
                }
                raceCounter++;
            }

            if (numberNotInt)
            {
                yield return "pattern should be whole numbers separated by commas ";
            }
            if (numberTooHigh)
            {
                yield return "number of discards should be less than the number of races ";
            }

        }

        public async Task<IEnumerable<DeletableInfo>> GetDeletableInfo(Guid clubId)
        {

            var systemId= _dbContext.ScoringSystems
                .Where(ss => ss.ClubId == clubId)
                .Select(ss => ss.Id);
            var seriesIdsInUser = await _dbContext.Series
                .Where(s => s.ClubId == clubId
                && s.ScoringSystemId != null)
                .Select(s => s.ScoringSystemId).ToListAsync();
            var regattaIdsInUse = await _dbContext.Regattas
                .Where(r => r.ClubId == clubId
                && r.ScoringSystemId != null)
                .Select(r => r.ScoringSystemId).ToListAsync();
            var clubScoring = await _dbContext.Clubs
                .Where(c => c.Id == clubId)
                .Select(s => s.DefaultScoringSystemId).ToListAsync();
            var parents = await _dbContext.ScoringSystems
                .Where(s => s.ClubId == clubId)
                .Select(s => s.ParentSystemId).ToListAsync();
            var inUse  = systemId.Select(s => new
            {
                Id = s,
                InUse =
                seriesIdsInUser.Contains(s) || clubScoring.Contains(s)
                || parents.Contains(s) || regattaIdsInUse.Contains(s)
            });

            return inUse.Select(s => new DeletableInfo
            {
                Id = s.Id,
                IsDeletable = !s.InUse,
                Reason = !s.InUse ? String.Empty
                    : "Scoring System is in use."
            });
        }

        public async Task<Guid?> GetClubDefaultScoringSystemId(Guid clubId)
        {
            return (await _dbContext.Clubs.Where(c => c.Id == clubId)
                .FirstOrDefaultAsync().ConfigureAwait(false))
                .DefaultScoringSystemId;
        }

        public async Task<IList<ScoringSystem>> CreateDefaultScoringSystemsAsync(Guid clubId, string clubInitials)
        {
            var createdSystems = new List<ScoringSystem>();

            var baseScoringSystem = await GetSiteDefaultSystemAsync().ConfigureAwait(false);
            if (baseScoringSystem != null)
            {
                var newScoringSystem = new ScoringSystem
                {
                    Id = Guid.NewGuid(),
                    ClubId = clubId,
                    ParentSystemId = baseScoringSystem.Id,
                    Name = $"{clubInitials} scoring based on App. A Rule 5.3",
                    DiscardPattern = "0"
                };
                await SaveScoringSystemAsync(newScoringSystem).ConfigureAwait(false);
                createdSystems.Add(newScoringSystem);
            }

            var regattaScoringSystem = await GetBaseRegattaSystemAsync().ConfigureAwait(false);
            if (regattaScoringSystem != null)
            {
                var newRegattaScoringSystem = new ScoringSystem
                {
                    Id = Guid.NewGuid(),
                    ClubId = clubId,
                    ParentSystemId = regattaScoringSystem.Id,
                    Name = $"{clubInitials} scoring based on App. A",
                    DiscardPattern = "0,1"
                };
                await SaveScoringSystemAsync(newRegattaScoringSystem).ConfigureAwait(false);
                createdSystems.Add(newRegattaScoringSystem);
            }

            return createdSystems;
        }

        private void InvalidateScoringSystemCache(Guid scoringSystemId)
        {
            var cacheKey = $"ScoringSystem-{scoringSystemId}";
            _cache.Remove(cacheKey);
        }

        public async Task<ScoreCodeGroup> GetScoreCodeGroupAsync(Guid id)
        {
            var dbGroup = await _dbContext.ScoreCodeGroups
                .Include(g => g.Codes)
                .FirstOrDefaultAsync(g => g.Id == id)
                .ConfigureAwait(false);

            if (dbGroup == null)
            {
                return null;
            }

            var includedCodeNames = dbGroup.Codes.Select(c => c.CodeName).ToList();
            return new ScoreCodeGroup(
                dbGroup.Id,
                dbGroup.ScoringSystemId,
                dbGroup.Name,
                (ScoreCodeGroupLimitationType)dbGroup.LimitationType,
                dbGroup.LimitationValue,
                dbGroup.UseNonDiscardedRaces,
                dbGroup.OverageCodeName,
                (ScoreCodeGroupOverageSelection)dbGroup.OverageSelectionMethod,
                includedCodeNames);
        }

        public async Task SaveScoreCodeGroupAsync(ScoreCodeGroup group)
        {
            if (group.Id == Guid.Empty)
            {
                group.Id = Guid.NewGuid();
            }

            var dbGroup = await _dbContext.ScoreCodeGroups
                .Include(g => g.Codes)
                .FirstOrDefaultAsync(g => g.Id == group.Id)
                .ConfigureAwait(false);

            if (dbGroup == null)
            {
                // Create new group
                dbGroup = new Db.ScoreCodeGroup(
                    group.Id,
                    group.ScoringSystemId,
                    group.Name,
                    (int)group.LimitationType,
                    group.LimitationValue,
                    group.UseNonDiscardedRaces,
                    group.OverageCodeName,
                    (int)group.OverageSelectionMethod);

                _dbContext.ScoreCodeGroups.Add(dbGroup);
            }
            else
            {
                // Update existing group
                dbGroup.Name = group.Name;
                dbGroup.LimitationType = (int)group.LimitationType;
                dbGroup.LimitationValue = group.LimitationValue;
                dbGroup.UseNonDiscardedRaces = group.UseNonDiscardedRaces;
                dbGroup.OverageCodeName = group.OverageCodeName;
                dbGroup.OverageSelectionMethod = (int)group.OverageSelectionMethod;
            }

            // Update codes
            _dbContext.ScoreCodeGroupCodes.RemoveRange(dbGroup.Codes);
            dbGroup.Codes = (group.IncludedCodeNames ?? new List<string>())
                .Select(codeName => new Db.ScoreCodeGroupCode { CodeName = codeName })
                .ToList();

            await _dbContext.SaveChangesAsync().ConfigureAwait(false);
            InvalidateScoringSystemCache(group.ScoringSystemId);
        }

        public async Task DeleteScoreCodeGroupAsync(Guid id)
        {
            var dbGroup = await _dbContext.ScoreCodeGroups
                .FirstOrDefaultAsync(g => g.Id == id)
                .ConfigureAwait(false);

            if (dbGroup != null)
            {
                var scoringSystemId = dbGroup.ScoringSystemId;
                _dbContext.ScoreCodeGroups.Remove(dbGroup);
                await _dbContext.SaveChangesAsync().ConfigureAwait(false);
                InvalidateScoringSystemCache(scoringSystemId);
            }
        }

        private async Task<IEnumerable<ScoreCodeGroup>> GetAllCodeGroupsAsync(Guid? parentSystemId)
        {
            if (!parentSystemId.HasValue)
            {
                return Enumerable.Empty<ScoreCodeGroup>();
            }

            var parentSystem = await GetScoringSystemAsync(parentSystemId.Value).ConfigureAwait(false);
            if (parentSystem == null)
            {
                return Enumerable.Empty<ScoreCodeGroup>();
            }

            var groups = parentSystem.ScoreCodeGroups ?? new List<ScoreCodeGroup>();
            var inheritedGroups = parentSystem.InheritedScoreCodeGroups ?? Enumerable.Empty<ScoreCodeGroup>();

            return groups.Concat(inheritedGroups);
        }
    }
}
