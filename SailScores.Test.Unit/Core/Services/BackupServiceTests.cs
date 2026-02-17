using Xunit;
using SailScores.Core.Services;
using SailScores.Database;
using SailScores.Test.Unit.Utilities;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SailScores.Core.Model.BackupEntities;
using Db = SailScores.Database.Entities;
using System.Collections.Generic;
using System.Reflection;

namespace SailScores.Test.Unit.Core.Services;

public class BackupServiceTests
{
    private readonly BackupService _service;
    private readonly ISailScoresContext _context;
    private readonly Guid _clubId;

    public BackupServiceTests()
    {
        _context = InMemoryContextBuilder.GetContext();
        _clubId = _context.Clubs.First().Id;
        _service = new BackupService(_context, NullLogger<BackupService>.Instance);
    }

    #region Basic Backup Tests

    [Fact]
    public async Task CreateBackupAsync_WithValidClub_ReturnsBackupData()
    {
        // Act
        var backup = await _service.CreateBackupAsync(_clubId, "testuser");

        // Assert
        Assert.NotNull(backup);
        Assert.NotNull(backup.Metadata);
        Assert.Equal(_clubId, backup.Metadata.SourceClubId);
        Assert.Equal("testuser", backup.Metadata.CreatedBy);
        Assert.Equal(ClubBackupMetadata.CurrentVersion, backup.Metadata.Version);
    }

