using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SailScores.Core.Model;
using SailScores.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Db = SailScores.Database.Entities;

namespace SailScores.Core.Services;

public class BackupService : IBackupService
{
    private readonly ISailScoresContext _dbContext;
    private readonly IMapper _mapper;

    // Used for GUID remapping during restore
    private Dictionary<Guid, Guid> _guidMap;

    public BackupService(
        ISailScoresContext dbContext,
        IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<ClubBackupData> CreateBackupAsync(Guid clubId, string createdBy)
    {
        var club = await _dbContext.Clubs
            .Where(c => c.Id == clubId)
            .AsNoTracking()
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);

        if (club == null)
        {
            throw new InvalidOperationException($"Club with id {clubId} not found.");
        }

        var backup = new ClubBackupData
        {
            Metadata = new ClubBackupMetadata
            {
                Version = ClubBackupMetadata.CurrentVersion,
                CreatedDateUtc = DateTime.UtcNow,
                SourceClubId = club.Id,
                SourceClubInitials = club.Initials,
                SourceClubName = club.Name,
                CreatedBy = createdBy
            },
            Name = club.Name,
            Initials = club.Initials,
            Description = club.Description,
            HomePageDescription = club.HomePageDescription,
            IsHidden = club.IsHidden,
            ShowClubInResults = club.ShowClubInResults,
            ShowCalendarInNav = club.ShowCalendarInNav,
            Url = club.Url,
            Locale = club.Locale,
            DefaultRaceDateOffset = club.DefaultRaceDateOffset,
            StatisticsDescription = club.StatisticsDescription
        };

        // Weather settings
        if (club.WeatherSettings != null)
        {
            backup.WeatherSettings = _mapper.Map<WeatherSettings>(club.WeatherSettings);
        }

        // Boat Classes
        var boatClasses = await _dbContext.BoatClasses
            .Where(bc => bc.ClubId == clubId)
            .AsNoTracking()
            .ToListAsync()
            .ConfigureAwait(false);
        backup.BoatClasses = _mapper.Map<IList<BoatClass>>(boatClasses);

        // Seasons
        var seasons = await _dbContext.Seasons
            .Where(s => s.ClubId == clubId)
            .AsNoTracking()
            .ToListAsync()
            .ConfigureAwait(false);
        backup.Seasons = _mapper.Map<IList<Season>>(seasons);

        // Scoring Systems (only those owned by this club)
        var scoringSystems = await _dbContext.ScoringSystems
            .Where(ss => ss.ClubId == clubId)
            .Include(ss => ss.ScoreCodes)
            .AsNoTracking()
            .ToListAsync()
            .ConfigureAwait(false);
        backup.ScoringSystems = _mapper.Map<IList<ScoringSystem>>(scoringSystems);

        // Get default scoring system name for reference
        if (club.DefaultScoringSystemId.HasValue)
        {
            var defaultSystem = await _dbContext.ScoringSystems
                .Where(ss => ss.Id == club.DefaultScoringSystemId.Value)
                .AsNoTracking()
                .FirstOrDefaultAsync()
                .ConfigureAwait(false);
            backup.DefaultScoringSystemName = defaultSystem?.Name;
        }

        // Fleets with boat class and competitor associations
        var fleets = await _dbContext.Fleets
            .Where(f => f.ClubId == clubId)
            .Include(f => f.FleetBoatClasses)
            .Include(f => f.CompetitorFleets)
            .AsNoTracking()
            .AsSplitQuery()
            .ToListAsync()
            .ConfigureAwait(false);
        backup.Fleets = MapFleets(fleets, boatClasses);

        // Competitors with fleet associations
        var competitors = await _dbContext.Competitors
            .Where(c => c.ClubId == clubId)
            .Include(c => c.CompetitorFleets)
            .AsNoTracking()
            .ToListAsync()
            .ConfigureAwait(false);
        backup.Competitors = MapCompetitors(competitors, boatClasses, fleets);

        // Series with race associations
        var series = await _dbContext.Series
            .Where(s => s.ClubId == clubId)
            .Include(s => s.RaceSeries)
            .Include(s => s.Season)
            .Include(s => s.ChildLinks)
            .Include(s => s.ParentLinks)
            .AsNoTracking()
            .AsSplitQuery()
            .ToListAsync()
            .ConfigureAwait(false);
        backup.Series = MapSeries(series, seasons, scoringSystems);

        // Races with scores, weather, and series associations
        var races = await _dbContext.Races
            .Where(r => r.ClubId == clubId)
            .Include(r => r.Fleet)
            .Include(r => r.Scores)
            .Include(r => r.Weather)
            .Include(r => r.SeriesRaces)
            .AsNoTracking()
            .AsSplitQuery()
            .ToListAsync()
            .ConfigureAwait(false);
        backup.Races = MapRaces(races, fleets, competitors, series, seasons);

        // Regattas
        var regattas = await _dbContext.Regattas
            .Where(r => r.ClubId == clubId)
            .Include(r => r.RegattaSeries)
            .Include(r => r.RegattaFleet)
            .Include(r => r.Season)
            .AsNoTracking()
            .AsSplitQuery()
            .ToListAsync()
            .ConfigureAwait(false);
        backup.Regattas = MapRegattas(regattas, series, fleets, seasons, scoringSystems);

        // Announcements (include CreatedBy/UpdatedBy as last modified user names)
        var announcements = await _dbContext.Announcements
            .Where(a => a.ClubId == clubId && !a.IsDeleted)
            .AsNoTracking()
            .ToListAsync()
            .ConfigureAwait(false);
        backup.Announcements = _mapper.Map<IList<Announcement>>(announcements);

        // Documents (include CreatedBy as last modified user name)
        var documents = await _dbContext.Documents
            .Where(d => d.ClubId == clubId)
            .AsNoTracking()
            .ToListAsync()
            .ConfigureAwait(false);
        backup.Documents = _mapper.Map<IList<Document>>(documents);

        return backup;
    }

