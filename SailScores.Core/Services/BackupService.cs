using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using SailScores.Core.Model.BackupEntities;
using SailScores.Database;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Db = SailScores.Database.Entities;

namespace SailScores.Core.Services;

public class BackupService : IBackupService
{
    private readonly ISailScoresContext _dbContext;
    private readonly ILogger<BackupService> _logger;

    // Used for GUID remapping during restore
    private Dictionary<Guid, Guid> _guidMap;

    // Batch size for loading races during backup to limit database load
    private const int RaceBatchSize = 50;

    // Extended timeout for backup reads (5 minutes)
    private static readonly TimeSpan BackupCommandTimeout = TimeSpan.FromMinutes(5);

    public BackupService(ISailScoresContext dbContext, ILogger<BackupService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ClubBackupData> CreateBackupAsync(Guid clubId, string createdBy, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("Starting backup creation for club {ClubId} by {CreatedBy}", clubId, createdBy);

        // Use ReadUncommitted isolation level for minimal database blocking during backup reads.
        // This allows dirty reads but avoids taking shared locks that would block writers.
        // For backups, slight inconsistencies are acceptable since we're taking a point-in-time snapshot.
        // Note: These relational features are only available with SQL Server, not InMemory provider.
        var isRelational = _dbContext.Database.ProviderName?.Contains("InMemory") != true;

        int? originalTimeout = null;
        IDbContextTransaction transaction = null;

        if (isRelational)
        {
            originalTimeout = _dbContext.Database.GetCommandTimeout();
            _dbContext.Database.SetCommandTimeout(BackupCommandTimeout);
            transaction = await _dbContext.Database
                .BeginTransactionAsync(IsolationLevel.ReadUncommitted, cancellationToken)
                .ConfigureAwait(false);
        }

        try
        {
            var backup = await CreateBackupInternalAsync(clubId, createdBy, cancellationToken).ConfigureAwait(false);

            if (transaction != null)
            {
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }

            stopwatch.Stop();
            _logger.LogInformation(
                "Backup completed for club {ClubId} in {ElapsedMs}ms. Entities: {RaceCount} races, {CompetitorCount} competitors, {SeriesCount} series, {ScoreCount} scores",
                clubId,
                stopwatch.ElapsedMilliseconds,
                backup.Races?.Count ?? 0,
                backup.Competitors?.Count ?? 0,
                backup.Series?.Count ?? 0,
                backup.Races?.Sum(r => r.Scores?.Count ?? 0) ?? 0);

            return backup;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Backup failed for club {ClubId} after {ElapsedMs}ms", clubId, stopwatch.ElapsedMilliseconds);
            throw;
        }
        finally
        {
            if (transaction != null)
            {
                await transaction.DisposeAsync().ConfigureAwait(false);
            }

            if (isRelational && originalTimeout.HasValue)
            {
                _dbContext.Database.SetCommandTimeout(originalTimeout);
            }
        }
    }

    private async Task<ClubBackupData> CreateBackupInternalAsync(Guid clubId, string createdBy, CancellationToken cancellationToken)
    {
        var club = await _dbContext.Clubs
            .Where(c => c.Id == clubId)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken)
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
            .ToListAsync(cancellationToken)
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
            .ToListAsync(cancellationToken)
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
            .ToListAsync(cancellationToken)
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
                .FirstOrDefaultAsync(cancellationToken)
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
            .ToListAsync(cancellationToken)
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
            .ToListAsync(cancellationToken)
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
            .ToListAsync(cancellationToken)
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

        // Races with scores, weather, and series associations (batched to limit DTU usage)
        var raceIds = await _dbContext.Races
            .Where(r => r.ClubId == clubId)
            .Select(r => r.Id)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        _logger.LogDebug("Loading {RaceCount} races in batches of {BatchSize} for club {ClubId}", raceIds.Count, RaceBatchSize, clubId);

        var allRaceBackups = new List<RaceBackup>();
        var batchNumber = 0;
        var batchStopwatch = Stopwatch.StartNew();

        foreach (var batch in raceIds.Chunk(RaceBatchSize))
        {
            cancellationToken.ThrowIfCancellationRequested();
            batchNumber++;
            batchStopwatch.Restart();

            var raceBatch = await _dbContext.Races
                .Where(r => batch.Contains(r.Id))
                .Include(r => r.Fleet)
                .Include(r => r.Scores)
                .Include(r => r.Weather)
                .Include(r => r.SeriesRaces)
                .AsNoTracking()
                .AsSplitQuery()
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            _logger.LogDebug("Race batch {BatchNumber} loaded {Count} races in {ElapsedMs}ms", batchNumber, raceBatch.Count, batchStopwatch.ElapsedMilliseconds);

            allRaceBackups.AddRange(raceBatch.Select(r => new RaceBackup
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
            }));
        }
        backup.Races = allRaceBackups;

