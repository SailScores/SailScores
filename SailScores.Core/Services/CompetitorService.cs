using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SailScores.Api.Dtos;
using SailScores.Core.Model;
using SailScores.Core.Utility;
using SailScores.Database;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Db = SailScores.Database.Entities;

namespace SailScores.Core.Services;

public class CompetitorService : ICompetitorService
{
    private readonly ISailScoresContext _dbContext;
    private readonly IForwarderService _forwarderService;
    private readonly IMapper _mapper;

    public string CompetitorSequenceName = "Competitor";

    public CompetitorService(
        ISailScoresContext dbContext,
        IForwarderService forwarderService,
        IMapper mapper)
    {
        _dbContext = dbContext;
        _forwarderService = forwarderService;
        _mapper = mapper;
    }


    public async Task<IList<Model.Competitor>> GetCompetitorsAsync(
        Guid clubId,
        Guid? fleetId,
        bool includeInactive)
    {

        var dbObjects = _dbContext.Clubs
            .Where(c => c.Id == clubId)
            .SelectMany(c => c.Competitors)
            .Where(c => (includeInactive || (c.IsActive ?? true)));

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
            //.OrderBy(c => c.SailNumber)
            //.ThenBy(c => c.Name)
            .Include(c => c.BoatClass)
            .ToListAsync()
            .ConfigureAwait(false);
        var modelList = _mapper.Map<List<Model.Competitor>>(list);
        modelList.Sort();
        return modelList;
    }

    public async Task<Model.Competitor> GetCompetitorAsync(Guid id)
    {
        var competitor = await
            _dbContext
                .Competitors
                .Include(c => c.CompetitorFleets)
                .ThenInclude(cf => cf.Fleet)
                .Include(c => c.ChangeHistory)
                .AsSingleQuery()
                .FirstOrDefaultAsync(c => c.Id == id)
                .ConfigureAwait(false);

        return _mapper.Map<Model.Competitor>(competitor);
    }


    public async Task<Competitor> GetCompetitorByUrlNameAsync(Guid clubId, string urlName)
    {
        var comps = await GetCompetitorsAsync(clubId, null, true);

        var comp = comps.Where(c => c.IsActive)
            .FirstOrDefault(c => c.UrlName == urlName);
        comp ??= comps.Where(c => !c.IsActive)
            .FirstOrDefault(c => c.UrlName == urlName);
        comp ??= comps.FirstOrDefault(c =>
            String.Equals(UrlUtility.GetUrlName(c.SailNumber), urlName, StringComparison.OrdinalIgnoreCase));

        return comp;
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

    public Task SaveAsync(Model.Competitor comp,
        string userName = "")
    {
        ArgumentNullException.ThrowIfNull(comp);

        return SaveInternalAsync(comp, userName);
    }

    public Task SaveAsync(
        CompetitorDto comp,
        string userName = "")
    {
        ArgumentNullException.ThrowIfNull(comp);

        return SaveInternalAsync(_mapper.Map<Model.Competitor>(comp), userName);
    }

    private async Task SaveInternalAsync(
        Model.Competitor comp,
        string userName = "")
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
                comp.UrlId = await GetNextCompetitorSequence(comp.ClubId)
                    .ConfigureAwait(false);
                comp.Created = DateTime.UtcNow;
            }

