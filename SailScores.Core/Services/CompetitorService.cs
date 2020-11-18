using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SailScores.Api.Dtos;
using SailScores.Core.Model;
using SailScores.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Db = SailScores.Database.Entities;

namespace SailScores.Core.Services
{
    public class CompetitorService : ICompetitorService
    {
        private readonly ISailScoresContext _dbContext;
        private readonly IMapper _mapper;

        public CompetitorService(
            ISailScoresContext dbContext,
            IMapper mapper)
        {
            _dbContext = dbContext;
            _mapper = mapper;
        }

        public async Task<IList<Model.Competitor>> GetInactiveCompetitorsAsync(
            Guid clubId,
            Guid? fleetId)
        {

            return await GetCompetitorsAsync(clubId, fleetId, true)
                .ConfigureAwait(false);
        }

        public async Task<IList<Model.Competitor>> GetCompetitorsAsync(
            Guid clubId,
            Guid? fleetId)
        {
            return await GetCompetitorsAsync(clubId, fleetId, false)
                .ConfigureAwait(false);
        }

        private async Task<IList<Model.Competitor>> GetCompetitorsAsync(
            Guid clubId,
            Guid? fleetId,
            bool onlyInactive)
        {

            var dbObjects = _dbContext.Clubs
                .Where(c => c.Id == clubId)
                .SelectMany(c => c.Competitors)
                .Where(c => (onlyInactive && !(c.IsActive ?? true))
                            || ((c.IsActive ?? true) && !onlyInactive));

            if (fleetId.HasValue && fleetId != Guid.Empty)
            {

                var fleet = await _dbContext.Fleets
                    .Include(f => f.FleetBoatClasses)
                    .FirstOrDefaultAsync(f =>
                       f.Id == fleetId
                       && f.ClubId == clubId)
                    .ConfigureAwait(false);
                if (fleet.FleetType == Api.Enumerations.FleetType.SelectedClasses)
                {
                    var classIds = fleet.FleetBoatClasses.Select(f => f.BoatClassId);
                    dbObjects = dbObjects
                        .Where(c => classIds.Contains(c.BoatClassId));
                }
                else if (fleet.FleetType == Api.Enumerations.FleetType.SelectedBoats)
                {
                    dbObjects = dbObjects
                        .Where(c => c.CompetitorFleets.Any(cf => cf.FleetId == fleetId));
                }
            }

            var list = await dbObjects
                .OrderBy(c => c.SailNumber)
                .ThenBy(c => c.Name)
                .ToListAsync()
                .ConfigureAwait(false);
            return _mapper.Map<List<Model.Competitor>>(list);
        }

        public async Task<Model.Competitor> GetCompetitorAsync(Guid id)
        {
            var competitor = await
                _dbContext
                .Competitors
                .Include(c => c.CompetitorFleets)
                .ThenInclude(cf => cf.Fleet)
                .FirstOrDefaultAsync(c => c.Id == id)
                .ConfigureAwait(false);
            
            return _mapper.Map<Model.Competitor>(competitor);
        }


        public async Task<Competitor> GetCompetitorBySailNumberAsync(Guid clubId, string sailNumber)
        {
            var competitor = await
                _dbContext
                .Competitors
                .FirstOrDefaultAsync(c =>
                    c.ClubId == clubId &&
                    c.SailNumber == sailNumber &&
                    (c.IsActive ?? true))
                .ConfigureAwait(false);

            return _mapper.Map<Model.Competitor>(competitor);
        }

        public Task SaveAsync(Model.Competitor comp)
        {
            if (comp == null)
            {
                throw new ArgumentNullException(nameof(comp));
            }

            return SaveInternalAsync(comp);
        }

        public Task SaveAsync(CompetitorDto comp)
        {
            if (comp == null)
            {
                throw new ArgumentNullException(nameof(comp));
            }

            return SaveInternalAsync(comp);
        }

