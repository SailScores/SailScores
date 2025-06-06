﻿using AutoMapper;
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
    public class FleetService : IFleetService
    {
        private readonly ISailScoresContext _dbContext;
        private readonly IMapper _mapper;

        public FleetService(
            ISailScoresContext dbContext,
            IMapper mapper)
        {
            _dbContext = dbContext;
            _mapper = mapper;
        }

        public async Task<Guid> SaveNew(Fleet fleet)
        {

            if (_dbContext.Fleets.Any(f =>
                f.ClubId == fleet.ClubId
                && (f.ShortName == fleet.ShortName
                || f.Name == fleet.Name)))
            {
                throw new InvalidOperationException("Cannot create fleet. A fleet with this name or short name already exists.");
            }
            var dbFleet = _mapper.Map<Db.Fleet>(fleet);
            dbFleet.Id = Guid.NewGuid();
            dbFleet.FleetBoatClasses = new List<Db.FleetBoatClass>();
            if ((fleet.BoatClasses?.Count ?? 0) != 0)
            {
                foreach (var newClass in fleet.BoatClasses)
                {
                    dbFleet.FleetBoatClasses.Add(
                        new Db.FleetBoatClass
                        {
                            BoatClassId = newClass.Id,
                            FleetId = dbFleet.Id
                        });
                }
            }
            dbFleet.CompetitorFleets = new List<Db.CompetitorFleet>();
            if ((fleet.Competitors?.Count ?? 0) != 0)
            {
                foreach (var newComp in fleet.Competitors)
                {
                    dbFleet.CompetitorFleets.Add(
                        new Db.CompetitorFleet
                        {
                            CompetitorId = newComp.Id,
                            FleetId = dbFleet.Id
                        });
                }
            }
            _dbContext.Fleets.Add(dbFleet);

            await _dbContext.SaveChangesAsync()
                .ConfigureAwait(false);
            return dbFleet.Id;

        }

        public async Task Update(Fleet fleet)
        {
            if (_dbContext.Fleets.Any(f =>
                f.Id != fleet.Id
                && f.ClubId == fleet.ClubId
                && (f.ShortName == fleet.ShortName
                || f.Name == fleet.Name)))
            {
                throw new InvalidOperationException("Cannot update fleet. A fleet with this name or short name already exists.");
            }
            var existingFleet = await _dbContext.Fleets
                .Include(f => f.FleetBoatClasses)
                .Include(f => f.CompetitorFleets)
                .AsSingleQuery()
                .SingleAsync(c => c.Id == fleet.Id)
                .ConfigureAwait(false);

            existingFleet.ShortName = fleet.ShortName;
            existingFleet.Name = fleet.Name;
            existingFleet.NickName = fleet.NickName;
            existingFleet.Description = fleet.Description;
            existingFleet.FleetType = fleet.FleetType;
            existingFleet.IsActive = fleet.IsActive;

            CleanUpClasses(fleet, existingFleet);
            CleanUpCompetitors(fleet, existingFleet);

            await _dbContext.SaveChangesAsync()
                .ConfigureAwait(false);
        }

        public async Task Delete(Guid fleetId)
        {
            var dbClass = await _dbContext.Fleets
                .Include(f => f.FleetBoatClasses)
                .Include(f => f.CompetitorFleets)
                .AsSingleQuery()
                .SingleAsync(c => c.Id == fleetId)
                .ConfigureAwait(false);
            foreach (var link in dbClass.FleetBoatClasses.ToList())
            {
                dbClass.FleetBoatClasses.Remove(link);
            }
            foreach (var link in dbClass.CompetitorFleets?.ToList())
            {
                dbClass.CompetitorFleets.Remove(link);
            }
            _dbContext.Fleets.Remove(dbClass);

            await _dbContext.SaveChangesAsync()
                .ConfigureAwait(false);
        }

        public async Task<Fleet> Get(Guid fleetId)
        {
            var dbFleet = await _dbContext.Fleets
                .Include(f => f.FleetBoatClasses)
                .ThenInclude(fbc => fbc.BoatClass)
                .Include(f => f.CompetitorFleets)
                .ThenInclude(fcf => fcf.Competitor)
                .AsSplitQuery()
                .SingleAsync(c => c.Id == fleetId)
                .ConfigureAwait(false);

            return _mapper.Map<Fleet>(dbFleet);
        }

        public async Task<IEnumerable<Fleet>> GetAllFleetsForClub(Guid clubId)
        {
            var dbFleets = _dbContext.Fleets
                .Include(f => f.FleetBoatClasses)
                .Where(c => c.ClubId == clubId);
            return _mapper.Map<IEnumerable<Fleet>>(dbFleets);
        }

        public async Task<IEnumerable<Series>> GetSeriesForFleet(Guid fleetId)
        {
            var dbSeries = _dbContext.Series
                .Include(s => s.Season)
                .Include(s => s.RaceSeries)
                .ThenInclude(rs => rs.Race)
                .Where(
                s => s.RaceSeries.Any(rs => rs.Race.Fleet.Id == fleetId));
            return _mapper.Map<IEnumerable<Series>>(dbSeries);
        }


        private static void CleanUpClasses(Fleet fleet, Db.Fleet existingFleet)
        {
            var classesToRemove = existingFleet.FleetBoatClasses.ToList();

            if (existingFleet.FleetType == Api.Enumerations.FleetType.SelectedClasses
                && fleet.BoatClasses != null)
            {
                classesToRemove =
                existingFleet.FleetBoatClasses
                .Where(f => !(fleet.BoatClasses.Any(c => c.Id == f.BoatClassId)))
                .ToList();
            }
            var classesToAdd =
                fleet.BoatClasses != null ?
                fleet.BoatClasses
                .Where(c =>
                    !(existingFleet.FleetBoatClasses.Any(f => c.Id == f.BoatClassId)))
                    .Select(c => new Db.FleetBoatClass { BoatClassId = c.Id, FleetId = existingFleet.Id })
                : new List<Db.FleetBoatClass>();

            foreach (var removingClass in classesToRemove)
            {
                existingFleet.FleetBoatClasses.Remove(removingClass);
            }
            foreach (var addClass in classesToAdd)
            {
                existingFleet.FleetBoatClasses.Add(addClass);
            }
        }

        private static void CleanUpCompetitors(Fleet fleet, Db.Fleet existingFleet)
        {
            var competitorsToRemove = existingFleet.CompetitorFleets.ToList();
            var competitorsToAdd =
                fleet.Competitors != null ?
                fleet.Competitors
                .Where(c =>
                    !(existingFleet.CompetitorFleets.Any(f => c.Id == f.CompetitorId)))
                    .Select(c => new Db.CompetitorFleet { CompetitorId = c.Id, FleetId = existingFleet.Id })
                : new List<Db.CompetitorFleet>();

            if (existingFleet.FleetType == Api.Enumerations.FleetType.SelectedBoats
                && fleet.Competitors != null)
            {
                competitorsToRemove =
                existingFleet.CompetitorFleets
                .Where(c => !(fleet.Competitors.Any(cp => cp.Id == c.CompetitorId)))
                .ToList();
            }


            foreach (var removingCompetitor in competitorsToRemove)
            {
                existingFleet.CompetitorFleets.Remove(removingCompetitor);
            }
            foreach (var addCompetitor in competitorsToAdd)
            {
                existingFleet.CompetitorFleets.Add(addCompetitor);
            }
        }

        public async Task<IEnumerable<DeletableInfo>> GetDeletableInfo(Guid clubId)
        {
            var usedFleets = _dbContext.Races.Select(r => r.Fleet.Id).Distinct();
            return _dbContext.Fleets.Where(f => f.ClubId == clubId)
                .Select(f => new DeletableInfo
                {
                    Id = f.Id,
                    IsDeletable = !usedFleets.Contains(f.Id)   
                });
        }

        public async Task<IDictionary<Guid, IEnumerable<Guid>>> GetClubRegattaFleets(Guid clubId)
        {
            // build a dictionary of fleet Ids to regatta Ids
            // where the fleet is in the regatta
            // and the regatta is in the club

            var regattaFleets = _dbContext.Regattas
                .Where(r => r.ClubId == clubId)
                .SelectMany(r => r.RegattaFleet);
            return await regattaFleets
                .GroupBy(r => r.FleetId)
                .Select(g => new
                {
                    FleetId = g.Key,
                    RegattaIds = g.Select(r => r.RegattaId)
                })
                .ToDictionaryAsync(g => g.FleetId, g => g.RegattaIds);
        }
    }
}
