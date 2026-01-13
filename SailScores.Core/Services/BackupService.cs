using Microsoft.EntityFrameworkCore;
using SailScores.Core.Model.BackupEntities;
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

    // Used for GUID remapping during restore
    private Dictionary<Guid, Guid> _guidMap;

    public BackupService(ISailScoresContext dbContext)
    {
        _dbContext = dbContext;
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
            StatisticsDescription = club.StatisticsDescription,
            LogoFileId = club.LogoFileId
        };

        // Weather settings
        if (club.WeatherSettings != null)
        {
            backup.WeatherSettings = new WeatherSettingsBackup
            {
                Latitude = club.WeatherSettings.Latitude,
                Longitude = club.WeatherSettings.Longitude,
                TemperatureUnits = club.WeatherSettings.TemperatureUnits,
                WindSpeedUnits = club.WeatherSettings.WindSpeedUnits
            };
        }

        // Boat Classes
        var boatClasses = await _dbContext.BoatClasses
            .Where(bc => bc.ClubId == clubId)
            .AsNoTracking()
            .ToListAsync()
            .ConfigureAwait(false);
        backup.BoatClasses = boatClasses.Select(bc => new BoatClassBackup
        {
            Id = bc.Id,
            Name = bc.Name,
            Description = bc.Description
        }).ToList();

        // Seasons
        var seasons = await _dbContext.Seasons
            .Where(s => s.ClubId == clubId)
            .AsNoTracking()
            .ToListAsync()
            .ConfigureAwait(false);
        backup.Seasons = seasons.Select(s => new SeasonBackup
        {
            Id = s.Id,
            Name = s.Name,
            UrlName = s.UrlName,
            Start = s.Start,
            End = s.End
        }).ToList();

        // Scoring Systems (only those owned by this club)
        var scoringSystems = await _dbContext.ScoringSystems
            .Where(ss => ss.ClubId == clubId)
            .Include(ss => ss.ScoreCodes)
            .AsNoTracking()
            .ToListAsync()
            .ConfigureAwait(false);
        backup.ScoringSystems = scoringSystems.Select(ss => new ScoringSystemBackup
        {
            Id = ss.Id,
            Name = ss.Name,
            DiscardPattern = ss.DiscardPattern,
            ParticipationPercent = ss.ParticipationPercent,
            ParentSystemId = ss.ParentSystemId,
            IsSiteDefault = ss.IsSiteDefault,
            ScoreCodes = ss.ScoreCodes?.Select(sc => new ScoreCodeBackup
            {
                Id = sc.Id,
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
            }).ToList() ?? new List<ScoreCodeBackup>()
        }).ToList();

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
        backup.Fleets = fleets.Select(f => new FleetBackup
        {
            Id = f.Id,
            Name = f.Name,
            ShortName = f.ShortName,
            NickName = f.NickName,
            Description = f.Description,
            IsActive = f.IsActive,
            FleetType = f.FleetType,
            BoatClassIds = f.FleetBoatClasses?.Select(fbc => fbc.BoatClassId).ToList() ?? new List<Guid>()
        }).ToList();

        // Competitors with fleet associations
        var competitors = await _dbContext.Competitors
            .Where(c => c.ClubId == clubId)
            .Include(c => c.CompetitorFleets)
            .AsNoTracking()
            .ToListAsync()
            .ConfigureAwait(false);
        backup.Competitors = competitors.Select(c => new CompetitorBackup
        {
            Id = c.Id,
            Name = c.Name,
            SailNumber = c.SailNumber,
            AlternativeSailNumber = c.AlternativeSailNumber,
            BoatName = c.BoatName,
            HomeClubName = c.HomeClubName,
            Notes = c.Notes,
            IsActive = c.IsActive,
            BoatClassId = c.BoatClassId,
            UrlName = c.UrlName,
            UrlId = c.UrlId,
            Created = c.Created,
            FleetIds = c.CompetitorFleets?.Select(cf => cf.FleetId).ToList() ?? new List<Guid>()
        }).ToList();

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
        backup.Series = series.Select(s => new SeriesBackup
        {
            Id = s.Id,
            Name = s.Name,
            UrlName = s.UrlName,
            Description = s.Description,
            Type = s.Type,
            IsImportantSeries = s.IsImportantSeries,
            ResultsLocked = s.ResultsLocked,
            UpdatedDate = s.UpdatedDate,
            UpdatedBy = s.UpdatedBy,
            ScoringSystemId = s.ScoringSystemId,
            TrendOption = s.TrendOption,
            FleetId = s.FleetId,
            PreferAlternativeSailNumbers = s.PreferAlternativeSailNumbers,
            ExcludeFromCompetitorStats = s.ExcludeFromCompetitorStats,
            HideDncDiscards = s.HideDncDiscards,
            ChildrenSeriesAsSingleRace = s.ChildrenSeriesAsSingleRace,
            RaceCount = s.RaceCount,
            DateRestricted = s.DateRestricted,
            EnforcedStartDate = s.EnforcedStartDate,
            EnforcedEndDate = s.EnforcedEndDate,
            StartDate = s.StartDate,
            EndDate = s.EndDate,
            SeasonId = s.Season?.Id,
            ChildrenSeriesIds = s.ChildLinks?.Select(cl => cl.ChildSeriesId).ToList() ?? new List<Guid>(),
            ParentSeriesIds = s.ParentLinks?.Select(pl => pl.ParentSeriesId).ToList() ?? new List<Guid>()
        }).ToList();

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
        backup.Races = races.Select(r => new RaceBackup
        {
            Id = r.Id,
            Name = r.Name,
            Date = r.Date,
            State = r.State,
            Order = r.Order,
            Description = r.Description,
            TrackingUrl = r.TrackingUrl,
            UpdatedDate = r.UpdatedDate,
            UpdatedBy = r.UpdatedBy,
            StartTime = r.StartTime,
            TrackTimes = r.TrackTimes,
            FleetId = r.Fleet?.Id,
            Weather = r.Weather != null ? new WeatherBackup
            {
                Id = r.Weather.Id,
                Description = r.Weather.Description,
                Icon = r.Weather.Icon,
                TemperatureString = r.Weather.TemperatureString,
                TemperatureDegreesKelvin = r.Weather.TemperatureDegreesKelvin,
                WindSpeedString = r.Weather.WindSpeedString,
                WindSpeedMeterPerSecond = r.Weather.WindSpeedMeterPerSecond,
                WindDirectionString = r.Weather.WindDirectionString,
                WindDirectionDegrees = r.Weather.WindDirectionDegrees,
                WindGustString = r.Weather.WindGustString,
                WindGustMeterPerSecond = r.Weather.WindGustMeterPerSecond,
                Humidity = r.Weather.Humidity,
                CloudCoverPercent = r.Weather.CloudCoverPercent,
                CreatedDate = r.Weather.CreatedDate
            } : null,
            Scores = r.Scores?.Select(sc => new ScoreBackup
            {
                Id = sc.Id,
                CompetitorId = sc.CompetitorId,
                RaceId = sc.RaceId,
                Place = sc.Place,
                Code = sc.Code,
                CodePoints = sc.CodePoints,
                FinishTime = sc.FinishTime,
                ElapsedTime = sc.ElapsedTime
            }).ToList() ?? new List<ScoreBackup>(),
            SeriesIds = r.SeriesRaces?.Select(sr => sr.SeriesId).ToList() ?? new List<Guid>()
        }).ToList();

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
        backup.Regattas = regattas.Select(r => new RegattaBackup
        {
            Id = r.Id,
            Name = r.Name,
            UrlName = r.UrlName,
            Description = r.Description,
            Url = r.Url,
            StartDate = r.StartDate,
            EndDate = r.EndDate,
            UpdatedDate = r.UpdatedDate,
            ScoringSystemId = r.ScoringSystemId,
            PreferAlternateSailNumbers = r.PreferAlternateSailNumbers,
            HideFromFrontPage = r.HideFromFrontPage,
            SeasonId = r.Season?.Id,
            SeriesIds = r.RegattaSeries?.Select(rs => rs.SeriesId).ToList() ?? new List<Guid>(),
            FleetIds = r.RegattaFleet?.Select(rf => rf.FleetId).ToList() ?? new List<Guid>()
        }).ToList();

        // Announcements (include CreatedBy/UpdatedBy as last modified user names)
        var announcements = await _dbContext.Announcements
            .Where(a => a.ClubId == clubId && !a.IsDeleted)
            .AsNoTracking()
            .ToListAsync()
            .ConfigureAwait(false);
        backup.Announcements = announcements.Select(a => new AnnouncementBackup
        {
            Id = a.Id,
            RegattaId = a.RegattaId,
            Content = a.Content,
            CreatedDate = a.CreatedDate,
            CreatedLocalDate = a.CreatedLocalDate,
            CreatedBy = a.CreatedBy,
            UpdatedDate = a.UpdatedDate,
            UpdatedLocalDate = a.UpdatedLocalDate,
            UpdatedBy = a.UpdatedBy,
            ArchiveAfter = a.ArchiveAfter
        }).ToList();

        // Documents (include CreatedBy as last modified user name)
        var documents = await _dbContext.Documents
            .Where(d => d.ClubId == clubId)
            .AsNoTracking()
            .ToListAsync()
            .ConfigureAwait(false);
        backup.Documents = documents.Select(d => new DocumentBackup
        {
            Id = d.Id,
            RegattaId = d.RegattaId,
            Name = d.Name,
            ContentType = d.ContentType,
            FileContents = d.FileContents,
            CreatedDate = d.CreatedDate,
            CreatedLocalDate = d.CreatedLocalDate,
            CreatedBy = d.CreatedBy
        }).ToList();

        // Club Sequences
        var clubSequences = await _dbContext.ClubSequences
            .Where(cs => cs.ClubId == clubId)
            .AsNoTracking()
            .ToListAsync()
            .ConfigureAwait(false);
        backup.ClubSequences = clubSequences.Select(cs => new ClubSequenceBackup
        {
            Id = cs.Id,
            NextValue = cs.NextValue,
            SequenceType = cs.SequenceType,
            SequencePrefix = cs.SequencePrefix,
            SequenceSuffix = cs.SequenceSuffix
        }).ToList();

        // Competitor Forwarders
        var competitorForwarders = await _dbContext.CompetitorForwarders
            .Where(cf => cf.OldClubInitials == club.Initials)
            .AsNoTracking()
            .ToListAsync()
            .ConfigureAwait(false);
        backup.CompetitorForwarders = competitorForwarders.Select(cf => new CompetitorForwarderBackup
        {
            Id = cf.Id,
            OldClubInitials = cf.OldClubInitials,
            OldCompetitorUrl = cf.OldCompetitorUrl,
            CompetitorId = cf.CompetitorId,
            Created = cf.Created
        }).ToList();

        // Regatta Forwarders
        var regattaForwarders = await _dbContext.RegattaForwarders
            .Where(rf => rf.OldClubInitials == club.Initials)
            .AsNoTracking()
            .ToListAsync()
            .ConfigureAwait(false);
        backup.RegattaForwarders = regattaForwarders.Select(rf => new RegattaForwarderBackup
        {
            Id = rf.Id,
            OldClubInitials = rf.OldClubInitials,
            OldSeasonUrlName = rf.OldSeasonUrlName,
            OldRegattaUrlName = rf.OldRegattaUrlName,
            RegattaId = rf.NewRegattaId,
            Created = rf.Created
        }).ToList();

        // Series Forwarders
        var seriesForwarders = await _dbContext.SeriesForwarders
            .Where(sf => sf.OldClubInitials == club.Initials)
            .AsNoTracking()
            .ToListAsync()
            .ConfigureAwait(false);
        backup.SeriesForwarders = seriesForwarders.Select(sf => new SeriesForwarderBackup
        {
            Id = sf.Id,
            OldClubInitials = sf.OldClubInitials,
            OldSeasonUrlName = sf.OldSeasonUrlName,
            OldSeriesUrlName = sf.OldSeriesUrlName,
            SeriesId = sf.NewSeriesId,
            Created = sf.Created
        }).ToList();

        // Files (for club logo)
        if (club.LogoFileId.HasValue)
        {
            var logoFile = await _dbContext.Files
                .Where(f => f.Id == club.LogoFileId.Value)
                .AsNoTracking()
                .FirstOrDefaultAsync()
                .ConfigureAwait(false);
            
            if (logoFile != null)
            {
                backup.Files = new List<FileBackup>
                {
                    new FileBackup
                    {
                        Id = logoFile.Id,
                        FileContents = logoFile.FileContents,
                        Created = logoFile.Created,
                        ImportedTime = logoFile.ImportedTime
                    }
                };
            }
        }

        // Series Chart Results
        var seriesIds = series.Select(s => s.Id).ToList();
        var seriesChartResults = await _dbContext.SeriesChartResults
            .Where(scr => seriesIds.Contains(scr.SeriesId))
            .AsNoTracking()
            .ToListAsync()
            .ConfigureAwait(false);
        backup.SeriesChartResults = seriesChartResults.Select(scr => new SeriesChartResultsBackup
        {
            Id = scr.Id,
            SeriesId = scr.SeriesId,
            IsCurrent = scr.IsCurrent,
            Results = scr.Results,
            Created = scr.Created
        }).ToList();

        // Historical Results
        var historicalResults = await _dbContext.HistoricalResults
            .Where(hr => seriesIds.Contains(hr.SeriesId))
            .AsNoTracking()
            .ToListAsync()
            .ConfigureAwait(false);
        backup.HistoricalResults = historicalResults.Select(hr => new HistoricalResultsBackup
        {
            Id = hr.Id,
            SeriesId = hr.SeriesId,
            IsCurrent = hr.IsCurrent,
            Results = hr.Results,
            Created = hr.Created
        }).ToList();

        return backup;
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
        foreach (var bc in backup.BoatClasses ?? Enumerable.Empty<BoatClassBackup>())
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
        foreach (var season in backup.Seasons ?? Enumerable.Empty<SeasonBackup>())
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
        foreach (var ss in backup.ScoringSystems ?? Enumerable.Empty<ScoringSystemBackup>())
        {
            var dbSs = new Db.ScoringSystem
            {
                Id = GetNewGuid(ss.Id),
                ClubId = targetClubId,
                Name = ss.Name,
                DiscardPattern = ss.DiscardPattern,
                ParticipationPercent = ss.ParticipationPercent,
                IsSiteDefault = ss.IsSiteDefault
            };

            // Track default scoring system
            if (ss.Name == backup.DefaultScoringSystemName)
            {
                defaultScoringSystemId = dbSs.Id;
            }

            _dbContext.ScoringSystems.Add(dbSs);

            // Add score codes
            foreach (var sc in ss.ScoreCodes ?? Enumerable.Empty<ScoreCodeBackup>())
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
        foreach (var ss in backup.ScoringSystems ?? Enumerable.Empty<ScoringSystemBackup>())
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

        // 4. Fleets
        foreach (var fleet in backup.Fleets ?? Enumerable.Empty<FleetBackup>())
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
            foreach (var bcId in fleet.BoatClassIds ?? Enumerable.Empty<Guid>())
            {
                var newBcId = GetNewGuidIfExists(bcId);
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
        foreach (var comp in backup.Competitors ?? Enumerable.Empty<CompetitorBackup>())
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
            foreach (var fleetId in comp.FleetIds ?? Enumerable.Empty<Guid>())
            {
                var newFleetId = GetNewGuidIfExists(fleetId);
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
        foreach (var series in backup.Series ?? Enumerable.Empty<SeriesBackup>())
        {
            var newSeasonId = series.SeasonId.HasValue ? GetNewGuidIfExists(series.SeasonId.Value) : null;
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
        foreach (var series in backup.Series ?? Enumerable.Empty<SeriesBackup>())
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
        foreach (var race in backup.Races ?? Enumerable.Empty<RaceBackup>())
        {
            var newFleetId = race.FleetId.HasValue ? GetNewGuidIfExists(race.FleetId.Value) : null;

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
            foreach (var score in race.Scores ?? Enumerable.Empty<ScoreBackup>())
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
            foreach (var seriesId in race.SeriesIds ?? Enumerable.Empty<Guid>())
            {
                var newSeriesId = GetNewGuidIfExists(seriesId);
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
        foreach (var regatta in backup.Regattas ?? Enumerable.Empty<RegattaBackup>())
        {
            var newSeasonId = regatta.SeasonId.HasValue ? GetNewGuidIfExists(regatta.SeasonId.Value) : null;
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
            foreach (var seriesId in regatta.SeriesIds ?? Enumerable.Empty<Guid>())
            {
                var newSeriesId = GetNewGuidIfExists(seriesId);
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
            foreach (var fleetId in regatta.FleetIds ?? Enumerable.Empty<Guid>())
            {
                var newFleetId = GetNewGuidIfExists(fleetId);
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
        foreach (var ann in backup.Announcements ?? Enumerable.Empty<AnnouncementBackup>())
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
        foreach (var doc in backup.Documents ?? Enumerable.Empty<DocumentBackup>())
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

        // 11. Files (for club logo)
        foreach (var file in backup.Files ?? Enumerable.Empty<FileBackup>())
        {
            var dbFile = new Db.File
            {
                Id = GetNewGuid(file.Id),
                FileContents = file.FileContents,
                Created = file.Created,
                ImportedTime = file.ImportedTime
            };
            _dbContext.Files.Add(dbFile);
        }

        // Update club logo file reference
        if (backup.LogoFileId.HasValue)
        {
            var newLogoFileId = GetNewGuidIfExists(backup.LogoFileId.Value);
            if (newLogoFileId.HasValue)
            {
                club.LogoFileId = newLogoFileId.Value;
            }
        }

        // 12. Club Sequences
        foreach (var sequence in backup.ClubSequences ?? Enumerable.Empty<ClubSequenceBackup>())
        {
            var dbSequence = new Db.ClubSequence
            {
                Id = GetNewGuid(sequence.Id),
                ClubId = targetClubId,
                NextValue = sequence.NextValue,
                SequenceType = sequence.SequenceType,
                SequencePrefix = sequence.SequencePrefix,
                SequenceSuffix = sequence.SequenceSuffix
            };
            _dbContext.ClubSequences.Add(dbSequence);
        }

        // 13. Competitor Forwarders
        foreach (var forwarder in backup.CompetitorForwarders ?? Enumerable.Empty<CompetitorForwarderBackup>())
        {
            var newCompetitorId = GetNewGuidIfExists(forwarder.CompetitorId);
            if (newCompetitorId.HasValue)
            {
                var dbForwarder = new Db.CompetitorForwarder
                {
                    Id = GetNewGuid(forwarder.Id),
                    OldClubInitials = forwarder.OldClubInitials,
                    OldCompetitorUrl = forwarder.OldCompetitorUrl,
                    CompetitorId = newCompetitorId.Value,
                    Created = forwarder.Created
                };
                _dbContext.CompetitorForwarders.Add(dbForwarder);
            }
        }

        // 14. Regatta Forwarders
        foreach (var forwarder in backup.RegattaForwarders ?? Enumerable.Empty<RegattaForwarderBackup>())
        {
            var newRegattaId = GetNewGuidIfExists(forwarder.RegattaId);
            if (newRegattaId.HasValue)
            {
                var dbForwarder = new Db.RegattaForwarder
                {
                    Id = GetNewGuid(forwarder.Id),
                    OldClubInitials = forwarder.OldClubInitials,
                    OldSeasonUrlName = forwarder.OldSeasonUrlName,
                    OldRegattaUrlName = forwarder.OldRegattaUrlName,
                    NewRegattaId = newRegattaId.Value,
                    Created = forwarder.Created
                };
                _dbContext.RegattaForwarders.Add(dbForwarder);
            }
        }

        // 15. Series Forwarders
        foreach (var forwarder in backup.SeriesForwarders ?? Enumerable.Empty<SeriesForwarderBackup>())
        {
            var newSeriesId = GetNewGuidIfExists(forwarder.SeriesId);
            if (newSeriesId.HasValue)
            {
                var dbForwarder = new Db.SeriesForwarder
                {
                    Id = GetNewGuid(forwarder.Id),
                    OldClubInitials = forwarder.OldClubInitials,
                    OldSeasonUrlName = forwarder.OldSeasonUrlName,
                    OldSeriesUrlName = forwarder.OldSeriesUrlName,
                    NewSeriesId = newSeriesId.Value,
                    Created = forwarder.Created
                };
                _dbContext.SeriesForwarders.Add(dbForwarder);
            }
        }

        // 16. Series Chart Results
        foreach (var chartResult in backup.SeriesChartResults ?? Enumerable.Empty<SeriesChartResultsBackup>())
        {
            var newSeriesId = GetNewGuidIfExists(chartResult.SeriesId);
            if (newSeriesId.HasValue)
            {
                var dbChartResult = new Db.SeriesChartResults
                {
                    Id = GetNewGuid(chartResult.Id),
                    SeriesId = newSeriesId.Value,
                    IsCurrent = chartResult.IsCurrent,
                    Results = chartResult.Results,
                    Created = chartResult.Created
                };
                _dbContext.SeriesChartResults.Add(dbChartResult);
            }
        }

        // 17. Historical Results
        foreach (var historicalResult in backup.HistoricalResults ?? Enumerable.Empty<HistoricalResultsBackup>())
        {
            var newSeriesId = GetNewGuidIfExists(historicalResult.SeriesId);
            if (newSeriesId.HasValue)
            {
                var dbHistoricalResult = new Db.HistoricalResults
                {
                    Id = GetNewGuid(historicalResult.Id),
                    SeriesId = newSeriesId.Value,
                    IsCurrent = historicalResult.IsCurrent,
                    Results = historicalResult.Results,
                    Created = historicalResult.Created
                };
                _dbContext.HistoricalResults.Add(dbHistoricalResult);
            }
        }

        await _dbContext.SaveChangesAsync().ConfigureAwait(false);

        return true;
    }

    private async Task DeleteExistingClubDataAsync(Guid clubId)
    {
        // Delete in reverse dependency order

        // Series Chart Results (depends on series)
        var seriesIds = await _dbContext.Series.Where(s => s.ClubId == clubId).Select(s => s.Id).ToListAsync().ConfigureAwait(false);
        var seriesChartResults = await _dbContext.SeriesChartResults.Where(scr => seriesIds.Contains(scr.SeriesId)).ToListAsync().ConfigureAwait(false);
        _dbContext.SeriesChartResults.RemoveRange(seriesChartResults);

        // Historical Results (depends on series)
        var historicalResults = await _dbContext.HistoricalResults.Where(hr => seriesIds.Contains(hr.SeriesId)).ToListAsync().ConfigureAwait(false);
        _dbContext.HistoricalResults.RemoveRange(historicalResults);

        // Competitor Forwarders
        var competitorIds = await _dbContext.Competitors.Where(c => c.ClubId == clubId).Select(c => c.Id).ToListAsync().ConfigureAwait(false);
        var competitorForwarders = await _dbContext.CompetitorForwarders.Where(cf => competitorIds.Contains(cf.CompetitorId)).ToListAsync().ConfigureAwait(false);
        _dbContext.CompetitorForwarders.RemoveRange(competitorForwarders);

        // Regatta Forwarders
        var regattaIds = await _dbContext.Regattas.Where(r => r.ClubId == clubId).Select(r => r.Id).ToListAsync().ConfigureAwait(false);
        var regattaForwarders = await _dbContext.RegattaForwarders.Where(rf => regattaIds.Contains(rf.NewRegattaId)).ToListAsync().ConfigureAwait(false);
        _dbContext.RegattaForwarders.RemoveRange(regattaForwarders);

        // Series Forwarders
        var seriesForwarders = await _dbContext.SeriesForwarders.Where(sf => seriesIds.Contains(sf.NewSeriesId)).ToListAsync().ConfigureAwait(false);
        _dbContext.SeriesForwarders.RemoveRange(seriesForwarders);

        // Documents
        var documents = await _dbContext.Documents.Where(d => d.ClubId == clubId || (d.RegattaId.HasValue && regattaIds.Contains(d.RegattaId.Value))).ToListAsync().ConfigureAwait(false);
        _dbContext.Documents.RemoveRange(documents);

        // Announcements
        var announcements = await _dbContext.Announcements.IgnoreQueryFilters().Where(a => a.ClubId == clubId || (a.RegattaId.HasValue && regattaIds.Contains(a.RegattaId.Value))).ToListAsync().ConfigureAwait(false);
        _dbContext.Announcements.RemoveRange(announcements);

        // Scores (via races)
        var raceIds = await _dbContext.Races.Where(r => r.ClubId == clubId).Select(r => r.Id).ToListAsync().ConfigureAwait(false);
        var scores = await _dbContext.Scores.Where(s => raceIds.Contains(s.RaceId)).ToListAsync().ConfigureAwait(false);
        _dbContext.Scores.RemoveRange(scores);

        // SeriesRace links
        var seriesRaces = await _dbContext.SeriesRaces.Where(sr => seriesIds.Contains(sr.SeriesId)).ToListAsync().ConfigureAwait(false);
        _dbContext.SeriesRaces.RemoveRange(seriesRaces);

        // Races
        var races = await _dbContext.Races.Where(r => r.ClubId == clubId).Include(r => r.Weather).ToListAsync().ConfigureAwait(false);
        _dbContext.Races.RemoveRange(races);

        // RegattaSeries and RegattaFleet links
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
        var competitorIds2 = await _dbContext.Competitors.Where(c => c.ClubId == clubId).Select(c => c.Id).ToListAsync().ConfigureAwait(false);
        var competitorFleets = await _dbContext.CompetitorFleets.Where(cf => competitorIds2.Contains(cf.CompetitorId)).ToListAsync().ConfigureAwait(false);
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

        // Club Sequences
        var clubSequences = await _dbContext.ClubSequences.Where(cs => cs.ClubId == clubId).ToListAsync().ConfigureAwait(false);
        _dbContext.ClubSequences.RemoveRange(clubSequences);

        // Note: Files (logo) are not deleted since they might be referenced elsewhere
        // and club.LogoFileId will be cleared/updated separately

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