        private async Task SaveInternalAsync(Model.Competitor comp)
        {
            var dbObject = await _dbContext
                .Competitors
                .Include(c => c.CompetitorFleets)
                .FirstOrDefaultAsync(
                    c =>
                    c.Id == comp.Id)
                .ConfigureAwait(false);

            if (dbObject == null)
            {
                if (comp.Id == Guid.Empty)
                {
                    comp.Id = Guid.NewGuid();
                }
                dbObject = _mapper.Map<Db.Competitor>(comp);
                await _dbContext.Competitors.AddAsync(dbObject)
                    .ConfigureAwait(false);
            }
            else
            {
                dbObject.Name = comp.Name;
                dbObject.SailNumber = comp.SailNumber;
                dbObject.AlternativeSailNumber = comp.AlternativeSailNumber;
                dbObject.BoatName = comp.BoatName;
                dbObject.Notes = comp.Notes;
                dbObject.IsActive = comp.IsActive;
                dbObject.HomeClubName = comp.HomeClubName;
                // should scores get added here?
                // I don't think so. Those will be recorded as a race update or scores update.
            }


            AddFleetsToDbObject(comp, dbObject);

            await _dbContext.SaveChangesAsync()
                .ConfigureAwait(false);

        }


        private async Task SaveInternalAsync(CompetitorDto comp)
        {
            var dbObject = await _dbContext
                .Competitors
                .Include(c => c.CompetitorFleets)
                .FirstOrDefaultAsync(
                    c =>
                        c.Id == comp.Id)
                .ConfigureAwait(false);

            if (dbObject == null)
            {
                if (comp.Id == Guid.Empty)
                {
                    comp.Id = Guid.NewGuid();
                }
                dbObject = _mapper.Map<Db.Competitor>(comp);
                await _dbContext.Competitors.AddAsync(dbObject)
                    .ConfigureAwait(false);
            }
            else
            {
                dbObject.Name = comp.Name;
                dbObject.SailNumber = comp.SailNumber;
                dbObject.AlternativeSailNumber = comp.AlternativeSailNumber;
                dbObject.BoatName = comp.BoatName;
                dbObject.Notes = comp.Notes;
                // should scores get added here?
                // I don't think so. Those will be recorded as a race update or scores update.
            }

            AddFleetsToDbObject(comp, dbObject);

            await _dbContext.SaveChangesAsync()
                .ConfigureAwait(false);

        }

        private void AddFleetsToDbObject(Competitor comp, Db.Competitor dbObject)
        {
            if (comp.Fleets == null)
            {
                return;
            }

            // remove fleets
            dbObject.CompetitorFleets ??= new List<Db.CompetitorFleet>();

            foreach (var existingFleet in dbObject.CompetitorFleets.ToList())
            {
                if (comp.Fleets.All(f => f.Id != existingFleet.FleetId))
                {
                    dbObject.CompetitorFleets.Remove(existingFleet);
                }
            }

            // add fleets
            foreach (var fleet in comp.Fleets)
            {
                if (dbObject.CompetitorFleets
                    .Any(cf => cf.FleetId == fleet.Id))
                {
                    // already there, so skip.
                    continue;
                }
                var dbFleet = _dbContext.Fleets
                    .SingleOrDefault(f => f.Id == fleet.Id
                                      && f.ClubId == comp.ClubId
                                      && f.FleetType != Api.Enumerations.FleetType.AllBoatsInClub
                                      && f.FleetType != Api.Enumerations.FleetType.SelectedClasses);
                if (dbFleet != null)
                {
                    dbObject.CompetitorFleets.Add(new Db.CompetitorFleet
                    {
                        Competitor = dbObject,
                        Fleet = dbFleet
                    });
                }

                //todo: create new fleets here if needed.
            }

            //add built in club fleets
            var autoAddFleets = GetClubAutomaticFleets(
                comp.ClubId,
                comp.BoatClassId);
            foreach (var dbFleet in autoAddFleets)
            {
                if (dbObject.CompetitorFleets
                    .Any(cf => cf.FleetId == dbFleet.Id))
                {
                    // already there, move on.
                    continue;
                }
                dbObject.CompetitorFleets.Add(
                    new Db.CompetitorFleet
                    {
                        Competitor = dbObject,
                        Fleet = dbFleet
                    });
            }

        }