        // Regattas
        var regattas = await _dbContext.Regattas
            .Where(r => r.ClubId == clubId)
            .Include(r => r.RegattaSeries)
            .Include(r => r.RegattaFleet)
            .Include(r => r.Season)
            .AsNoTracking()
            .AsSplitQuery()
            .ToListAsync(cancellationToken)
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
            .ToListAsync(cancellationToken)
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
            .ToListAsync(cancellationToken)
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
            .ToListAsync(cancellationToken)
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
            .ToListAsync(cancellationToken)
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
            .ToListAsync(cancellationToken)
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
            .ToListAsync(cancellationToken)
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
                .FirstOrDefaultAsync(cancellationToken)
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
            .ToListAsync(cancellationToken)
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
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        backup.HistoricalResults = historicalResults.Select(hr => new HistoricalResultsBackup
        {
            Id = hr.Id,
            SeriesId = hr.SeriesId,
            IsCurrent = hr.IsCurrent,
            Results = hr.Results,
            Created = hr.Created
        }).ToList();

        // Populate entity counts for validation
        backup.Metadata.BoatClassCount = backup.BoatClasses?.Count ?? 0;
        backup.Metadata.CompetitorCount = backup.Competitors?.Count ?? 0;
        backup.Metadata.FleetCount = backup.Fleets?.Count ?? 0;
        backup.Metadata.RaceCount = backup.Races?.Count ?? 0;
        backup.Metadata.ScoreCount = backup.Races?.Sum(r => r.Scores?.Count ?? 0) ?? 0;
        backup.Metadata.SeasonCount = backup.Seasons?.Count ?? 0;
        backup.Metadata.SeriesCount = backup.Series?.Count ?? 0;
        backup.Metadata.RegattaCount = backup.Regattas?.Count ?? 0;
        backup.Metadata.ScoringSystemCount = backup.ScoringSystems?.Count ?? 0;

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

        if (!string.IsNullOrEmpty(backup.Metadata.Schema)
            && backup.Metadata.Schema != ClubBackupMetadata.SchemaIdentifier)
        {
            return new BackupValidationResult
            {
                IsValid = false,
                ErrorMessage = $"Unrecognized backup schema: '{backup.Metadata.Schema}'.",
                Version = backup.Metadata.Version,
                SourceClubName = backup.Metadata.SourceClubName,
                CreatedDateUtc = backup.Metadata.CreatedDateUtc
            };
        }

        // Validate entity counts match actual data when counts are present
        var warnings = new List<string>();
        if (backup.Metadata.RaceCount.HasValue
            && backup.Races != null
            && backup.Metadata.RaceCount.Value != backup.Races.Count)
        {
            warnings.Add($"Expected {backup.Metadata.RaceCount} races but found {backup.Races.Count}.");
        }
        if (backup.Metadata.CompetitorCount.HasValue
            && backup.Competitors != null
            && backup.Metadata.CompetitorCount.Value != backup.Competitors.Count)
        {
            warnings.Add($"Expected {backup.Metadata.CompetitorCount} competitors but found {backup.Competitors.Count}.");
        }
        if (backup.Metadata.SeriesCount.HasValue
            && backup.Series != null
            && backup.Metadata.SeriesCount.Value != backup.Series.Count)
        {
            warnings.Add($"Expected {backup.Metadata.SeriesCount} series but found {backup.Series.Count}.");
        }