    [Fact]
    public async Task CreateBackupAsync_WithInvalidClubId_ThrowsException()
    {
        // Arrange
        var invalidClubId = Guid.NewGuid();

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateBackupAsync(invalidClubId, "testuser"));
    }

    [Fact]
    public async Task CreateBackupAsync_WithClubData_BacksUpAllEntities()
    {
        // Act
        var backup = await _service.CreateBackupAsync(_clubId, "testuser");

        // Assert - Verify all collections are populated or at least initialized
        Assert.NotNull(backup.BoatClasses);
        Assert.NotNull(backup.Seasons);
        Assert.NotNull(backup.Fleets);
        Assert.NotNull(backup.Competitors);
        Assert.NotNull(backup.ScoringSystems);
        Assert.NotNull(backup.Series);
        Assert.NotNull(backup.Races);
        Assert.NotNull(backup.Regattas);
        Assert.NotNull(backup.Announcements);
        Assert.NotNull(backup.Documents);
        Assert.NotNull(backup.ClubSequences);
        Assert.NotNull(backup.CompetitorForwarders);
        Assert.NotNull(backup.RegattaForwarders);
        Assert.NotNull(backup.SeriesForwarders);
    }

    [Fact]
    public async Task CreateBackupAsync_WithScoringSystemHierarchy_PreservesParentSystemId()
    {
        // Arrange - Create a scoring system with a parent
        var parentSystem = new Db.ScoringSystem
        {
            Id = Guid.NewGuid(),
            ClubId = _clubId,
            Name = "Parent System",
            DiscardPattern = "0"
        };
        _context.ScoringSystems.Add(parentSystem);

        var childSystem = new Db.ScoringSystem
        {
            Id = Guid.NewGuid(),
            ClubId = _clubId,
            Name = "Child System",
            DiscardPattern = "0",
            ParentSystemId = parentSystem.Id
        };
        _context.ScoringSystems.Add(childSystem);
        await _context.SaveChangesAsync();

        // Act
        var backup = await _service.CreateBackupAsync(_clubId, "testuser");

        // Assert
        var backedUpChild = backup.ScoringSystems.FirstOrDefault(s => s.Name == "Child System");
        Assert.NotNull(backedUpChild);
        Assert.Equal(parentSystem.Id, backedUpChild.ParentSystemId);
    }

    #endregion

    #region Validation Tests

    [Fact]
    public void ValidateBackup_WithNullBackup_ReturnsInvalid()
    {
        // Act
        var result = _service.ValidateBackup(null);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("null", result.ErrorMessage);
    }

    [Fact]
    public void ValidateBackup_WithNullMetadata_ReturnsInvalid()
    {
        // Arrange
        var backup = new ClubBackupData { Metadata = null };

        // Act
        var result = _service.ValidateBackup(backup);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("metadata", result.ErrorMessage.ToLower());
    }

    [Fact]
    public void ValidateBackup_WithNewerVersion_ReturnsInvalid()
    {
        // Arrange
        var backup = new ClubBackupData
        {
            Metadata = new ClubBackupMetadata
            {
                Version = ClubBackupMetadata.CurrentVersion + 1,
                SourceClubName = "Test Club",
                CreatedDateUtc = DateTime.UtcNow
            }
        };

        // Act
        var result = _service.ValidateBackup(backup);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("newer", result.ErrorMessage);
    }

    [Fact]
    public void ValidateBackup_WithValidBackup_ReturnsValid()
    {
        // Arrange
        var backup = new ClubBackupData
        {
            Metadata = new ClubBackupMetadata
            {
                Version = ClubBackupMetadata.CurrentVersion,
                SourceClubName = "Test Club",
                CreatedDateUtc = DateTime.UtcNow
            }
        };

        // Act
        var result = _service.ValidateBackup(backup);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(backup.Metadata.SourceClubName, result.SourceClubName);
    }

    #endregion

    #region Restore Tests

    [Fact]
    public async Task RestoreBackupAsync_WithValidBackup_RestoresData()
    {
        // Arrange
        var backup = await _service.CreateBackupAsync(_clubId, "testuser");

        // Act
        var result = await _service.RestoreBackupAsync(_clubId, backup, preserveClubName: true);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task RestoreBackupAsync_WithInvalidBackup_ThrowsException()
    {
        // Arrange
        var invalidBackup = new ClubBackupData { Metadata = null };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.RestoreBackupAsync(_clubId, invalidBackup));
    }

    [Fact]
    public async Task RestoreBackupAsync_PreserveClubName_KeepsClubName()
    {
        // Arrange
        var club = await _context.Clubs.FindAsync(_clubId);
        var originalName = club.Name;
        var originalInitials = club.Initials;
        var backup = await _service.CreateBackupAsync(_clubId, "testuser");
        backup.Name = "Different Name";
        backup.Url = "http://different.url";

        // Act
        await _service.RestoreBackupAsync(_clubId, backup, preserveClubName: true);

        // Assert
        var restoredClub = await _context.Clubs.FindAsync(_clubId);
        Assert.Equal(originalName, restoredClub.Name); // Name is preserved
        Assert.Equal("http://different.url", restoredClub.Url); // URL is always updated
        Assert.Equal(originalInitials, restoredClub.Initials); // Initials are always preserved
    }

    [Fact]
    public async Task RestoreBackupAsync_DontPreserveClubName_UpdatesClubNameButNotUrl()
    {
        // Arrange
        var club = await _context.Clubs.FindAsync(_clubId);
        var originalInitials = club.Initials;
        var originalUrl = club.Url;
        var backup = await _service.CreateBackupAsync(_clubId, "testuser");
        backup.Name = "Different Name";
        backup.Url = "http://different.url";

        // Act
        await _service.RestoreBackupAsync(_clubId, backup, preserveClubName: false);

        // Assert
        var restoredClub = await _context.Clubs.FindAsync(_clubId);
        Assert.Equal("Different Name", restoredClub.Name); // Name is updated
        Assert.Equal("http://different.url", restoredClub.Url); // URL is always updated
        Assert.Equal(originalInitials, restoredClub.Initials); // Initials are always preserved
    }

    [Fact]
    public async Task RestoreBackupAsync_ClearsExistingData_BeforeRestore()
    {
        // Arrange
        var initialCompetitorCount = await _context.Competitors.CountAsync(c => c.ClubId == _clubId);
        var backup = await _service.CreateBackupAsync(_clubId, "testuser");

        // Add a new competitor
        _context.Competitors.Add(new Db.Competitor
        {
            Id = Guid.NewGuid(),
            ClubId = _clubId,
            Name = "Extra Competitor",
            SailNumber = "999",
            BoatClassId = _context.BoatClasses.First(bc => bc.ClubId == _clubId).Id
        });
        await _context.SaveChangesAsync();

        var beforeRestoreCount = await _context.Competitors.CountAsync(c => c.ClubId == _clubId);
        Assert.Equal(initialCompetitorCount + 1, beforeRestoreCount);

        // Act
        await _service.RestoreBackupAsync(_clubId, backup, preserveClubName: true);

        // Assert - Should be back to original count (new GUIDs but same data)
        var afterRestoreCount = await _context.Competitors.CountAsync(c => c.ClubId == _clubId);
        Assert.Equal(initialCompetitorCount, afterRestoreCount);
    }

    #endregion

    #region Round-Trip Tests

    [Fact]
    public async Task BackupAndRestore_RoundTrip_PreservesCompetitorData()
    {
        // Arrange
        var originalCompetitors = await _context.Competitors
            .Where(c => c.ClubId == _clubId)
            .AsNoTracking()
            .ToListAsync();

        // Act - Backup
        var backup = await _service.CreateBackupAsync(_clubId, "testuser");

        // Act - Restore
        await _service.RestoreBackupAsync(_clubId, backup, preserveClubName: true);

        // Assert
        var restoredCompetitors = await _context.Competitors
            .Where(c => c.ClubId == _clubId)
            .ToListAsync();

        Assert.Equal(originalCompetitors.Count, restoredCompetitors.Count);

        // Verify field-by-field (GUIDs will be different)
        foreach (var original in originalCompetitors)
        {
            var restored = restoredCompetitors.FirstOrDefault(c => 
                c.SailNumber == original.SailNumber && 
                c.Name == original.Name);
            Assert.NotNull(restored);
            Assert.Equal(original.BoatName, restored.BoatName);
            Assert.Equal(original.AlternativeSailNumber, restored.AlternativeSailNumber);
            Assert.Equal(original.HomeClubName, restored.HomeClubName);
            Assert.Equal(original.Notes, restored.Notes);
            Assert.Equal(original.IsActive, restored.IsActive);
        }
    }

    [Fact]
    public async Task BackupAndRestore_RoundTrip_PreservesBoatClasses()
    {
        // Arrange
        var originalBoatClasses = await _context.BoatClasses
            .Where(bc => bc.ClubId == _clubId)
            .AsNoTracking()
            .ToListAsync();

        // Act
        var backup = await _service.CreateBackupAsync(_clubId, "testuser");
        await _service.RestoreBackupAsync(_clubId, backup, preserveClubName: true);
        
        // Assert
        var restoredBoatClasses = await _context.BoatClasses
            .Where(bc => bc.ClubId == _clubId)
            .ToListAsync();
        
        Assert.Equal(originalBoatClasses.Count, restoredBoatClasses.Count);
        
        foreach (var original in originalBoatClasses)
        {
            var restored = restoredBoatClasses.FirstOrDefault(bc => bc.Name == original.Name);
            Assert.NotNull(restored);
            Assert.Equal(original.Description, restored.Description);
        }
    }

    [Fact]
    public async Task BackupAndRestore_RoundTrip_PreservesSeasons()
    {
        // Arrange
        var originalSeasons = await _context.Seasons
            .Where(s => s.ClubId == _clubId)
            .AsNoTracking()
            .ToListAsync();

        // Act
        var backup = await _service.CreateBackupAsync(_clubId, "testuser");
        await _service.RestoreBackupAsync(_clubId, backup, preserveClubName: true);

        // Assert
        var restoredSeasons = await _context.Seasons
            .Where(s => s.ClubId == _clubId)
            .ToListAsync();

        Assert.Equal(originalSeasons.Count, restoredSeasons.Count);

        foreach (var original in originalSeasons)
        {
            var restored = restoredSeasons.FirstOrDefault(s => s.Name == original.Name);
            Assert.NotNull(restored);
            Assert.Equal(original.UrlName, restored.UrlName);
            Assert.Equal(original.Start, restored.Start);
            Assert.Equal(original.End, restored.End);
        }
    }

    [Fact]
    public async Task BackupAndRestore_WithScoringSystemHierarchy_PreservesParentReferences()
    {
        // Arrange - Create a scoring system with a parent
        var parentSystem = new Db.ScoringSystem
        {
            Id = Guid.NewGuid(),
            ClubId = _clubId,
            Name = "Test Parent System",
            DiscardPattern = "0"
        };
        _context.ScoringSystems.Add(parentSystem);

        var childSystem = new Db.ScoringSystem
        {
            Id = Guid.NewGuid(),
            ClubId = _clubId,
            Name = "Test Child System",
            DiscardPattern = "0",
            ParentSystemId = parentSystem.Id
        };
        _context.ScoringSystems.Add(childSystem);
        await _context.SaveChangesAsync();

        // Act
        var backup = await _service.CreateBackupAsync(_clubId, "testuser");
        await _service.RestoreBackupAsync(_clubId, backup, preserveClubName: true);

        // Assert
        var restoredParent = await _context.ScoringSystems
            .FirstOrDefaultAsync(s => s.ClubId == _clubId && s.Name == "Test Parent System");
        var restoredChild = await _context.ScoringSystems
            .FirstOrDefaultAsync(s => s.ClubId == _clubId && s.Name == "Test Child System");

        Assert.NotNull(restoredParent);
        Assert.NotNull(restoredChild);
        Assert.Equal(restoredParent.Id, restoredChild.ParentSystemId);
    }

    [Fact]
    public async Task BackupAndRestore_WithFleetBoatClassRelationships_PreservesAssociations()
    {
        // Arrange
        var fleet = await _context.Fleets
            .Include(f => f.FleetBoatClasses)
            .FirstOrDefaultAsync(f => f.ClubId == _clubId);

        if (fleet != null && fleet.FleetBoatClasses?.Any() == true)
        {
            var originalAssociationCount = fleet.FleetBoatClasses.Count;

            // Act
            var backup = await _service.CreateBackupAsync(_clubId, "testuser");
            await _service.RestoreBackupAsync(_clubId, backup, preserveClubName: true);

            // Assert
            var restoredFleet = await _context.Fleets
                .Include(f => f.FleetBoatClasses)
                .FirstOrDefaultAsync(f => f.ClubId == _clubId && f.Name == fleet.Name);

            Assert.NotNull(restoredFleet);
            Assert.Equal(originalAssociationCount, restoredFleet.FleetBoatClasses.Count);
        }
    }

    #endregion

    #region Field Coverage Tests

    [Theory]
    [InlineData(typeof(Db.Competitor), typeof(CompetitorBackup))]
    [InlineData(typeof(Db.Fleet), typeof(FleetBackup))]
    [InlineData(typeof(Db.BoatClass), typeof(BoatClassBackup))]
    [InlineData(typeof(Db.Season), typeof(SeasonBackup))]
    [InlineData(typeof(Db.ScoringSystem), typeof(ScoringSystemBackup))]
    [InlineData(typeof(Db.ScoreCode), typeof(ScoreCodeBackup))]
    [InlineData(typeof(Db.Series), typeof(SeriesBackup))]
    [InlineData(typeof(Db.Race), typeof(RaceBackup))]
    [InlineData(typeof(Db.Score), typeof(ScoreBackup))]
    [InlineData(typeof(Db.Weather), typeof(WeatherBackup))]
    [InlineData(typeof(Db.Regatta), typeof(RegattaBackup))]
    [InlineData(typeof(Db.Announcement), typeof(AnnouncementBackup))]
    [InlineData(typeof(Db.Document), typeof(DocumentBackup))]
    [InlineData(typeof(Db.ClubSequence), typeof(ClubSequenceBackup))]
    [InlineData(typeof(Db.CompetitorForwarder), typeof(CompetitorForwarderBackup))]
    [InlineData(typeof(Db.RegattaForwarder), typeof(RegattaForwarderBackup))]
    [InlineData(typeof(Db.SeriesForwarder), typeof(SeriesForwarderBackup))]
    [InlineData(typeof(Db.File), typeof(FileBackup))]
    [InlineData(typeof(Db.SeriesChartResults), typeof(SeriesChartResultsBackup))]
    [InlineData(typeof(Db.HistoricalResults), typeof(HistoricalResultsBackup))]
    public void BackupEntity_HasAllDatabaseEntityFields(Type dbType, Type backupType)
    {
        // Arrange
        var dbProps = GetSimpleProperties(dbType);
        var backupProps = GetSimpleProperties(backupType);
        
        // Act
        var missingFields = dbProps.Except(backupProps).ToList();
        
        // Assert
        Assert.Empty(missingFields
            .Select(f => $"{dbType.Name}.{f} is missing from {backupType.Name}")
            .ToList());
    }

    [Fact]
    public void AllBackupEntities_MatchDatabaseEntities_FieldCoverage()
    {
        // Arrange
        var entityPairs = new[]
        {
            (typeof(Db.Competitor), typeof(CompetitorBackup), new[] { "ClubId", "BoatClass", "CompetitorFleets", "Scores", "ChangeHistory" }),
            (typeof(Db.Fleet), typeof(FleetBackup), new[] { "ClubId", "FleetBoatClasses", "CompetitorFleets", "IsHidden" }),
            (typeof(Db.BoatClass), typeof(BoatClassBackup), new[] { "ClubId" }),
            (typeof(Db.Season), typeof(SeasonBackup), new[] { "ClubId" }),
            (typeof(Db.ScoringSystem), typeof(ScoringSystemBackup), new[] { "ClubId" }),
            (typeof(Db.ScoreCode), typeof(ScoreCodeBackup), new[] { "ScoringSystemId" }),
            (typeof(Db.Series), typeof(SeriesBackup), new[] { "ClubId", "RaceSeries", "ChildLinks", "ParentLinks", "Season", "ScoringSystem" }),
            (typeof(Db.Race), typeof(RaceBackup), new[] { "ClubId", "Fleet", "Scores", "SeriesRaces", "Weather" }),
            (typeof(Db.Score), typeof(ScoreBackup), new[] { "Competitor", "Race" }),
            (typeof(Db.Weather), typeof(WeatherBackup), new string[0]),
            (typeof(Db.Regatta), typeof(RegattaBackup), new[] { "ClubId", "RegattaSeries", "RegattaFleet", "Season", "ScoringSystem" }),
            (typeof(Db.Announcement), typeof(AnnouncementBackup), new[] { "ClubId", "PreviousVersion", "IsDeleted" }),
            (typeof(Db.Document), typeof(DocumentBackup), new[] { "ClubId", "PreviousVersion" }),
            (typeof(Db.ClubSequence), typeof(ClubSequenceBackup), new[] { "ClubId", "Club" }),
            (typeof(Db.CompetitorForwarder), typeof(CompetitorForwarderBackup), new[] { "NewCompetitor" }),
            (typeof(Db.RegattaForwarder), typeof(RegattaForwarderBackup), new[] { "NewRegatta", "NewRegattaId" }),
            (typeof(Db.SeriesForwarder), typeof(SeriesForwarderBackup), new[] { "NewSeries", "NewSeriesId" }),
            (typeof(Db.File), typeof(FileBackup), new string[0]),
            (typeof(Db.SeriesChartResults), typeof(SeriesChartResultsBackup), new[] { "Series" }),
            (typeof(Db.HistoricalResults), typeof(HistoricalResultsBackup), new[] { "Series" }),
        };
        
        var results = new List<string>();
        
        // Act
        foreach (var (dbType, backupType, excludedFields) in entityPairs)
        {
            var coverage = AnalyzeFieldCoverage(dbType, backupType, excludedFields);
            if (!coverage.IsComplete)
            {
                results.Add($"{dbType.Name}: Missing {string.Join(", ", coverage.MissingFields)}");
            }
        }
        
        // Assert
        Assert.Empty(results);
    }

    #endregion

    #region Helper Methods

    private IEnumerable<string> GetSimpleProperties(Type type)
    {
        return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => IsSimpleType(p.PropertyType))
            .Where(p => !ShouldExcludeProperty(p.Name, type))
            .Select(p => p.Name);
    }

    private bool ShouldExcludeProperty(string propertyName, Type type)
    {
        // Properties that are handled differently in backup entities
        var commonExclusions = new[] 
        { 
            "ClubId",           // Added during restore
            "ScoringSystemId",  // For ScoreCode - it's a foreign key
        };

        // Navigation properties
        var navigationExclusions = new[]
        {
            "Club", "BoatClass", "Fleet", "Season", "ScoringSystem", "Competitor", "Race", "Series", "Regatta",
            "FleetBoatClasses", "CompetitorFleets", "Scores", "RaceSeries", "SeriesRaces", "ChildLinks", "ParentLinks",
            "RegattaSeries", "RegattaFleet", "Weather", "NewCompetitor", "NewRegatta", "NewSeries", "ChangeHistory"
        };

        // Backup-specific exclusions
        var backupSpecificExclusions = new[]
        {
            "IsHidden",         // Fleet: not in model but in database
            "IsDeleted",        // Announcement: filtered during backup
            "PreviousVersion",  // Announcement/Document: version chains not preserved
            "NewRegattaId",     // RegattaForwarder: uses RegattaId in backup
            "NewSeriesId"       // SeriesForwarder: uses SeriesId in backup
        };

        return commonExclusions.Contains(propertyName) 
            || navigationExclusions.Contains(propertyName)
            || backupSpecificExclusions.Contains(propertyName);
    }

    private bool IsSimpleType(Type type)
    {
        var actualType = Nullable.GetUnderlyingType(type) ?? type;
        return actualType.IsPrimitive 
            || actualType.IsEnum 
            || actualType == typeof(string)
            || actualType == typeof(Guid)
            || actualType == typeof(DateTime)
            || actualType == typeof(DateOnly)
            || actualType == typeof(TimeSpan)
            || actualType == typeof(decimal)
            || actualType == typeof(byte[]);
    }

    private (bool IsComplete, List<string> MissingFields) AnalyzeFieldCoverage(
        Type dbType, 
        Type backupType, 
        string[] additionalExclusions)
    {
        var dbProps = dbType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => IsSimpleType(p.PropertyType))
            .Where(p => !ShouldExcludeProperty(p.Name, dbType))
            .Where(p => !additionalExclusions.Contains(p.Name))
            .Select(p => p.Name)
            .ToList();

        var backupProps = backupType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name)
            .ToList();

        var missingFields = dbProps.Except(backupProps).ToList();

        return (missingFields.Count == 0, missingFields);
    }

    #endregion
}
