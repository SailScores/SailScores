using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SailScores.Core.Model;
using SailScores.Database;
using dbObj = SailScores.Database.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SailScores.Core.Services
{
    public class RegattaService : IRegattaService
    {
        private readonly ISeriesService _seriesService;
        private readonly IForwarderService _forwarderService;
        private readonly ISailScoresContext _dbContext;
        private readonly IDbObjectBuilder _dbObjectBuilder;
        private readonly IMapper _mapper;

        public RegattaService(
            ISeriesService seriesService,
            IForwarderService forwarderService,
            ISailScoresContext dbContext,
            IDbObjectBuilder dbObjBuilder,
            IMapper mapper)
        {
            _seriesService = seriesService;
            _forwarderService = forwarderService;
            _dbContext = dbContext;
            _dbObjectBuilder = dbObjBuilder;
            _mapper = mapper;
        }

        public async Task<IList<Regatta>> GetAllRegattasAsync(Guid clubId)
        {
            var regattaDb = await _dbContext
                .Clubs
                .Where(c => c.Id == clubId)
                .SelectMany(c => c.Regattas)
                .Include(c => c.Season)
                .Include(c => c.RegattaFleet)
                .OrderBy(r => r.StartDate)
                .AsSplitQuery()
                .ToListAsync()
                .ConfigureAwait(false);

            var returnObj = _mapper.Map<List<Regatta>>(regattaDb);
            return returnObj;
        }

        public async Task<IList<Regatta>> GetRegattasDuringSpanAsync(DateTime start, DateTime end)
        {
            var regattaDb = await _dbContext
                .Regattas
                .Where(r => r.StartDate <= end && (r.EndDate >= start || r.StartDate >= start))
                .Include(r => r.Season)
                .ToListAsync()
                .ConfigureAwait(false);

            var returnObj = _mapper.Map<List<Regatta>>(regattaDb);
            return returnObj;
        }

        public async Task<Regatta> GetRegattaAsync(Guid regattaId)
        {
            var regattaDb = await _dbContext
                .Regattas
                .Where(r =>
                    r.Id == regattaId)
                .Include(r => r.RegattaFleet)
                .ThenInclude(rf => rf.Fleet)
                .ThenInclude(f => f.CompetitorFleets)
                .ThenInclude(cf => cf.Competitor)
                .Include(r => r.RegattaSeries)
                .ThenInclude(rs => rs.Series)
                .Include(r => r.Season)
                .Include(r => r.Announcements)
                .AsSplitQuery()
                .SingleAsync()
                .ConfigureAwait(false);

            var fullRegatta = _mapper.Map<Regatta>(regattaDb);

            fullRegatta.Documents = await _dbContext.Documents.Where(
                d => d.RegattaId == regattaId)
                .Select(d => new Document
                {
                    Id = d.Id,
                    RegattaId = d.RegattaId,
                    ClubId = d.ClubId,
                    Name = d.Name,
                    CreatedDate = d.CreatedDate,
                    CreatedLocalDate = d.CreatedLocalDate,
                    CreatedBy = d.CreatedBy
                }).ToListAsync();

            foreach (var series in fullRegatta.Series)
            {
                series.FlatResults = await _seriesService.GetHistoricalResults(series)
                    .ConfigureAwait(false);
                series.PreferAlternativeSailNumbers = fullRegatta.PreferAlternateSailNumbers;
                series.ShowCompetitorClub = true;
            }
            return fullRegatta;
        }

        public async Task<Regatta> GetRegattaAsync(string clubInitials, string seasonName, string regattaName)
        {
            var clubId = await _dbContext.Clubs
                .Where(c =>
                   c.Initials == clubInitials
                ).Select(c => c.Id).SingleAsync()
                .ConfigureAwait(false);

            var regattaId = await _dbContext
                .Regattas
                .Where(r =>
                    r.ClubId == clubId &&
                    r.UrlName == regattaName &&
                    r.Season.UrlName == seasonName)
                .Select(r => r.Id)
                .SingleOrDefaultAsync()
                .ConfigureAwait(false);
            if (regattaId == default)
            {
                return null;
            }
            return await GetRegattaAsync(regattaId).ConfigureAwait(false);
        }

        public Task<Guid> SaveNewRegattaAsync(Regatta regatta)
        {
            if (regatta == null)
            {
                throw new ArgumentNullException(nameof(regatta));
            }
            return SaveNewRegattaInternalAsync(regatta);
        }

        private async Task<Guid> SaveNewRegattaInternalAsync(Regatta regatta) { 
            Database.Entities.Regatta dbRegatta =
                await _dbObjectBuilder.BuildDbRegattaAsync(regatta)
                .ConfigureAwait(false);
            dbRegatta.UrlName = UrlUtility.GetUrlName(dbRegatta.Name);
            dbRegatta.UpdatedDate = DateTime.UtcNow;
            if (dbRegatta.Season == null
                && regatta.Season.Id != Guid.Empty
                && regatta.Season.Start != default)
            {
                var season = _mapper.Map<dbObj.Season>(regatta.Season);
                _dbContext.Seasons.Add(season);
                dbRegatta.Season = season;
            }
            if (dbRegatta.Season == null)
            {
                throw new InvalidOperationException(
                    "Could not find or create season for new Regatta.");
            }

            if (_dbContext.Regattas.Any(s =>
                s.Id == regatta.Id
                || (s.ClubId == regatta.ClubId
                    && s.UrlName == regatta.UrlName
                    && s.Season.Id == dbRegatta.Season.Id)))
            {
                throw new InvalidOperationException(
                    "Cannot create regatta. A regatta with this name in this season already exists.");
            }

            _dbContext.Regattas.Add(dbRegatta);
            await _dbContext.SaveChangesAsync()
                .ConfigureAwait(false);
            return dbRegatta.Id;
        }

        public Task<Guid> UpdateAsync(Regatta model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }
            return UpdateInternalAsync(model);
        }

        private async Task<Guid> UpdateInternalAsync(Regatta model)
        {
            if (_dbContext.Regattas.Any(r =>
                r.Id != model.Id
                && r.ClubId == model.ClubId
                && r.Name == model.Name
                && r.Season.Id == model.Season.Id))
            {
                throw new InvalidOperationException(
                    "Cannot update Regatta. A regatta with this name in this season already exists.");
            }
            var existingRegatta = await _dbContext.Regattas
                .Include(r => r.RegattaFleet)
                .SingleAsync(c => c.Id == model.Id)
                .ConfigureAwait(false);

            if(!DoIdentifiersMatch(model, existingRegatta))
            {
                await _forwarderService.CreateRegattaForwarder(model, existingRegatta);
            }

            existingRegatta.Name = model.Name;
            // Now that forwarders are in place, we can update the url name.
            existingRegatta.UrlName = UrlUtility.GetUrlName(model.Name);
            existingRegatta.Url = model.Url;
            existingRegatta.Description = model.Description;
            existingRegatta.StartDate = model.StartDate;
            existingRegatta.EndDate = model.EndDate;
            existingRegatta.UpdatedDate = DateTime.UtcNow;
            existingRegatta.ScoringSystemId = model.ScoringSystemId;
            existingRegatta.PreferAlternateSailNumbers = model.PreferAlternateSailNumbers;

            if (model.Season != null
                && model.Season.Id != Guid.Empty
                && existingRegatta.Season?.Id != model.Season?.Id)
            {
                existingRegatta.Season = _dbContext.Seasons.Single(s => s.Id == model.Season.Id);
            }

            CleanupFleets(model, existingRegatta);

            await _dbContext.SaveChangesAsync()
                .ConfigureAwait(false);

            return existingRegatta.Id;
        }

        private bool DoIdentifiersMatch(Regatta model, dbObj.Regatta existingRegatta)
        {
            return model.Season.Id == existingRegatta.Season.Id
                && model.ClubId == existingRegatta.ClubId
                && model.Name == existingRegatta.Name;
        }

        private static void CleanupFleets(Regatta model, dbObj.Regatta existingRegatta)
        {
            var fleetsToRemove = new List<dbObj.RegattaFleet>();

            if (model.Fleets != null)
            {
                fleetsToRemove =
                    existingRegatta.RegattaFleet
                    .Where(f => !(model.Fleets.Any(f2 => f2.Id == f.FleetId)))
                    .ToList();
            }
            var fleetsToAdd =
                model.Fleets != null ?
                model.Fleets
                .Where(c =>
                    !(existingRegatta.RegattaFleet.Any(f => c.Id == f.FleetId)))
                    .Select(c => new dbObj.RegattaFleet
                    {
                        FleetId = c.Id,
                        RegattaId = existingRegatta.Id
                    })
                : new List<dbObj.RegattaFleet>();

            foreach (var removingFleet in fleetsToRemove)
            {
                existingRegatta.RegattaFleet.Remove(removingFleet);
            }
            foreach (var addFleet in fleetsToAdd)
            {
                existingRegatta.RegattaFleet.Add(addFleet);
            }
        }


        public async Task DeleteAsync(Guid regattaId)
        {
            var dbRegatta = await _dbContext.Regattas
                .Include(r => r.RegattaFleet)
                .Include(r => r.RegattaSeries)
                .SingleAsync(c => c.Id == regattaId)
                .ConfigureAwait(false);

            dbRegatta.RegattaFleet.Clear();
            dbRegatta.RegattaSeries.Clear();
            _dbContext.Regattas.Remove(dbRegatta);

            await _dbContext.SaveChangesAsync()
                .ConfigureAwait(false);
        }

        // based on the fleet of the race, find the correct series from the regatta
        // and then add race to that series. Create the series if necessary.
        public Task AddRaceToRegattaAsync(Race race, Guid regattaId)
        {
            if (race == null) { throw new ArgumentNullException(nameof(race));
            }

            return AddRaceToRegattaInternalAsync(race, regattaId);
        }

        private async Task AddRaceToRegattaInternalAsync(Race race, Guid regattaId)
        {
            var dbFleet = await _dbContext.Fleets.SingleAsync(f => f.Id == race.Fleet.Id)
                .ConfigureAwait(false);
            var dbRegatta = await _dbContext.Regattas
                .Include(r => r.Season)
                .Include(r => r.RegattaSeries)
                .SingleAsync(c => c.Id == regattaId)
                .ConfigureAwait(false);

            var series = await _dbContext.Regattas.SelectMany(r => r.RegattaSeries.Select(rs => rs.Series))
                .Include(s => s.RaceSeries)
                .SingleOrDefaultAsync(s => s.FleetId == dbFleet.Id)
                .ConfigureAwait(false);

            if (series == null)
            {
                // create a new series for this fleet.
                var seriesName = $"{dbRegatta.Season.Name} {dbRegatta.Name} {dbFleet.NickName}";
                series = new Database.Entities.Series
                {
                    ClubId = race.ClubId,
                    Name = seriesName,
                    Season = dbRegatta.Season,
                    UpdatedDate = DateTime.UtcNow,
                    ScoringSystem = dbRegatta.ScoringSystem,
                    TrendOption = Api.Enumerations.TrendOption.PreviousRace,
                    FleetId = dbFleet.Id,
                    RaceSeries = new List<Database.Entities.SeriesRace>(),
                    UrlName = UrlUtility.GetUrlName(seriesName)
                };
                dbRegatta.RegattaSeries.Add(new dbObj.RegattaSeries
                {
                    Regatta = dbRegatta,
                    Series = series
                });
                _dbContext.Series.Add(series);
            }
            series.RaceSeries.Add(new dbObj.SeriesRace
            {
                RaceId = race.Id,
                SeriesId = series.Id
            });

            await _dbContext.SaveChangesAsync().ConfigureAwait(false);
            await _seriesService.UpdateSeriesResults(series.Id, race.UpdatedBy)
                .ConfigureAwait(false);

        }

        public async Task AddFleetToRegattaAsync(Guid fleetId, Guid regattaId)
        {
            var exists = await _dbContext.Regattas.SelectMany(r => r.RegattaFleet)
                .AnyAsync(rf => rf.FleetId == fleetId && rf.RegattaId == regattaId)
                .ConfigureAwait(false);

            if (!exists)
            {
                var regatta = await _dbContext.Regattas
                    .Include(r => r.RegattaFleet)
                    .SingleAsync(r => r.Id == regattaId)
                    .ConfigureAwait(false);
                regatta.RegattaFleet.Add(new dbObj.RegattaFleet
                {
                    RegattaId = regattaId,
                    FleetId = fleetId
                });
                await _dbContext.SaveChangesAsync()
                    .ConfigureAwait(false);
            }
        }

        public async Task<Regatta> GetRegattaForRace(Guid raceId)
        {
            var seriesIds = _dbContext.Series
                .Where(s => s.RaceSeries.Any(rs => rs.RaceId == raceId))
                .Select(s => s.Id);

            var r = await _dbContext.Regattas
                .Where(r => r.RegattaSeries.Any(rs => seriesIds.Contains(rs.SeriesId)))
                .FirstOrDefaultAsync()
                .ConfigureAwait(false);
            return _mapper.Map<Regatta>(r);
        }

        public async Task<int> GetMaxFleetRaceNumberAsync(Guid regattaId, Guid fleetId)
        {
            var max = _dbContext.Regattas.Where(r => r.Id == regattaId)
                .SelectMany(r => r.RegattaSeries)
                .Select(rs => rs.Series)
                .SelectMany(s => s.RaceSeries)
                .Select(rs => rs.Race)
                .Max(r => (int?)r.Order) ?? 0;

            return max;
        }
    }
}