    private IList<Fleet> MapFleets(List<Db.Fleet> dbFleets, List<Db.BoatClass> allBoatClasses)
    {
        var fleets = new List<Fleet>();
        var boatClassDict = allBoatClasses.ToDictionary(bc => bc.Id);

        foreach (var dbFleet in dbFleets)
        {
            var fleet = _mapper.Map<Fleet>(dbFleet);
            fleet.BoatClasses = new List<BoatClass>();

            if (dbFleet.FleetBoatClasses != null)
            {
                foreach (var fbc in dbFleet.FleetBoatClasses)
                {
                    if (boatClassDict.TryGetValue(fbc.BoatClassId, out var bc))
                    {
                        fleet.BoatClasses.Add(_mapper.Map<BoatClass>(bc));
                    }
                }
            }

            fleets.Add(fleet);
        }

        return fleets;
    }

    private IList<Competitor> MapCompetitors(
        List<Db.Competitor> dbCompetitors,
        List<Db.BoatClass> allBoatClasses,
        List<Db.Fleet> allFleets)
    {
        var competitors = new List<Competitor>();
        var boatClassDict = allBoatClasses.ToDictionary(bc => bc.Id);
        var fleetDict = allFleets.ToDictionary(f => f.Id);

        foreach (var dbComp in dbCompetitors)
        {
            var comp = _mapper.Map<Competitor>(dbComp);

            if (boatClassDict.TryGetValue(dbComp.BoatClassId, out var bc))
            {
                comp.BoatClass = _mapper.Map<BoatClass>(bc);
            }

            comp.Fleets = new List<Fleet>();
            if (dbComp.CompetitorFleets != null)
            {
                foreach (var cf in dbComp.CompetitorFleets)
                {
                    if (fleetDict.TryGetValue(cf.FleetId, out var fleet))
                    {
                        comp.Fleets.Add(new Fleet { Id = fleet.Id, Name = fleet.Name, ShortName = fleet.ShortName });
                    }
                }
            }

            competitors.Add(comp);
        }

        return competitors;
    }

    private IList<Series> MapSeries(
        List<Db.Series> dbSeries,
        List<Db.Season> allSeasons,
        List<Db.ScoringSystem> allScoringSystems)
    {
        var seriesList = new List<Series>();
        var seasonDict = allSeasons.ToDictionary(s => s.Id);
        var scoringDict = allScoringSystems.ToDictionary(ss => ss.Id);

        foreach (var dbS in dbSeries)
        {
            var s = _mapper.Map<Series>(dbS);

            if (dbS.Season != null)
            {
                s.Season = _mapper.Map<Season>(dbS.Season);
            }

            if (dbS.ScoringSystemId.HasValue && scoringDict.TryGetValue(dbS.ScoringSystemId.Value, out var ss))
            {
                s.ScoringSystem = new ScoringSystem { Id = ss.Id, Name = ss.Name };
            }

            // Store child/parent series links as Guids
            s.ChildrenSeriesIds = dbS.ChildLinks?.Select(cl => cl.ChildSeriesId).ToList() ?? new List<Guid>();
            s.ParentSeriesIds = dbS.ParentLinks?.Select(pl => pl.ParentSeriesId).ToList() ?? new List<Guid>();

            seriesList.Add(s);
        }

        return seriesList;
    }