        return new BackupValidationResult
        {
            IsValid = true,
            Version = backup.Metadata.Version,
            SourceClubName = backup.Metadata.SourceClubName,
            CreatedDateUtc = backup.Metadata.CreatedDateUtc,
            Warnings = warnings
        };
    }

    public async Task<BackupDryRunResult> ValidateBackupAsync(Guid targetClubId, ClubBackupData backup, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting dry-run validation of backup from {SourceClub} for club {TargetClubId}",
            backup.Metadata?.SourceClubName ?? "unknown", targetClubId);

        var result = new BackupDryRunResult
        {
            Errors = new List<string>(),
            Warnings = new List<string>(),
            ReferenceIssues = new BackupReferenceIssues()
        };

        // First run quick validation
        var quickValidation = ValidateBackup(backup);
        if (!quickValidation.IsValid)
        {
            result.IsValid = false;
            result.CanRestore = false;
            result.Errors.Add(quickValidation.ErrorMessage);
            return result;
        }

        result.Version = quickValidation.Version;
        result.SourceClubName = quickValidation.SourceClubName;
        result.CreatedDateUtc = quickValidation.CreatedDateUtc;
        result.Warnings.AddRange(quickValidation.Warnings);

        // Verify target club exists
        var targetClub = await _dbContext.Clubs
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == targetClubId, cancellationToken)
            .ConfigureAwait(false);

        if (targetClub == null)
        {
            result.IsValid = false;
            result.CanRestore = false;
            result.Errors.Add($"Target club {targetClubId} not found.");
            return result;
        }

        // Build entity summary
        result.EntitySummary = new BackupEntitySummary
        {
            BoatClassCount = backup.BoatClasses?.Count ?? 0,
            SeasonCount = backup.Seasons?.Count ?? 0,
            FleetCount = backup.Fleets?.Count ?? 0,
            CompetitorCount = backup.Competitors?.Count ?? 0,
            ScoringSystemCount = backup.ScoringSystems?.Count ?? 0,
            SeriesCount = backup.Series?.Count ?? 0,
            RaceCount = backup.Races?.Count ?? 0,
            ScoreCount = backup.Races?.Sum(r => r.Scores?.Count ?? 0) ?? 0,
            RegattaCount = backup.Regattas?.Count ?? 0,
            AnnouncementCount = backup.Announcements?.Count ?? 0,
            DocumentCount = backup.Documents?.Count ?? 0
        };

        // Build lookup sets for reference checking
        var boatClassIds = new HashSet<Guid>(backup.BoatClasses?.Select(bc => bc.Id) ?? Enumerable.Empty<Guid>());
        var fleetIds = new HashSet<Guid>(backup.Fleets?.Select(f => f.Id) ?? Enumerable.Empty<Guid>());
        var seasonIds = new HashSet<Guid>(backup.Seasons?.Select(s => s.Id) ?? Enumerable.Empty<Guid>());
        var competitorIds = new HashSet<Guid>(backup.Competitors?.Select(c => c.Id) ?? Enumerable.Empty<Guid>());
        var scoringSystemIds = new HashSet<Guid>(backup.ScoringSystems?.Select(ss => ss.Id) ?? Enumerable.Empty<Guid>());
        var seriesIds = new HashSet<Guid>(backup.Series?.Select(s => s.Id) ?? Enumerable.Empty<Guid>());

        // Check competitor boat class references
        foreach (var competitor in backup.Competitors ?? Enumerable.Empty<CompetitorBackup>())
        {
            if (competitor.BoatClassId != Guid.Empty && !boatClassIds.Contains(competitor.BoatClassId))
            {
                result.ReferenceIssues.OrphanedCompetitorBoatClasses.Add(
                    $"Competitor '{competitor.Name}' ({competitor.SailNumber}) references missing boat class {competitor.BoatClassId}");
            }
        }

        // Check fleet boat class references
        foreach (var fleet in backup.Fleets ?? Enumerable.Empty<FleetBackup>())
        {
            foreach (var bcId in fleet.BoatClassIds ?? Enumerable.Empty<Guid>())
            {
                if (!boatClassIds.Contains(bcId))
                {
                    result.ReferenceIssues.OrphanedFleetBoatClasses.Add(
                        $"Fleet '{fleet.Name}' references missing boat class {bcId}");
                }
            }
        }

        // Check series references
        foreach (var series in backup.Series ?? Enumerable.Empty<SeriesBackup>())
        {
            if (series.SeasonId.HasValue && !seasonIds.Contains(series.SeasonId.Value))
            {
                result.ReferenceIssues.OrphanedSeriesSeasons.Add(
                    $"Series '{series.Name}' references missing season {series.SeasonId.Value}");
            }
            if (series.FleetId.HasValue && !fleetIds.Contains(series.FleetId.Value))
            {
                result.ReferenceIssues.OrphanedSeriesFleets.Add(
                    $"Series '{series.Name}' references missing fleet {series.FleetId.Value}");
            }
            if (series.ScoringSystemId.HasValue && !scoringSystemIds.Contains(series.ScoringSystemId.Value))
            {
                result.ReferenceIssues.OrphanedSeriesScoringSystems.Add(
                    $"Series '{series.Name}' references missing scoring system {series.ScoringSystemId.Value}");
            }
        }

        // Check scoring system parent references
        foreach (var ss in backup.ScoringSystems ?? Enumerable.Empty<ScoringSystemBackup>())
        {
            if (ss.ParentSystemId.HasValue && !scoringSystemIds.Contains(ss.ParentSystemId.Value))
            {
                // Check if parent exists in database (might be a site-wide scoring system)
                var parentExists = await _dbContext.ScoringSystems
                    .AsNoTracking()
                    .AnyAsync(s => s.Id == ss.ParentSystemId.Value, cancellationToken)
                    .ConfigureAwait(false);

                if (!parentExists)
                {
                    result.ReferenceIssues.UnresolvableParentScoringSystems.Add(
                        $"Scoring system '{ss.Name}' references missing parent system {ss.ParentSystemId.Value}");
                }
            }
        }

        // Check score competitor references
        foreach (var race in backup.Races ?? Enumerable.Empty<RaceBackup>())
        {
            foreach (var score in race.Scores ?? Enumerable.Empty<ScoreBackup>())
            {
                if (!competitorIds.Contains(score.CompetitorId))
                {
                    result.ReferenceIssues.OrphanedScoreCompetitors.Add(
                        $"Score in race '{race.Name}' ({race.Date:d}) references missing competitor {score.CompetitorId}");
                }
            }
        }

        // Determine overall validity
        result.IsValid = !result.ReferenceIssues.HasIssues && result.Errors.Count == 0;
        result.CanRestore = result.Errors.Count == 0; // Can restore even with warnings/orphans

        // Add warnings for any reference issues found
        if (result.ReferenceIssues.HasIssues)
        {
            result.Warnings.Add($"Found {result.ReferenceIssues.OrphanedCompetitorBoatClasses.Count} orphaned competitor boat class references");
            result.Warnings.Add($"Found {result.ReferenceIssues.OrphanedFleetBoatClasses.Count} orphaned fleet boat class references");
            result.Warnings.Add($"Found {result.ReferenceIssues.OrphanedScoreCompetitors.Count} orphaned score competitor references");
            result.Warnings.Add($"Found {result.ReferenceIssues.OrphanedSeriesSeasons.Count} orphaned series season references");
            result.Warnings.Add($"Found {result.ReferenceIssues.OrphanedSeriesFleets.Count} orphaned series fleet references");
            result.Warnings.Add($"Found {result.ReferenceIssues.OrphanedSeriesScoringSystems.Count} orphaned series scoring system references");
            result.Warnings.Add($"Found {result.ReferenceIssues.UnresolvableParentScoringSystems.Count} unresolvable parent scoring systems");

            // Remove zero-count warnings
            result.Warnings = result.Warnings.Where(w => !w.Contains(" 0 ")).ToList();
        }

        _logger.LogInformation(
            "Dry-run validation completed for club {TargetClubId}. IsValid={IsValid}, CanRestore={CanRestore}, Errors={ErrorCount}, Warnings={WarningCount}",
            targetClubId, result.IsValid, result.CanRestore, result.Errors.Count, result.Warnings.Count);

        return result;
    }

    public async Task<bool> RestoreBackupAsync(Guid targetClubId, ClubBackupData backup, bool preserveClubName = true, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation(
            "Starting restore of backup from {SourceClub} (created {CreatedDate:u}) to club {TargetClubId}",
            backup.Metadata?.SourceClubName ?? "unknown",
            backup.Metadata?.CreatedDateUtc,
            targetClubId);

        var validation = ValidateBackup(backup);
        if (!validation.IsValid)
        {
            _logger.LogWarning("Restore validation failed for club {TargetClubId}: {ErrorMessage}", targetClubId, validation.ErrorMessage);
            throw new InvalidOperationException($"Invalid backup: {validation.ErrorMessage}");
        }

        // Wrap entire restore in a transaction for atomicity.
        // If any step fails, all changes will be rolled back to prevent partial restores.
        // Note: Transactions are only supported with relational providers, not InMemory.
        var isRelational = _dbContext.Database.ProviderName?.Contains("InMemory") != true;
        IDbContextTransaction transaction = null;

        if (isRelational)
        {
            transaction = await _dbContext.Database
                .BeginTransactionAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        try
        {
            _guidMap = new Dictionary<Guid, Guid>();

            var club = await _dbContext.Clubs
                .Include(c => c.WeatherSettings)
                .FirstOrDefaultAsync(c => c.Id == targetClubId, cancellationToken)
                .ConfigureAwait(false)
                ?? throw new InvalidOperationException($"Target club {targetClubId} not found.");

            _logger.LogDebug("Deleting existing data for club {TargetClubId}", targetClubId);
            await DeleteExistingClubDataAsync(targetClubId, cancellationToken).ConfigureAwait(false);

            UpdateClubSettings(club, backup, preserveClubName);

            RestoreBoatClasses(backup, targetClubId);
            RestoreSeasons(backup, targetClubId);
            var defaultScoringSystemId = RestoreScoringSystems(backup, targetClubId);
            RestoreFleets(backup, targetClubId);
            RestoreCompetitors(backup, targetClubId);

            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await UpdateScoringSystemParentReferencesAsync(backup, cancellationToken).ConfigureAwait(false);

            if (defaultScoringSystemId.HasValue)
            {
                club.DefaultScoringSystemId = defaultScoringSystemId;
            }

            await RestoreSeriesAsync(backup, targetClubId, cancellationToken).ConfigureAwait(false);
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            RestoreSeriesLinks(backup);
            await RestoreRacesAsync(backup, targetClubId, cancellationToken).ConfigureAwait(false);
            await RestoreRegattasAsync(backup, targetClubId, cancellationToken).ConfigureAwait(false);

            RestoreAnnouncements(backup, targetClubId);
            RestoreDocuments(backup, targetClubId);
            RestoreFiles(backup, club);
            RestoreClubSequences(backup, targetClubId);
            RestoreForwarders(backup);
            RestoreSeriesResults(backup);

            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            if (transaction != null)
            {
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }

            stopwatch.Stop();
            _logger.LogInformation(
                "Restore completed for club {TargetClubId} in {ElapsedMs}ms. Restored: {RaceCount} races, {CompetitorCount} competitors, {SeriesCount} series",
                targetClubId,
                stopwatch.ElapsedMilliseconds,
                backup.Races?.Count ?? 0,
                backup.Competitors?.Count ?? 0,
                backup.Series?.Count ?? 0);

            return true;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Restore failed for club {TargetClubId} after {ElapsedMs}ms", targetClubId, stopwatch.ElapsedMilliseconds);

            if (transaction != null)
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            }
            throw;
        }
        finally
        {
            if (transaction != null)
            {
                await transaction.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    private void UpdateClubSettings(Db.Club club, ClubBackupData backup, bool preserveClubName)
    {
        // Only optionally preserve club name; initials are always preserved, URL is always restored
        if (!preserveClubName)
        {
            club.Name = backup.Name;
        }

        // Always restore these settings
        club.Url = backup.Url;
        club.Description = backup.Description;
        club.HomePageDescription = backup.HomePageDescription;
        club.ShowClubInResults = backup.ShowClubInResults;
        club.ShowCalendarInNav = backup.ShowCalendarInNav;
        club.Locale = backup.Locale;
        club.DefaultRaceDateOffset = backup.DefaultRaceDateOffset;
        club.StatisticsDescription = backup.StatisticsDescription;

        // Note: club.Initials are NOT updated (always preserved from target club)

        if (backup.WeatherSettings != null)
        {
            club.WeatherSettings ??= new Db.WeatherSettings();
            club.WeatherSettings.Latitude = backup.WeatherSettings.Latitude;
            club.WeatherSettings.Longitude = backup.WeatherSettings.Longitude;
            club.WeatherSettings.TemperatureUnits = backup.WeatherSettings.TemperatureUnits;
            club.WeatherSettings.WindSpeedUnits = backup.WeatherSettings.WindSpeedUnits;
        }
    }

    private void RestoreBoatClasses(ClubBackupData backup, Guid targetClubId)
    {
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
    }

    private void RestoreSeasons(ClubBackupData backup, Guid targetClubId)
    {
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
    }

    private Guid? RestoreScoringSystems(ClubBackupData backup, Guid targetClubId)
    {
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

            if (ss.Name == backup.DefaultScoringSystemName)
            {
                defaultScoringSystemId = dbSs.Id;
            }

            _dbContext.ScoringSystems.Add(dbSs);
            RestoreScoreCodes(ss, dbSs.Id);
        }

        return defaultScoringSystemId;
    }

    private void RestoreScoreCodes(ScoringSystemBackup ss, Guid scoringSystemId)
    {
        foreach (var sc in ss.ScoreCodes ?? Enumerable.Empty<ScoreCodeBackup>())
        {
            var dbSc = new Db.ScoreCode
            {
                Id = GetNewGuid(sc.Id),
                ScoringSystemId = scoringSystemId,
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

    private async Task UpdateScoringSystemParentReferencesAsync(ClubBackupData backup, CancellationToken cancellationToken = default)
    {
        foreach (var ss in backup.ScoringSystems ?? Enumerable.Empty<ScoringSystemBackup>())
        {
            if (!ss.ParentSystemId.HasValue) continue;

            var newId = GetNewGuidIfExists(ss.Id);
            var newParentId = GetNewOrOldGuid(ss.ParentSystemId.Value);

            if (newId.HasValue && newParentId.HasValue)
            {
                var dbSs = await _dbContext.ScoringSystems.FindAsync([newId.Value], cancellationToken).ConfigureAwait(false);
                if (dbSs != null)
                {
                    dbSs.ParentSystemId = newParentId.Value;
                }
            }
        }
    }

    private void RestoreFleets(ClubBackupData backup, Guid targetClubId)
    {
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
            RestoreFleetBoatClasses(fleet, dbFleet.Id);
        }
    }

    private void RestoreFleetBoatClasses(FleetBackup fleet, Guid fleetId)
    {
        foreach (var bcId in fleet.BoatClassIds ?? Enumerable.Empty<Guid>())
        {
            var newBcId = GetNewGuidIfExists(bcId);
            if (newBcId.HasValue)
            {
                var fbc = new Db.FleetBoatClass
                {
                    FleetId = fleetId,
                    BoatClassId = newBcId.Value
                };
                _dbContext.FleetBoatClasses.Add(fbc);
            }
        }
    }

    private void RestoreCompetitors(ClubBackupData backup, Guid targetClubId)
    {
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
            RestoreCompetitorFleets(comp, dbComp.Id);
        }
    }

    private void RestoreCompetitorFleets(CompetitorBackup comp, Guid competitorId)
    {
        foreach (var fleetId in comp.FleetIds ?? Enumerable.Empty<Guid>())
        {
            var newFleetId = GetNewGuidIfExists(fleetId);
            if (newFleetId.HasValue)
            {
                var cf = new Db.CompetitorFleet
                {
                    CompetitorId = competitorId,
                    FleetId = newFleetId.Value
                };
                _dbContext.CompetitorFleets.Add(cf);
            }
        }
    }

    private async Task RestoreSeriesAsync(ClubBackupData backup, Guid targetClubId, CancellationToken cancellationToken = default)
    {
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

            if (newSeasonId.HasValue)
            {
                var season = await _dbContext.Seasons.FindAsync([newSeasonId.Value], cancellationToken).ConfigureAwait(false);
                dbSeries.Season = season;
            }

            _dbContext.Series.Add(dbSeries);
        }
    }

    private void RestoreSeriesLinks(ClubBackupData backup)
    {
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
    }

    private async Task RestoreRacesAsync(ClubBackupData backup, Guid targetClubId, CancellationToken cancellationToken = default)
    {
        foreach (var race in backup.Races ?? Enumerable.Empty<RaceBackup>())
        {
            var dbRace = await CreateRaceEntityAsync(race, targetClubId, cancellationToken).ConfigureAwait(false);
            _dbContext.Races.Add(dbRace);
            RestoreScores(race, dbRace.Id);
            RestoreSeriesRaceLinks(race, dbRace.Id);
        }
    }

    private async Task<Db.Race> CreateRaceEntityAsync(RaceBackup race, Guid targetClubId, CancellationToken cancellationToken = default)
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

        if (newFleetId.HasValue)
        {
            var fleet = await _dbContext.Fleets.FindAsync([newFleetId.Value], cancellationToken).ConfigureAwait(false);
            dbRace.Fleet = fleet;
        }

        if (race.Weather != null)
        {
            dbRace.Weather = CreateWeatherEntity(race.Weather);
        }

        return dbRace;
    }

    private static Db.Weather CreateWeatherEntity(WeatherBackup weather)
    {
        return new Db.Weather
        {
            Id = Guid.NewGuid(),
            Description = weather.Description,
            Icon = weather.Icon,
            TemperatureString = weather.TemperatureString,
            TemperatureDegreesKelvin = weather.TemperatureDegreesKelvin,
            WindSpeedString = weather.WindSpeedString,
            WindSpeedMeterPerSecond = weather.WindSpeedMeterPerSecond,
            WindDirectionString = weather.WindDirectionString,
            WindDirectionDegrees = weather.WindDirectionDegrees,
            WindGustString = weather.WindGustString,
            WindGustMeterPerSecond = weather.WindGustMeterPerSecond,
            Humidity = weather.Humidity,
            CloudCoverPercent = weather.CloudCoverPercent,
            CreatedDate = weather.CreatedDate
        };
    }

    private void RestoreScores(RaceBackup race, Guid raceId)
    {
        foreach (var score in race.Scores ?? Enumerable.Empty<ScoreBackup>())
        {
            var newCompId = GetNewGuidIfExists(score.CompetitorId);
            if (!newCompId.HasValue) continue;

            var dbScore = new Db.Score
            {
                Id = GetNewGuid(score.Id),
                CompetitorId = newCompId.Value,
                RaceId = raceId,
                Place = score.Place,
                Code = score.Code,
                CodePoints = score.CodePoints,
                FinishTime = score.FinishTime,
                ElapsedTime = score.ElapsedTime
            };
            _dbContext.Scores.Add(dbScore);
        }
    }

    private void RestoreSeriesRaceLinks(RaceBackup race, Guid raceId)
    {
        foreach (var seriesId in race.SeriesIds ?? Enumerable.Empty<Guid>())
        {
            var newSeriesId = GetNewGuidIfExists(seriesId);
            if (newSeriesId.HasValue)
            {
                var sr = new Db.SeriesRace
                {
                    RaceId = raceId,
                    SeriesId = newSeriesId.Value
                };
                _dbContext.SeriesRaces.Add(sr);
            }
        }
    }

    private async Task RestoreRegattasAsync(ClubBackupData backup, Guid targetClubId, CancellationToken cancellationToken = default)
    {
        foreach (var regatta in backup.Regattas ?? Enumerable.Empty<RegattaBackup>())
        {
            var dbRegatta = await CreateRegattaEntityAsync(regatta, targetClubId, cancellationToken).ConfigureAwait(false);
            _dbContext.Regattas.Add(dbRegatta);
            RestoreRegattaSeriesLinks(regatta, dbRegatta.Id);
            RestoreRegattaFleetLinks(regatta, dbRegatta.Id);
        }
    }

    private async Task<Db.Regatta> CreateRegattaEntityAsync(RegattaBackup regatta, Guid targetClubId, CancellationToken cancellationToken = default)
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

        if (newSeasonId.HasValue)
        {
            var season = await _dbContext.Seasons.FindAsync([newSeasonId.Value], cancellationToken).ConfigureAwait(false);
            dbRegatta.Season = season;
        }

        return dbRegatta;
    }

    private void RestoreRegattaSeriesLinks(RegattaBackup regatta, Guid regattaId)
    {
        foreach (var seriesId in regatta.SeriesIds ?? Enumerable.Empty<Guid>())
        {
            var newSeriesId = GetNewGuidIfExists(seriesId);
            if (newSeriesId.HasValue)
            {
                var rs = new Db.RegattaSeries
                {
                    RegattaId = regattaId,
                    SeriesId = newSeriesId.Value
                };
                _dbContext.RegattaSeries.Add(rs);
            }
        }
    }

    private void RestoreRegattaFleetLinks(RegattaBackup regatta, Guid regattaId)
    {
        foreach (var fleetId in regatta.FleetIds ?? Enumerable.Empty<Guid>())
        {
            var newFleetId = GetNewGuidIfExists(fleetId);
            if (newFleetId.HasValue)
            {
                var rf = new Db.RegattaFleet
                {
                    RegattaId = regattaId,
                    FleetId = newFleetId.Value
                };
                _dbContext.RegattaFleets.Add(rf);
            }
        }
    }

    private void RestoreAnnouncements(ClubBackupData backup, Guid targetClubId)
    {
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
                PreviousVersion = null,
                IsDeleted = false
            };
            _dbContext.Announcements.Add(dbAnn);
        }
    }

    private void RestoreDocuments(ClubBackupData backup, Guid targetClubId)
    {
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
                PreviousVersion = null
            };
            _dbContext.Documents.Add(dbDoc);
        }
    }

    private void RestoreFiles(ClubBackupData backup, Db.Club club)
    {
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

        if (backup.LogoFileId.HasValue)
        {
            var newLogoFileId = GetNewGuidIfExists(backup.LogoFileId.Value);
            if (newLogoFileId.HasValue)
            {
                club.LogoFileId = newLogoFileId.Value;
            }
        }
    }

    private void RestoreClubSequences(ClubBackupData backup, Guid targetClubId)
    {
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
    }

    private void RestoreForwarders(ClubBackupData backup)
    {
        RestoreCompetitorForwarders(backup);
        RestoreRegattaForwarders(backup);
        RestoreSeriesForwarders(backup);
    }

    private void RestoreCompetitorForwarders(ClubBackupData backup)
    {
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
    }

    private void RestoreRegattaForwarders(ClubBackupData backup)
    {
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
    }

    private void RestoreSeriesForwarders(ClubBackupData backup)
    {
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
    }

    private void RestoreSeriesResults(ClubBackupData backup)
    {
        RestoreSeriesChartResults(backup);
        RestoreHistoricalResults(backup);
    }

    private void RestoreSeriesChartResults(ClubBackupData backup)
    {
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
    }

    private void RestoreHistoricalResults(ClubBackupData backup)
    {
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
    }

    private async Task DeleteExistingClubDataAsync(Guid clubId, CancellationToken cancellationToken = default)
    {
        // Delete in reverse dependency order

        // Series Chart Results (depends on series)
        var seriesIds = await _dbContext.Series.Where(s => s.ClubId == clubId).Select(s => s.Id).ToListAsync(cancellationToken).ConfigureAwait(false);
        var seriesChartResults = await _dbContext.SeriesChartResults.Where(scr => seriesIds.Contains(scr.SeriesId)).ToListAsync(cancellationToken).ConfigureAwait(false);
        _dbContext.SeriesChartResults.RemoveRange(seriesChartResults);

        // Historical Results (depends on series)
        var historicalResults = await _dbContext.HistoricalResults.Where(hr => seriesIds.Contains(hr.SeriesId)).ToListAsync(cancellationToken).ConfigureAwait(false);
        _dbContext.HistoricalResults.RemoveRange(historicalResults);

        // Competitor Forwarders
        var competitorIds = await _dbContext.Competitors.Where(c => c.ClubId == clubId).Select(c => c.Id).ToListAsync(cancellationToken).ConfigureAwait(false);
        var competitorForwarders = await _dbContext.CompetitorForwarders.Where(cf => competitorIds.Contains(cf.CompetitorId)).ToListAsync(cancellationToken).ConfigureAwait(false);
        _dbContext.CompetitorForwarders.RemoveRange(competitorForwarders);

        // Regatta Forwarders
        var regattaIds = await _dbContext.Regattas.Where(r => r.ClubId == clubId).Select(r => r.Id).ToListAsync(cancellationToken).ConfigureAwait(false);
        var regattaForwarders = await _dbContext.RegattaForwarders.Where(rf => regattaIds.Contains(rf.NewRegattaId)).ToListAsync(cancellationToken).ConfigureAwait(false);
        _dbContext.RegattaForwarders.RemoveRange(regattaForwarders);

        // Series Forwarders
        var seriesForwarders = await _dbContext.SeriesForwarders.Where(sf => seriesIds.Contains(sf.NewSeriesId)).ToListAsync(cancellationToken).ConfigureAwait(false);
        _dbContext.SeriesForwarders.RemoveRange(seriesForwarders);

        // Documents
        var documents = await _dbContext.Documents.Where(d => d.ClubId == clubId || (d.RegattaId.HasValue && regattaIds.Contains(d.RegattaId.Value))).ToListAsync(cancellationToken).ConfigureAwait(false);
        _dbContext.Documents.RemoveRange(documents);

        // Announcements
        var announcements = await _dbContext.Announcements.IgnoreQueryFilters().Where(a => a.ClubId == clubId || (a.RegattaId.HasValue && regattaIds.Contains(a.RegattaId.Value))).ToListAsync(cancellationToken).ConfigureAwait(false);
        _dbContext.Announcements.RemoveRange(announcements);

        // Scores (via races)
        var raceIds = await _dbContext.Races.Where(r => r.ClubId == clubId).Select(r => r.Id).ToListAsync(cancellationToken).ConfigureAwait(false);
        var scores = await _dbContext.Scores.Where(s => raceIds.Contains(s.RaceId)).ToListAsync(cancellationToken).ConfigureAwait(false);
        _dbContext.Scores.RemoveRange(scores);

        // SeriesRace links
        var seriesRaces = await _dbContext.SeriesRaces.Where(sr => seriesIds.Contains(sr.SeriesId)).ToListAsync(cancellationToken).ConfigureAwait(false);
        _dbContext.SeriesRaces.RemoveRange(seriesRaces);

        // Races
        var races = await _dbContext.Races.Where(r => r.ClubId == clubId).Include(r => r.Weather).ToListAsync(cancellationToken).ConfigureAwait(false);
        _dbContext.Races.RemoveRange(races);

        // RegattaSeries and RegattaFleet links
        var regattaSeries = await _dbContext.RegattaSeries.Where(rs => regattaIds.Contains(rs.RegattaId)).ToListAsync(cancellationToken).ConfigureAwait(false);
        _dbContext.RegattaSeries.RemoveRange(regattaSeries);
        var regattaFleets = await _dbContext.RegattaFleets.Where(rf => regattaIds.Contains(rf.RegattaId)).ToListAsync(cancellationToken).ConfigureAwait(false);
        _dbContext.RegattaFleets.RemoveRange(regattaFleets);

        // Regattas
        var regattas = await _dbContext.Regattas.Where(r => r.ClubId == clubId).ToListAsync(cancellationToken).ConfigureAwait(false);
        _dbContext.Regattas.RemoveRange(regattas);

        // SeriesToSeriesLinks
        var seriesToSeriesLinks = await _dbContext.SeriesToSeriesLinks
            .Where(l => seriesIds.Contains(l.ParentSeriesId) || seriesIds.Contains(l.ChildSeriesId))
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        _dbContext.SeriesToSeriesLinks.RemoveRange(seriesToSeriesLinks);

        // Series
        var series = await _dbContext.Series.Where(s => s.ClubId == clubId).ToListAsync(cancellationToken).ConfigureAwait(false);
        _dbContext.Series.RemoveRange(series);

        // CompetitorFleet links
        var competitorIds2 = await _dbContext.Competitors.Where(c => c.ClubId == clubId).Select(c => c.Id).ToListAsync(cancellationToken).ConfigureAwait(false);
        var competitorFleets = await _dbContext.CompetitorFleets.Where(cf => competitorIds2.Contains(cf.CompetitorId)).ToListAsync(cancellationToken).ConfigureAwait(false);
        _dbContext.CompetitorFleets.RemoveRange(competitorFleets);

        // Competitors
        var competitors = await _dbContext.Competitors.Where(c => c.ClubId == clubId).ToListAsync(cancellationToken).ConfigureAwait(false);
        _dbContext.Competitors.RemoveRange(competitors);

        // FleetBoatClass links
        var fleetIds = await _dbContext.Fleets.Where(f => f.ClubId == clubId).Select(f => f.Id).ToListAsync(cancellationToken).ConfigureAwait(false);
        var fleetBoatClasses = await _dbContext.FleetBoatClasses.Where(fbc => fleetIds.Contains(fbc.FleetId)).ToListAsync(cancellationToken).ConfigureAwait(false);
        _dbContext.FleetBoatClasses.RemoveRange(fleetBoatClasses);

        // Fleets
        var fleets = await _dbContext.Fleets.Where(f => f.ClubId == clubId).ToListAsync(cancellationToken).ConfigureAwait(false);
        _dbContext.Fleets.RemoveRange(fleets);

        // ScoreCodes (via scoring systems)
        var scoringSystemIds = await _dbContext.ScoringSystems.Where(ss => ss.ClubId == clubId).Select(ss => ss.Id).ToListAsync(cancellationToken).ConfigureAwait(false);
        var scoreCodes = await _dbContext.ScoreCodes.Where(sc => scoringSystemIds.Contains(sc.ScoringSystemId)).ToListAsync(cancellationToken).ConfigureAwait(false);
        _dbContext.ScoreCodes.RemoveRange(scoreCodes);

        // ScoringSystems
        var scoringSystems = await _dbContext.ScoringSystems.Where(ss => ss.ClubId == clubId).ToListAsync(cancellationToken).ConfigureAwait(false);
        _dbContext.ScoringSystems.RemoveRange(scoringSystems);

        // Seasons
        var seasons = await _dbContext.Seasons.Where(s => s.ClubId == clubId).ToListAsync(cancellationToken).ConfigureAwait(false);
        _dbContext.Seasons.RemoveRange(seasons);

        // BoatClasses
        var boatClasses = await _dbContext.BoatClasses.Where(bc => bc.ClubId == clubId).ToListAsync(cancellationToken).ConfigureAwait(false);
        _dbContext.BoatClasses.RemoveRange(boatClasses);

        // Club Sequences
        var clubSequences = await _dbContext.ClubSequences.Where(cs => cs.ClubId == clubId).ToListAsync(cancellationToken).ConfigureAwait(false);
        _dbContext.ClubSequences.RemoveRange(clubSequences);

        // Note: Files (logo) are not deleted since they might be referenced elsewhere
        // and club.LogoFileId will be cleared/updated separately

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
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
