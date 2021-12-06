﻿using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SailScores.Database;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using SailScores.Core.Model;
using Db = SailScores.Database.Entities;

namespace SailScores.Core.Services
{
    public class ScoringService : IScoringService
    {
        private readonly ISailScoresContext _dbContext;
        private readonly IMapper _mapper;

        public ScoringService(
            ISailScoresContext dbContext,
            IMapper mapper)
        {
            _dbContext = dbContext;
            _mapper = mapper;
        }

        public async Task<IEnumerable<Model.ScoreCode>> GetScoreCodesAsync(Guid clubId)
        {
            var allScoreCodes = await _dbContext
                .ScoringSystems
                .Where(s => s.ClubId == clubId || s.ClubId == null)
                .SelectMany(s => s.ScoreCodes).ToListAsync()
                .ConfigureAwait(false);
            var distinctScoreCodes = allScoreCodes
                .GroupBy(s => s.Name, (key, g) => g.OrderBy(e => e.Name).First());

            return _mapper.Map<List<Model.ScoreCode>>(distinctScoreCodes);
        }

        public async Task<IList<Model.ScoringSystem>> GetScoringSystemsAsync(
            Guid clubId,
            bool includeBaseSystems)
        {
            var dbObjects = await _dbContext
                .ScoringSystems
                .Where(s => s.ClubId == clubId ||
                (includeBaseSystems && s.ClubId == null))
                .Include(s => s.ScoreCodes)
                .ToListAsync()
                .ConfigureAwait(false);
            return _mapper.Map<List<Model.ScoringSystem>>(dbObjects);
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
            return _mapper.Map<Model.ScoringSystem>(dbObject);
        }

        // This is a big one: returns a scoring system, including inherited ScoreCodes.
        // So all score codes that will work in this scoring system will be returned.
        public async Task<ScoringSystem> GetScoringSystemAsync(Guid scoringSystemId)
        {
            var requestedDbSystem = await _dbContext
                .ScoringSystems
                .Include(s => s.ScoreCodes)
                .SingleAsync(s => s.Id == scoringSystemId)
                .ConfigureAwait(false);

            var requestedSystem = _mapper.Map<Model.ScoringSystem>(requestedDbSystem);

            // Should inherited codes include the codes that are overriden in this system?
            // For now, going to say no.  but the call below does include those, so
            // need to remove them
            var allInheritedCodes = await GetAllCodesAsync(requestedSystem.ParentSystemId)
                .ConfigureAwait(false);
            requestedSystem.InheritedScoreCodes = allInheritedCodes
                .Where(c => !requestedSystem.ScoreCodes.Any(ec => ec.Name == c.Name));

            return requestedSystem;

        }

        public async Task<ScoringSystem> GetScoringSystemAsync(Series series)
        {
            Guid? scoringSystemId = series.ScoringSystemId;
            var regatta = await _dbContext.Regattas
                .FirstOrDefaultAsync(r =>
                    r.RegattaSeries.Any(rs => rs.SeriesId == series.Id))
                .ConfigureAwait(false);
            scoringSystemId = regatta?.ScoringSystemId ?? scoringSystemId;
            if (scoringSystemId == null)
            {
                scoringSystemId = (await _dbContext.Clubs.Where(c => c.Id == series.ClubId)
                    .FirstOrDefaultAsync().ConfigureAwait(false))
                    .DefaultScoringSystemId;
            }
            if (scoringSystemId == null)
            {
                throw new InvalidOperationException("Scoring system for series not found and club default scoring system not found.");
            }
            return await GetScoringSystemAsync(scoringSystemId.Value)
                .ConfigureAwait(false);
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
                var newDbObject = _mapper.Map<Db.ScoringSystem>(scoringSystem);
                _dbContext.ScoringSystems.Add(newDbObject);
            }
            else
            {
                _mapper.Map(scoringSystem, dbObject);
            }
            await _dbContext.SaveChangesAsync().ConfigureAwait(false);
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
            if (scoreCode.Id != default)
            {
                Db.ScoreCode dbObject = await _dbContext
                    .ScoreCodes
                    .Where(sc => sc.Id == scoreCode.Id)
                    .SingleAsync()
                    .ConfigureAwait(false);
                _mapper.Map(scoreCode, dbObject);
            }
            else
            {
                Db.ScoreCode dbObject = _mapper.Map<Db.ScoreCode>(scoreCode);
                dbObject.Id = Guid.NewGuid();
                _dbContext.ScoreCodes.Add(dbObject);
            }
            await _dbContext.SaveChangesAsync()
                .ConfigureAwait(false);
        }

        public async Task DeleteScoreCodeAsync(Guid id)
        {
            var scoreCode = await _dbContext.ScoreCodes
                .SingleAsync(s => s.Id == id)
                .ConfigureAwait(false);
            _dbContext.ScoreCodes.Remove(scoreCode);
            await _dbContext.SaveChangesAsync()
                .ConfigureAwait(false);
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
                || (await _dbContext.ScoringSystems.AnyAsync(s =>
                    s.ParentSystemId == scoringSystemId)
                    .ConfigureAwait(false));
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
    }
}