    private IList<Race> MapRaces(
        List<Db.Race> dbRaces,
        List<Db.Fleet> allFleets,
        List<Db.Competitor> allCompetitors,
        List<Db.Series> allSeries,
        List<Db.Season> allSeasons)
    {
        var races = new List<Race>();
        var fleetDict = allFleets.ToDictionary(f => f.Id);
        var compDict = allCompetitors.ToDictionary(c => c.Id);
        var seriesDict = allSeries.ToDictionary(s => s.Id);

        foreach (var dbRace in dbRaces)
        {
            var race = _mapper.Map<Race>(dbRace);

            if (dbRace.Fleet != null)
            {
                race.Fleet = new Fleet { Id = dbRace.Fleet.Id, Name = dbRace.Fleet.Name, ShortName = dbRace.Fleet.ShortName };
            }

            if (dbRace.Weather != null)
            {
                race.Weather = _mapper.Map<Weather>(dbRace.Weather);
            }

            // Map scores
            race.Scores = new List<Score>();
            if (dbRace.Scores != null)
            {
                foreach (var dbScore in dbRace.Scores)
                {
                    var score = new Score
                    {
                        Id = dbScore.Id,
                        CompetitorId = dbScore.CompetitorId,
                        RaceId = dbScore.RaceId,
                        Place = dbScore.Place,
                        Code = dbScore.Code,
                        CodePoints = dbScore.CodePoints,
                        FinishTime = dbScore.FinishTime,
                        ElapsedTime = dbScore.ElapsedTime
                    };

                    if (compDict.TryGetValue(dbScore.CompetitorId, out var comp))
                    {
                        score.Competitor = new Competitor { Id = comp.Id, Name = comp.Name, SailNumber = comp.SailNumber };
                    }

                    race.Scores.Add(score);
                }
            }

            // Map series associations
            race.Series = new List<Series>();
            if (dbRace.SeriesRaces != null)
            {
                foreach (var sr in dbRace.SeriesRaces)
                {
                    if (seriesDict.TryGetValue(sr.SeriesId, out var series))
                    {
                        race.Series.Add(new Series { Id = series.Id, Name = series.Name });
                    }
                }
            }

            races.Add(race);
        }

        return races;
    }

    private IList<Regatta> MapRegattas(
        List<Db.Regatta> dbRegattas,
        List<Db.Series> allSeries,
        List<Db.Fleet> allFleets,
        List<Db.Season> allSeasons,
        List<Db.ScoringSystem> allScoringSystems)
    {
        var regattas = new List<Regatta>();
        var seriesDict = allSeries.ToDictionary(s => s.Id);
        var fleetDict = allFleets.ToDictionary(f => f.Id);
        var scoringDict = allScoringSystems.ToDictionary(ss => ss.Id);

        foreach (var dbRegatta in dbRegattas)
        {
            var regatta = _mapper.Map<Regatta>(dbRegatta);

            if (dbRegatta.Season != null)
            {
                regatta.Season = _mapper.Map<Season>(dbRegatta.Season);
            }

            if (dbRegatta.ScoringSystemId.HasValue && scoringDict.TryGetValue(dbRegatta.ScoringSystemId.Value, out var ss))
            {
                regatta.ScoringSystem = new ScoringSystem { Id = ss.Id, Name = ss.Name };
            }

            // Map series
            regatta.Series = new List<Series>();
            if (dbRegatta.RegattaSeries != null)
            {
                foreach (var rs in dbRegatta.RegattaSeries)
                {
                    if (seriesDict.TryGetValue(rs.SeriesId, out var series))
                    {
                        regatta.Series.Add(new Series { Id = series.Id, Name = series.Name });
                    }
                }
            }

            // Map fleets
            regatta.Fleets = new List<Fleet>();
            if (dbRegatta.RegattaFleet != null)
            {
                foreach (var rf in dbRegatta.RegattaFleet)
                {
                    if (fleetDict.TryGetValue(rf.FleetId, out var fleet))
                    {
                        regatta.Fleets.Add(new Fleet { Id = fleet.Id, Name = fleet.Name, ShortName = fleet.ShortName });
                    }
                }
            }

            regattas.Add(regatta);
        }

        return regattas;
    }

    public BackupValidationResult ValidateBackup(ClubBackupData backup)
    {
        if (backup == null)
        {
            return new BackupValidationResult
            {
                IsValid = false,
                ErrorMessage = "Backup data is null."
            };
        }

        if (backup.Metadata == null)
        {
            return new BackupValidationResult
            {
                IsValid = false,
                ErrorMessage = "Backup metadata is missing."
            };
        }

        if (backup.Metadata.Version > ClubBackupMetadata.CurrentVersion)
        {
            return new BackupValidationResult
            {
                IsValid = false,
                ErrorMessage = $"Backup version {backup.Metadata.Version} is newer than supported version {ClubBackupMetadata.CurrentVersion}.",
                Version = backup.Metadata.Version,
                SourceClubName = backup.Metadata.SourceClubName,
                CreatedDateUtc = backup.Metadata.CreatedDateUtc
            };
        }

        return new BackupValidationResult
        {
            IsValid = true,
            Version = backup.Metadata.Version,
            SourceClubName = backup.Metadata.SourceClubName,
            CreatedDateUtc = backup.Metadata.CreatedDateUtc
        };
    }