        private void AddFleetsToDbObject(CompetitorDto comp, Db.Competitor dbObject)
        {
            dbObject.CompetitorFleets ??= new List<Db.CompetitorFleet>();

            if (comp.FleetIds == null) return;
            // remove fleets
            foreach (var existingFleet in dbObject.CompetitorFleets.ToList())
            {
                if (comp.FleetIds.All(f => f != existingFleet.FleetId))
                {
                    dbObject.CompetitorFleets.Remove(existingFleet);
                }
            }

            // add fleets
            foreach (var fleetId in comp.FleetIds)
            {
                if (dbObject.CompetitorFleets
                    .Any(cf => cf.FleetId == fleetId))
                {
                    continue;
                }
                var dbFleet = _dbContext.Fleets
                    .SingleOrDefault(f => f.Id == fleetId
                                          && f.ClubId == comp.ClubId);
                if (dbFleet != null)
                {
                    dbObject.CompetitorFleets.Add(new Db.CompetitorFleet
                    {
                        Competitor = dbObject,
                        CompetitorId = dbObject.Id,
                        Fleet = dbFleet,
                        FleetId = dbFleet.Id
                    });
                }

                // Create new fleets here if needed.
            }

            //add built in club fleets
            var autoAddFleets = GetClubAutomaticFleets(
                comp.ClubId,
                comp.BoatClassId);
            foreach (var dbFleet in autoAddFleets)
            {
                if (dbObject.CompetitorFleets
                    .Any(cf => cf.FleetId == dbFleet.Id))
                {
                    continue;
                }
                dbObject.CompetitorFleets.Add(
                    new Db.CompetitorFleet
                    {
                        Competitor = dbObject,
                        Fleet = dbFleet
                    });
            }
        }


        private IQueryable<Db.Fleet> GetClubAutomaticFleets(
            Guid clubId,
            Guid boatClassId)
        {
            return _dbContext.Fleets
                .Where(f => f.ClubId == clubId)
                .Where(f =>
                    f.FleetType == Api.Enumerations.FleetType.AllBoatsInClub
                   || (f.FleetType == Api.Enumerations.FleetType.SelectedClasses
                       && f.FleetBoatClasses.Any(c => c.BoatClassId == boatClassId)));
        }

        public async Task DeleteCompetitorAsync(Guid competitorId)
        {
            var dbComp = await _dbContext
                .Competitors
                .SingleAsync(c => c.Id == competitorId)
                .ConfigureAwait(false);
            _dbContext.Competitors.Remove(dbComp);
            await _dbContext.SaveChangesAsync()
                .ConfigureAwait(false);
        }

        public async Task<IList<CompetitorSeasonStats>> GetCompetitorStatsAsync(Guid clubId, Guid competitorId)
        {
            var seasonSummaries = await _dbContext.GetCompetitorStatsSummaryAsync(clubId, competitorId)
                .ConfigureAwait(false);

            var returnList = new List<CompetitorSeasonStats>();
            foreach (var season in seasonSummaries.OrderByDescending(s => s.SeasonStart))
            {
                var seasonStats = new CompetitorSeasonStats
                {
                    SeasonName = season.SeasonName,
                    SeasonUrlName = season.SeasonUrlName,
                    SeasonStart = season.SeasonStart,
                    SeasonEnd = season.SeasonEnd,
                    RaceCount = season.RaceCount,
                    AverageFinishPlace = season.AverageFinishRank,
                    DaysRaced = season.DaysRaced,
                    BoatsRacedAgainst = season.BoatsRacedAgainst,
                    BoatsBeat = season.BoatsBeat,
                };
                returnList.Add(seasonStats);
            }
            return returnList;
        }

#pragma warning disable CA1054 // Uri parameters should not be strings
        public async Task<IList<PlaceCount>> GetCompetitorSeasonRanksAsync(
            Guid competitorId,
            string seasonUrlName)
#pragma warning restore CA1054 // Uri parameters should not be strings
        {
            var ranks = await _dbContext.GetCompetitorRankCountsAsync(
                competitorId,
                seasonUrlName)
                .ConfigureAwait(false);
            return _mapper.Map<IList<PlaceCount>>(ranks
                .OrderBy(r => r.Place ?? 100).ThenBy(r => r.Code));
        }
    }
}