            dbObject = _mapper.Map<Db.Competitor>(comp);
            dbObject.ChangeHistory = new List<Db.CompetitorChange>();
            dbObject.ChangeHistory.Add(GetCompCreatedChange(userName));
            await _dbContext.Competitors.AddAsync(dbObject)
                .ConfigureAwait(false);
        }
        else
        {
            if(dbObject.ChangeHistory == null)
            {
                dbObject.ChangeHistory = new List<Db.CompetitorChange>();
            }
            if (dbObject.Name != comp.Name)
            {
                dbObject.ChangeHistory.Add(new Db.CompetitorChange
                {
                    ChangeTimeStamp = DateTime.UtcNow,
                    ChangeTypeId = Db.ChangeType.PropertyChangedId,
                    ChangedBy = userName,
                    NewValue = comp.Name,
                    Summary = "Competitor Name Changed"
                });
                dbObject.Name = comp.Name;
            }
            if(dbObject.SailNumber != comp.SailNumber)
            {
                dbObject.ChangeHistory.Add(new Db.CompetitorChange
                {
                    ChangeTimeStamp = DateTime.UtcNow,
                    ChangeTypeId = Db.ChangeType.PropertyChangedId,
                    ChangedBy = userName,
                    NewValue = comp.SailNumber,
                    Summary = "Sail Number Changed"
                });
                dbObject.SailNumber = comp.SailNumber;
            }
            if(dbObject.AlternativeSailNumber != comp.AlternativeSailNumber)
            {
                dbObject.ChangeHistory.Add(new Db.CompetitorChange
                {
                    ChangeTimeStamp = DateTime.UtcNow,
                    ChangeTypeId = Db.ChangeType.PropertyChangedId,
                    ChangedBy = userName,
                    NewValue = comp.AlternativeSailNumber,
                    Summary = "Alternative Sail Number Changed"
                });
                dbObject.AlternativeSailNumber = comp.AlternativeSailNumber;
            }
            if(dbObject.BoatName != comp.BoatName)
            {
                dbObject.ChangeHistory.Add(new Db.CompetitorChange
                {
                    ChangeTimeStamp = DateTime.UtcNow,
                    ChangeTypeId = Db.ChangeType.PropertyChangedId,
                    ChangedBy = userName,
                    NewValue = comp.BoatName,
                    Summary = "Boat Name Changed"
                });
                dbObject.BoatName = comp.BoatName;
            }
            if((dbObject.IsActive ?? true) && !comp.IsActive)
            {
                dbObject.ChangeHistory.Add(new Db.CompetitorChange
                {
                    ChangeTimeStamp = DateTime.UtcNow,
                    ChangeTypeId = Db.ChangeType.DeactivatedId,
                    ChangedBy = userName,
                    NewValue = String.Empty,
                    Summary = "Competitor Deactivated"
                });

                dbObject.IsActive = comp.IsActive;
            } else if((dbObject.IsActive == false) && comp.IsActive)
            {
                dbObject.ChangeHistory.Add(new Db.CompetitorChange
                {
                    ChangeTimeStamp = DateTime.UtcNow,
                    ChangeTypeId = Db.ChangeType.ActivatedId,
                    ChangedBy = userName,
                    NewValue = String.Empty,
                    Summary = "Competitor Activated"
                });

                dbObject.IsActive = comp.IsActive;
            }

            dbObject.HomeClubName = comp.HomeClubName;
            dbObject.Notes = comp.Notes;
            if (dbObject.UrlId == null)
            {
                dbObject.UrlId = await GetNextCompetitorSequence(comp.ClubId)
                   .ConfigureAwait(false);
            }
        }

        await SetForwardersAndUrlName(dbObject, comp)
            .ConfigureAwait(false);

        AddFleetsToDbObject(comp, dbObject);

        await _dbContext.SaveChangesAsync()
            .ConfigureAwait(false);

    }

    private Db.CompetitorChange GetCompCreatedChange(
        string userName)
    {
        return new Db.CompetitorChange
        {
            ChangeTimeStamp = DateTime.UtcNow,
            ChangeTypeId = Db.ChangeType.CreatedId,
            ChangedBy = userName,
            NewValue = String.Empty,
            Summary = "Competitor Created"
        };
    }

    private Db.CompetitorChange GetCompDeletedChange(
    string userName)
    {
        return new Db.CompetitorChange
        {
            ChangeTimeStamp = DateTime.UtcNow,
            ChangeTypeId = Db.ChangeType.DeletedId,
            ChangedBy = userName,
            NewValue = String.Empty,
            Summary = "Competitor Deleted"
        };
    }

    private async Task SetForwardersAndUrlName(Db.Competitor dbObject, Competitor comp)
    {
        var proposedIdentifier = String.IsNullOrEmpty(UrlUtility.GetUrlName(comp.SailNumber)) ? comp.Name.Left(20) : comp.SailNumber;
        proposedIdentifier = UrlUtility.GetUrlName(proposedIdentifier);

        string newId = dbObject.UrlId;
        if (!String.IsNullOrEmpty(proposedIdentifier))
        {
            var duplicateCount = await _dbContext.Competitors
                .CountAsync(c => c.ClubId == comp.ClubId
                    && c.UrlName == proposedIdentifier
                    && c.Id != dbObject.Id)
                .ConfigureAwait(false);
            newId = duplicateCount == 0 ? proposedIdentifier : dbObject.UrlId;
        }


        // if urlName is changing, and not newly created, set a forwarder.
        if(dbObject.UrlName != newId && !String.IsNullOrEmpty(dbObject.UrlName))
        {
            comp.UrlName = newId;
            await _forwarderService.CreateCompetitorForwarder(comp, dbObject)
                .ConfigureAwait(false);
        }

        dbObject.UrlName = newId;
    }

    /// <summary>
    /// This method is not thread-safe. Could convert to a sql command /transaction for thread safety
    /// </summary>
    private async Task<string> GetNextCompetitorSequence(Guid clubId)
    {
        var sequence = await _dbContext.ClubSequences
            .Where(cs => cs.ClubId.Equals(clubId)
            && cs.SequenceType == CompetitorSequenceName)
            .FirstOrDefaultAsync();
        if(sequence == null)
        {
            sequence = new Db.ClubSequence
            {
                ClubId = clubId,
                SequenceType = CompetitorSequenceName,
                SequencePrefix = "id",
                SequenceSuffix = "",
                NextValue = 1
            };
            _dbContext.ClubSequences.Add(sequence);
        }
        var returnValue = $"{sequence.SequencePrefix}{sequence.NextValue}{sequence.SequenceSuffix}";
        sequence.NextValue++;
        await _dbContext.SaveChangesAsync();
        return returnValue;
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
            returnList.Add(_mapper.Map<CompetitorSeasonStats>(season));
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

    public async Task<IDictionary<Guid, Tuple<DateTime?, DateTime?>>> GetCompetitorActiveDates2(Guid clubId)
    {
        // get the min and max race dates for each competitor
        var dates = await _dbContext.Competitors
                .Where(c => c.ClubId == clubId)
                .Where(c => c.Scores.Any()) // Ensure competitors have scores
                .Select(c => new
                {
                    CompetitorId = c.Id,
                    MinRaceDate = c.Scores.Min(s => s.Race.Date),
                    MaxRaceDate = c.Scores.Max(s => s.Race.Date)
                })
                .ToListAsync();
        // Convert to dictionary
        var result = dates.ToDictionary(
            d => d.CompetitorId,
            d => new Tuple<DateTime?, DateTime?>(d.MinRaceDate, d.MaxRaceDate));
        return result;
    }

    public async Task<IList<Database.Entities.CompetitorActiveDates>> GetCompetitorActiveDates(Guid clubId)
    {
        // get the min and max race dates for each competitor
        return await _dbContext.GetCompetitorActiveDates(clubId);
    }

    public Task<Dictionary<Guid, DateTime?>> GetLastActiveDates(Guid clubId)
    {
        var returnValue = _dbContext.Competitors
            .Include(c => c.Scores)
            .ThenInclude(s => s.Race)
            .Where(c => c.ClubId == clubId)
            .ToDictionaryAsync(
                c => c.Id,
                c => c.Scores.Max(s => s.Race.Date));
        return returnValue;
    }

    public async Task<Dictionary<String, IEnumerable<Competitor>>> GetCompetitorsForFleetAsync(Guid clubId, Guid fleetId)
    {
        var fleetName = await _dbContext.Fleets
            .Where(f => f.Id == fleetId)
            .Select(f => f.Name)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);
        var comps = await GetCompetitorsAsync(clubId, fleetId, true);

        return new Dictionary<string, IEnumerable<Competitor>>
        {
            { fleetName, comps }
        };
    }

    public async Task<Dictionary<String, IEnumerable<Competitor>>> GetCompetitorsForRegattaAsync(Guid clubId, Guid regattaId)
    {
        var dbComps = await _dbContext.Regattas
        .Where(r => r.Id == regattaId)
        .SelectMany(r => r.RegattaFleet)
        .Include(rf => rf.Fleet)
        .ThenInclude(f => f.CompetitorFleets)
        .ThenInclude(cf => cf.Competitor)
        .ThenInclude(c => c.BoatClass)
        .Select(rf => rf.Fleet)
        .ToDictionaryAsync(
            f => f.NickName ?? f.ShortName ?? f.Name,
            f => f.CompetitorFleets?.Select(cf => cf.Competitor));

        var modelList = _mapper.Map<Dictionary<String, IEnumerable<Model.Competitor>>>(dbComps);
        foreach(var key in modelList.Keys)
        {
            var list = modelList[key].ToList();
            list.Sort();
            modelList[key] = list;
        }
        return modelList;
    }

    public async Task SetCompetitorActive(
        Guid clubId,
        Guid competitorId,
        bool active,
        string userName = "")
    {
        var comp = _dbContext.Competitors
            .Single(c => c.Id == competitorId && c.ClubId == clubId);
        if(comp.ChangeHistory == null)
        {
            comp.ChangeHistory = new List<Db.CompetitorChange>();
        }

        if((comp.IsActive ?? true) && !active)
        {
            comp.ChangeHistory.Add(new Db.CompetitorChange
            {
                ChangeTimeStamp = DateTime.UtcNow,
                ChangeTypeId = Db.ChangeType.DeactivatedId,
                ChangedBy = userName,
                NewValue = String.Empty,
                Summary = "Competitor Deactivated"
            });
        } else if((comp.IsActive == false) && active)
        {
            comp.ChangeHistory.Add(new Db.CompetitorChange
            {
                ChangeTimeStamp = DateTime.UtcNow,
                ChangeTypeId = Db.ChangeType.ActivatedId,
                ChangedBy = userName,
                NewValue = String.Empty,
                Summary = "Competitor Activated"
            });
        }
        comp.IsActive = active;
        await _dbContext.SaveChangesAsync();
    }

    public async Task<List<HistoricalNote>> GetCompetitorParticipationAsync(Guid competitorId)
    {
        var groupedData = await _dbContext.Scores
            .Where(s => s.CompetitorId == competitorId && s.Race.Date.HasValue)
            .Select(s => new { s.Race.Date })
            .Where(x => x.Date.HasValue)
            .GroupBy(x => new DateTime(x.Date.Value.Year, x.Date.Value.Month, 1))
            .Select(g => new
            {
                Date = g.Key,
                Count = g.Count()
            })
            .ToListAsync();

        var participation = groupedData
            .Select(g => new HistoricalNote
            {
                Date = g.Date,
                Summary = $"Participated in {g.Count} races",
                Aggregation = HistoricalNoteAggregation.Month
            })
            .OrderByDescending(x => x.Date)
            .ToList();

        return participation;
    }

}