    public async Task<bool> RestoreBackupAsync(Guid targetClubId, ClubBackupData backup, bool preserveClubSettings = true)
    {
        var validation = ValidateBackup(backup);
        if (!validation.IsValid)
        {
            throw new InvalidOperationException($"Invalid backup: {validation.ErrorMessage}");
        }

        _guidMap = new Dictionary<Guid, Guid>();

        // Get target club
        var club = await _dbContext.Clubs
            .Include(c => c.WeatherSettings)
            .FirstOrDefaultAsync(c => c.Id == targetClubId)
            .ConfigureAwait(false);

        if (club == null)
        {
            throw new InvalidOperationException($"Target club {targetClubId} not found.");
        }

        // Delete existing data in reverse dependency order
        await DeleteExistingClubDataAsync(targetClubId).ConfigureAwait(false);

        // Update club settings if not preserving
        if (!preserveClubSettings)
        {
            club.Name = backup.Name;
            club.Description = backup.Description;
            club.HomePageDescription = backup.HomePageDescription;
            club.Url = backup.Url;
        }

        // Always restore these settings
        club.ShowClubInResults = backup.ShowClubInResults;
        club.ShowCalendarInNav = backup.ShowCalendarInNav;
        club.Locale = backup.Locale;
        club.DefaultRaceDateOffset = backup.DefaultRaceDateOffset;
        club.StatisticsDescription = backup.StatisticsDescription;

        if (backup.WeatherSettings != null)
        {
            club.WeatherSettings ??= new Db.WeatherSettings();
            club.WeatherSettings.Latitude = backup.WeatherSettings.Latitude;
            club.WeatherSettings.Longitude = backup.WeatherSettings.Longitude;
            club.WeatherSettings.TemperatureUnits = backup.WeatherSettings.TemperatureUnits;
            club.WeatherSettings.WindSpeedUnits = backup.WeatherSettings.WindSpeedUnits;
        }

        // Restore data in dependency order

        // 1. Boat Classes
        foreach (var bc in backup.BoatClasses ?? Enumerable.Empty<BoatClass>())
        {
            var dbBc = new Db.BoatClass
            {
                Id = GetNewGuid(bc.Id),
                ClubId = targetClubId,
                Name = bc.Name,
                Description = bc.Description
            };
            _dbContext.BoatClasses.Add(dbBc);
        }

        // 2. Seasons
        foreach (var season in backup.Seasons ?? Enumerable.Empty<Season>())
        {
            var dbSeason = new Db.Season
            {
                Id = GetNewGuid(season.Id),
                ClubId = targetClubId,
                Name = season.Name,
                UrlName = season.UrlName,
                Start = season.Start,
                End = season.End
            };
            _dbContext.Seasons.Add(dbSeason);
        }

        // 3. Scoring Systems
        Guid? defaultScoringSystemId = null;
        foreach (var ss in backup.ScoringSystems ?? Enumerable.Empty<ScoringSystem>())
        {
            var dbSs = new Db.ScoringSystem
            {
                Id = GetNewGuid(ss.Id),
                ClubId = targetClubId,
                Name = ss.Name,
                DiscardPattern = ss.DiscardPattern,
                ParticipationPercent = ss.ParticipationPercent

            };

            // Track default scoring system
            if (ss.Name == backup.DefaultScoringSystemName)
            {
                defaultScoringSystemId = dbSs.Id;
            }

            _dbContext.ScoringSystems.Add(dbSs);

            // Add score codes
            foreach (var sc in ss.ScoreCodes ?? Enumerable.Empty<ScoreCode>())
            {
                var dbSc = new Db.ScoreCode
                {
                    Id = GetNewGuid(sc.Id),
                    ScoringSystemId = dbSs.Id,
                    Name = sc.Name,
                    Description = sc.Description,
                    Formula = sc.Formula,
                    FormulaValue = sc.FormulaValue,
                    ScoreLike = sc.ScoreLike,
                    Discardable = sc.Discardable,
                    CameToStart = sc.CameToStart,
                    Started = sc.Started,
                    Finished = sc.Finished,
                    PreserveResult = sc.PreserveResult,
                    AdjustOtherScores = sc.AdjustOtherScores,
                    CountAsParticipation = sc.CountAsParticipation
                };
                _dbContext.ScoreCodes.Add(dbSc);
            }
        }


        // Update parent system references (second pass)
        await _dbContext.SaveChangesAsync().ConfigureAwait(false);
        foreach (var ss in backup.ScoringSystems ?? Enumerable.Empty<ScoringSystem>())
        {
            if (ss.ParentSystemId.HasValue)
            {
                var newId = GetNewGuidIfExists(ss.Id);
                var newParentId = GetNewOrOldGuid(ss.ParentSystemId.Value);
                if (newId.HasValue && newParentId.HasValue)
                {
                    var dbSs = await _dbContext.ScoringSystems.FindAsync(newId.Value).ConfigureAwait(false);
                    if (dbSs != null)
                    {
                        dbSs.ParentSystemId = newParentId.Value;
                    }
                }
            }
        }

        // Set default scoring system
        if (defaultScoringSystemId.HasValue)
        {
            club.DefaultScoringSystemId = defaultScoringSystemId;
        }

        // 4. Fleets (Fleet model doesn't have IsHidden, so we default to false)
        foreach (var fleet in backup.Fleets ?? Enumerable.Empty<Fleet>())
        {
            var dbFleet = new Db.Fleet
            {
                Id = GetNewGuid(fleet.Id),
                ClubId = targetClubId,
                Name = fleet.Name,
                ShortName = fleet.ShortName,
                NickName = fleet.NickName,
                Description = fleet.Description,
                IsHidden = false,
                IsActive = fleet.IsActive,
                FleetType = fleet.FleetType
            };
            _dbContext.Fleets.Add(dbFleet);

            // Fleet boat class associations
            foreach (var bc in fleet.BoatClasses ?? Enumerable.Empty<BoatClass>())
            {
                var newBcId = GetNewGuidIfExists(bc.Id);
                if (newBcId.HasValue)
                {
                    var fbc = new Db.FleetBoatClass
                    {
                        FleetId = dbFleet.Id,
                        BoatClassId = newBcId.Value
                    };
                    _dbContext.FleetBoatClasses.Add(fbc);
                }
            }
        }

        // 5. Competitors
        foreach (var comp in backup.Competitors ?? Enumerable.Empty<Competitor>())
        {
            var newBoatClassId = GetNewGuidIfExists(comp.BoatClassId);
            var dbComp = new Db.Competitor
            {
                Id = GetNewGuid(comp.Id),
                ClubId = targetClubId,
                Name = comp.Name,
                SailNumber = comp.SailNumber,
                AlternativeSailNumber = comp.AlternativeSailNumber,
                BoatName = comp.BoatName,
                HomeClubName = comp.HomeClubName,
                Notes = comp.Notes,
                IsActive = comp.IsActive,
                BoatClassId = newBoatClassId ?? Guid.Empty,
                UrlName = comp.UrlName,
                UrlId = comp.UrlId,
                Created = comp.Created
            };
            _dbContext.Competitors.Add(dbComp);

            // Competitor fleet associations
            foreach (var fleet in comp.Fleets ?? Enumerable.Empty<Fleet>())
            {
                var newFleetId = GetNewGuidIfExists(fleet.Id);
                if (newFleetId.HasValue)
                {
                    var cf = new Db.CompetitorFleet
                    {
                        CompetitorId = dbComp.Id,
                        FleetId = newFleetId.Value
                    };
                    _dbContext.CompetitorFleets.Add(cf);
                }
            }
        }

        // 6. Series
        foreach (var series in backup.Series ?? Enumerable.Empty<Series>())
        {
            var newSeasonId = series.Season != null ? GetNewGuidIfExists(series.Season.Id) : null;
            var newScoringId = series.ScoringSystemId.HasValue ? GetNewGuidIfExists(series.ScoringSystemId.Value) : null;

            var dbSeries = new Db.Series
            {
                Id = GetNewGuid(series.Id),
                ClubId = targetClubId,
                Name = series.Name,
                UrlName = series.UrlName,
                Description = series.Description,
                Type = (Db.SeriesType?)(int?)series.Type,
                IsImportantSeries = series.IsImportantSeries,
                ResultsLocked = series.ResultsLocked,
                UpdatedDate = series.UpdatedDate,
                UpdatedBy = series.UpdatedBy,
                ScoringSystemId = newScoringId,
                TrendOption = series.TrendOption,
                FleetId = series.FleetId.HasValue ? GetNewGuidIfExists(series.FleetId.Value) : null,
                PreferAlternativeSailNumbers = series.PreferAlternativeSailNumbers,
                ExcludeFromCompetitorStats = series.ExcludeFromCompetitorStats,
                HideDncDiscards = series.HideDncDiscards,
                ChildrenSeriesAsSingleRace = series.ChildrenSeriesAsSingleRace,
                RaceCount = series.RaceCount,
                DateRestricted = series.DateRestricted,
                EnforcedStartDate = series.EnforcedStartDate,
                EnforcedEndDate = series.EnforcedEndDate,
                StartDate = series.StartDate,
                EndDate = series.EndDate
            };

            // Set season - required field
            if (newSeasonId.HasValue)
            {
                var season = await _dbContext.Seasons.FindAsync(newSeasonId.Value).ConfigureAwait(false);
                dbSeries.Season = season;
            }

            _dbContext.Series.Add(dbSeries);
        }

        await _dbContext.SaveChangesAsync().ConfigureAwait(false);

        // Add series-to-series links (second pass after all series exist)
        foreach (var series in backup.Series ?? Enumerable.Empty<Series>())
        {
            var newSeriesId = GetNewGuidIfExists(series.Id);
            if (!newSeriesId.HasValue) continue;

            foreach (var childId in series.ChildrenSeriesIds ?? Enumerable.Empty<Guid>())
            {
                var newChildId = GetNewGuidIfExists(childId);
                if (newChildId.HasValue)
                {
                    var link = new Db.SeriesToSeriesLink
                    {
                        ParentSeriesId = newSeriesId.Value,
                        ChildSeriesId = newChildId.Value
                    };
                    _dbContext.SeriesToSeriesLinks.Add(link);
                }
            }
        }

        // 7. Races with Scores
        foreach (var race in backup.Races ?? Enumerable.Empty<Race>())
        {
            var newFleetId = race.Fleet != null ? GetNewGuidIfExists(race.Fleet.Id) : null;

            var dbRace = new Db.Race
            {
                Id = GetNewGuid(race.Id),
                ClubId = targetClubId,
                Name = race.Name,
                Date = race.Date,
                State = race.State,
                Order = race.Order,
                Description = race.Description,
                TrackingUrl = race.TrackingUrl,
                UpdatedDate = race.UpdatedDate,
                UpdatedBy = race.UpdatedBy,
                StartTime = race.StartTime,
                TrackTimes = race.TrackTimes
            };

            // Set fleet
            if (newFleetId.HasValue)
            {
                var fleet = await _dbContext.Fleets.FindAsync(newFleetId.Value).ConfigureAwait(false);
                dbRace.Fleet = fleet;
            }

            // Weather
            if (race.Weather != null)
            {
                dbRace.Weather = new Db.Weather
                {
                    Id = Guid.NewGuid(),
                    Description = race.Weather.Description,
                    Icon = race.Weather.Icon,
                    TemperatureString = race.Weather.TemperatureString,
                    TemperatureDegreesKelvin = race.Weather.TemperatureDegreesKelvin,
                    WindSpeedString = race.Weather.WindSpeedString,
                    WindSpeedMeterPerSecond = race.Weather.WindSpeedMeterPerSecond,
                    WindDirectionString = race.Weather.WindDirectionString,
                    WindDirectionDegrees = race.Weather.WindDirectionDegrees,
                    WindGustString = race.Weather.WindGustString,
                    WindGustMeterPerSecond = race.Weather.WindGustMeterPerSecond,
                    Humidity = race.Weather.Humidity,
                    CloudCoverPercent = race.Weather.CloudCoverPercent,
                    CreatedDate = race.Weather.CreatedDate
                };
            }

            _dbContext.Races.Add(dbRace);

            // Scores
            foreach (var score in race.Scores ?? Enumerable.Empty<Score>())
            {
                var newCompId = GetNewGuidIfExists(score.CompetitorId);
                if (!newCompId.HasValue) continue;

                var dbScore = new Db.Score
                {
                    Id = GetNewGuid(score.Id),
                    CompetitorId = newCompId.Value,
                    RaceId = dbRace.Id,
                    Place = score.Place,
                    Code = score.Code,
                    CodePoints = score.CodePoints,
                    FinishTime = score.FinishTime,
                    ElapsedTime = score.ElapsedTime
                };
                _dbContext.Scores.Add(dbScore);
            }

            // Series-Race associations
            foreach (var series in race.Series ?? Enumerable.Empty<Series>())
            {
                var newSeriesId = GetNewGuidIfExists(series.Id);
                if (newSeriesId.HasValue)
                {
                    var sr = new Db.SeriesRace
                    {
                        RaceId = dbRace.Id,
                        SeriesId = newSeriesId.Value
                    };
                    _dbContext.SeriesRaces.Add(sr);
                }
            }
        }

        // 8. Regattas
        foreach (var regatta in backup.Regattas ?? Enumerable.Empty<Regatta>())
        {
            var newSeasonId = regatta.Season != null ? GetNewGuidIfExists(regatta.Season.Id) : null;
            var newScoringId = regatta.ScoringSystemId.HasValue ? GetNewGuidIfExists(regatta.ScoringSystemId.Value) : null;

            var dbRegatta = new Db.Regatta
            {
                Id = GetNewGuid(regatta.Id),
                ClubId = targetClubId,
                Name = regatta.Name,
                UrlName = regatta.UrlName,
                Description = regatta.Description,
                Url = regatta.Url,
                StartDate = regatta.StartDate,
                EndDate = regatta.EndDate,
                UpdatedDate = regatta.UpdatedDate,
                ScoringSystemId = newScoringId,
                PreferAlternateSailNumbers = regatta.PreferAlternateSailNumbers,
                HideFromFrontPage = regatta.HideFromFrontPage
            };

            // Set season
            if (newSeasonId.HasValue)
            {
                var season = await _dbContext.Seasons.FindAsync(newSeasonId.Value).ConfigureAwait(false);
                dbRegatta.Season = season;
            }

            _dbContext.Regattas.Add(dbRegatta);

            // Regatta-Series associations
            foreach (var series in regatta.Series ?? Enumerable.Empty<Series>())
            {
                var newSeriesId = GetNewGuidIfExists(series.Id);
                if (newSeriesId.HasValue)
                {
                    var rs = new Db.RegattaSeries
                    {
                        RegattaId = dbRegatta.Id,
                        SeriesId = newSeriesId.Value
                    };
                    _dbContext.RegattaSeries.Add(rs);
                }
            }

            // Regatta-Fleet associations
            foreach (var fleet in regatta.Fleets ?? Enumerable.Empty<Fleet>())
            {
                var newFleetId = GetNewGuidIfExists(fleet.Id);
                if (newFleetId.HasValue)
                {
                    var rf = new Db.RegattaFleet
                    {
                        RegattaId = dbRegatta.Id,
                        FleetId = newFleetId.Value
                    };
                    _dbContext.RegattaFleets.Add(rf);
                }
            }
        }

        // 9. Announcements
        foreach (var ann in backup.Announcements ?? Enumerable.Empty<Announcement>())
        {
            var newRegattaId = ann.RegattaId.HasValue ? GetNewGuidIfExists(ann.RegattaId.Value) : null;

            var dbAnn = new Db.Announcement
            {
                Id = GetNewGuid(ann.Id),
                ClubId = targetClubId,
                RegattaId = newRegattaId,
                Content = ann.Content,
                CreatedDate = ann.CreatedDate,
                CreatedLocalDate = ann.CreatedLocalDate,
                CreatedBy = ann.CreatedBy,
                UpdatedDate = ann.UpdatedDate,
                UpdatedLocalDate = ann.UpdatedLocalDate,
                UpdatedBy = ann.UpdatedBy,
                ArchiveAfter = ann.ArchiveAfter,
                PreviousVersion = null, // Don't preserve version chain
                IsDeleted = false
            };
            _dbContext.Announcements.Add(dbAnn);
        }

        // 10. Documents
        foreach (var doc in backup.Documents ?? Enumerable.Empty<Document>())
        {
            var newRegattaId = doc.RegattaId.HasValue ? GetNewGuidIfExists(doc.RegattaId.Value) : null;

            var dbDoc = new Db.Document
            {
                Id = GetNewGuid(doc.Id),
                ClubId = targetClubId,
                RegattaId = newRegattaId,
                Name = doc.Name,
                ContentType = doc.ContentType,
                FileContents = doc.FileContents,
                CreatedDate = doc.CreatedDate,
                CreatedLocalDate = doc.CreatedLocalDate,
                CreatedBy = doc.CreatedBy,
                PreviousVersion = null // Don't preserve version chain
            };
            _dbContext.Documents.Add(dbDoc);
        }

        await _dbContext.SaveChangesAsync().ConfigureAwait(false);

        return true;
    }

