using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SailScores.Database;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using AutoMapper.QueryableExtensions;
using System.Linq;
using SailScores.Core.Model;
using Db = SailScores.Database.Entities;
using SailScores.Api.Dtos;

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

        public async Task<IList<Model.Competitor>> GetCompetitorsAsync(
            Guid clubId,
            Guid? fleetId)
        {

            var dbObjects = _dbContext.Clubs
                .Where(c => c.Id == clubId)
                .SelectMany(c => c.Competitors);

            if (fleetId.HasValue && fleetId != Guid.Empty)
            {
                dbObjects = dbObjects
                    .Where(c => c.CompetitorFleets.Any(cf => cf.FleetId == fleetId));
            }

            var list = await dbObjects.ToListAsync();
            return _mapper.Map<List<Model.Competitor>>(list);
        }

        public async Task<Model.Competitor> GetCompetitorAsync(Guid id)
        {
            var competitor = await 
                _dbContext
                .Competitors
                .FirstOrDefaultAsync(c => c.Id == id);

            return _mapper.Map<Model.Competitor>(competitor);
        }

        public async Task SaveAsync(Competitor comp)
        {
            var dbObject = await _dbContext
                .Competitors
                .FirstOrDefaultAsync(
                    c =>
                    c.Id == comp.Id);
            var addingNew = dbObject == null;
            if(addingNew)
            {
                if(comp.Id == null || comp.Id == Guid.Empty)
                {
                    comp.Id = Guid.NewGuid();
                }
                dbObject = _mapper.Map<Db.Competitor>(comp);
                await _dbContext.Competitors.AddAsync(dbObject);
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


            if (comp.Fleets != null)
            {
                // remove fleets
                foreach (var existingFleet in dbObject.CompetitorFleets.ToList())
                {
                    if (!comp.Fleets.Any(f => f.Id == existingFleet.FleetId))
                    {
                        dbObject.CompetitorFleets.Remove(existingFleet);
                    }
                }

                // add fleets
                foreach (var fleet in comp.Fleets)
                {
                    if (!dbObject.CompetitorFleets.Any(
                        cf => cf.FleetId == fleet.Id)) {
                        var dbFleet = _dbContext.Fleets
                            .SingleOrDefault(f => f.Id == fleet.Id
                                && f.ClubId == comp.ClubId);
                        if (fleet != null)
                        {
                            dbObject.CompetitorFleets.Add(new Db.CompetitorFleet
                            {
                                Competitor = dbObject,
                                Fleet = dbFleet
                            });
                        }
                        //todo: create new fleets here if needed.
                    }
                }
                //add built in club fleets
                var autoAddFleets = _dbContext.Fleets
                    .Where(f => f.ClubId == comp.ClubId
                    && (f.FleetType == Api.Enumerations.FleetType.AllBoatsInClub
                    || (f.FleetType == Api.Enumerations.FleetType.SelectedClasses
                    && f.FleetBoatClasses.Any(c => c.BoatClassId == comp.BoatClassId))));
                foreach(var dbFleet in autoAddFleets)
                {
                    if (!dbObject.CompetitorFleets.Any(
                        cf => cf.FleetId == dbFleet.Id))
                    {
                        dbObject.CompetitorFleets.Add(
                            new Db.CompetitorFleet
                            {
                                Competitor = dbObject,
                                Fleet = dbFleet
                            });
                    }
                }
            }

            await _dbContext.SaveChangesAsync();

        }

        public async Task SaveAsync(CompetitorDto comp)
        {
            var dbObject = await _dbContext
                .Competitors
                .Include(c => c.CompetitorFleets)
                .FirstOrDefaultAsync(
                    c =>
                    c.Id == comp.Id);
            var addingNew = dbObject == null;
            if (addingNew)
            {
                if (comp.Id == null || comp.Id == Guid.Empty)
                {
                    comp.Id = Guid.NewGuid();
                }
                dbObject = _mapper.Map<Db.Competitor>(comp);
                await _dbContext.Competitors.AddAsync(dbObject);
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
            if(dbObject.CompetitorFleets == null)
            {
                dbObject.CompetitorFleets = new List<Db.CompetitorFleet>();
            }

            if (comp.FleetIds != null)
            {
                // remove fleets
                foreach (var existingFleet in dbObject.CompetitorFleets.ToList())
                {
                    if (!comp.FleetIds.Any(f => f == existingFleet.FleetId))
                    {
                        dbObject.CompetitorFleets.Remove(existingFleet);
                    }
                }

                // add fleets
                foreach (var fleetId in comp.FleetIds)
                {
                    if (!dbObject.CompetitorFleets.Any(
                        cf => cf.FleetId == fleetId))
                    {
                        var dbFleet = _dbContext.Fleets
                            .SingleOrDefault(f => f.Id == fleetId
                                && f.ClubId == comp.ClubId);
                        if (fleetId != null)
                        {
                            dbObject.CompetitorFleets.Add(new Db.CompetitorFleet
                            {
                                Competitor = dbObject,
                                CompetitorId = dbObject.Id,
                                Fleet = dbFleet,
                                FleetId = dbFleet.Id
                            });
                        }
                        //todo: create new fleets here if needed.
                    }
                }
                //add built in club fleets
                var autoAddFleets = _dbContext.Fleets
                    .Where(f => f.ClubId == comp.ClubId
                    && (f.FleetType == Api.Enumerations.FleetType.AllBoatsInClub
                    || (f.FleetType == Api.Enumerations.FleetType.SelectedClasses
                    && f.FleetBoatClasses.Any(c => c.BoatClassId == comp.BoatClassId))));
                foreach (var dbFleet in autoAddFleets)
                {
                    if (!dbObject.CompetitorFleets.Any(
                        cf => cf.FleetId == dbFleet.Id))
                    {
                        dbObject.CompetitorFleets.Add(
                            new Db.CompetitorFleet
                            {
                                Competitor = dbObject,
                                Fleet = dbFleet
                            });
                    }
                }
            }

            await _dbContext.SaveChangesAsync();
        }
    }
}