    private async Task DeleteExistingClubDataAsync(Guid clubId)
    {
        // Delete in reverse dependency order

        // Documents
        var documents = await _dbContext.Documents.Where(d => d.ClubId == clubId).ToListAsync().ConfigureAwait(false);
        _dbContext.Documents.RemoveRange(documents);

        // Announcements
        var announcements = await _dbContext.Announcements.Where(a => a.ClubId == clubId).ToListAsync().ConfigureAwait(false);
        _dbContext.Announcements.RemoveRange(announcements);

        // Scores (via races)
        var raceIds = await _dbContext.Races.Where(r => r.ClubId == clubId).Select(r => r.Id).ToListAsync().ConfigureAwait(false);
        var scores = await _dbContext.Scores.Where(s => raceIds.Contains(s.RaceId)).ToListAsync().ConfigureAwait(false);
        _dbContext.Scores.RemoveRange(scores);

        // SeriesRace links
        var seriesIds = await _dbContext.Series.Where(s => s.ClubId == clubId).Select(s => s.Id).ToListAsync().ConfigureAwait(false);
        var seriesRaces = await _dbContext.SeriesRaces.Where(sr => seriesIds.Contains(sr.SeriesId)).ToListAsync().ConfigureAwait(false);
        _dbContext.SeriesRaces.RemoveRange(seriesRaces);

        // Races
        var races = await _dbContext.Races.Where(r => r.ClubId == clubId).Include(r => r.Weather).ToListAsync().ConfigureAwait(false);
        _dbContext.Races.RemoveRange(races);

        // RegattaSeries and RegattaFleet links
        var regattaIds = await _dbContext.Regattas.Where(r => r.ClubId == clubId).Select(r => r.Id).ToListAsync().ConfigureAwait(false);
        var regattaSeries = await _dbContext.RegattaSeries.Where(rs => regattaIds.Contains(rs.RegattaId)).ToListAsync().ConfigureAwait(false);
        _dbContext.RegattaSeries.RemoveRange(regattaSeries);
        var regattaFleets = await _dbContext.RegattaFleets.Where(rf => regattaIds.Contains(rf.RegattaId)).ToListAsync().ConfigureAwait(false);
        _dbContext.RegattaFleets.RemoveRange(regattaFleets);

        // Regattas
        var regattas = await _dbContext.Regattas.Where(r => r.ClubId == clubId).ToListAsync().ConfigureAwait(false);
        _dbContext.Regattas.RemoveRange(regattas);

        // SeriesToSeriesLinks
        var seriesToSeriesLinks = await _dbContext.SeriesToSeriesLinks
            .Where(l => seriesIds.Contains(l.ParentSeriesId) || seriesIds.Contains(l.ChildSeriesId))
            .ToListAsync().ConfigureAwait(false);
        _dbContext.SeriesToSeriesLinks.RemoveRange(seriesToSeriesLinks);

        // Series
        var series = await _dbContext.Series.Where(s => s.ClubId == clubId).ToListAsync().ConfigureAwait(false);
        _dbContext.Series.RemoveRange(series);

        // CompetitorFleet links
        var competitorIds = await _dbContext.Competitors.Where(c => c.ClubId == clubId).Select(c => c.Id).ToListAsync().ConfigureAwait(false);
        var competitorFleets = await _dbContext.CompetitorFleets.Where(cf => competitorIds.Contains(cf.CompetitorId)).ToListAsync().ConfigureAwait(false);
        _dbContext.CompetitorFleets.RemoveRange(competitorFleets);

        // Competitors
        var competitors = await _dbContext.Competitors.Where(c => c.ClubId == clubId).ToListAsync().ConfigureAwait(false);
        _dbContext.Competitors.RemoveRange(competitors);

        // FleetBoatClass links
        var fleetIds = await _dbContext.Fleets.Where(f => f.ClubId == clubId).Select(f => f.Id).ToListAsync().ConfigureAwait(false);
        var fleetBoatClasses = await _dbContext.FleetBoatClasses.Where(fbc => fleetIds.Contains(fbc.FleetId)).ToListAsync().ConfigureAwait(false);
        _dbContext.FleetBoatClasses.RemoveRange(fleetBoatClasses);

        // Fleets
        var fleets = await _dbContext.Fleets.Where(f => f.ClubId == clubId).ToListAsync().ConfigureAwait(false);
        _dbContext.Fleets.RemoveRange(fleets);

        // ScoreCodes (via scoring systems)
        var scoringSystemIds = await _dbContext.ScoringSystems.Where(ss => ss.ClubId == clubId).Select(ss => ss.Id).ToListAsync().ConfigureAwait(false);
        var scoreCodes = await _dbContext.ScoreCodes.Where(sc => scoringSystemIds.Contains(sc.ScoringSystemId)).ToListAsync().ConfigureAwait(false);
        _dbContext.ScoreCodes.RemoveRange(scoreCodes);

        // ScoringSystems
        var scoringSystems = await _dbContext.ScoringSystems.Where(ss => ss.ClubId == clubId).ToListAsync().ConfigureAwait(false);
        _dbContext.ScoringSystems.RemoveRange(scoringSystems);

        // Seasons
        var seasons = await _dbContext.Seasons.Where(s => s.ClubId == clubId).ToListAsync().ConfigureAwait(false);
        _dbContext.Seasons.RemoveRange(seasons);

        // BoatClasses
        var boatClasses = await _dbContext.BoatClasses.Where(bc => bc.ClubId == clubId).ToListAsync().ConfigureAwait(false);
        _dbContext.BoatClasses.RemoveRange(boatClasses);

        await _dbContext.SaveChangesAsync().ConfigureAwait(false);
    }

    private Guid GetNewGuid(Guid oldGuid)
    {
        if (!_guidMap.ContainsKey(oldGuid))
        {
            _guidMap[oldGuid] = Guid.NewGuid();
        }
        return _guidMap[oldGuid];
    }

    private Guid? GetNewGuidIfExists(Guid oldGuid)
    {
        if (_guidMap.TryGetValue(oldGuid, out var newGuid))
        {
            return newGuid;
        }
        return null;
    }

    private Guid? GetNewOrOldGuid(Guid oldGuid)
    {
        if (_guidMap.TryGetValue(oldGuid, out var newGuid))
        {
            return newGuid;
        }
        return oldGuid;
    }
}
