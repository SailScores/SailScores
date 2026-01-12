using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SailScores.Core.Services;
using SailScores.Database;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Testcontainers.MsSql;
using Xunit;
using Xunit.Abstractions;
using Db = SailScores.Database.Entities;

namespace SailScores.Test.Integration.Services;

/// <summary>
/// Integration tests for BackupService using real SQL Server in Docker container.
/// Can optionally restore production .bacpac file for testing with real data.
/// </summary>
public class BackupServiceIntegrationTests : IAsyncLifetime
{
    private readonly ITestOutputHelper _output;
    private MsSqlContainer _dbContainer;
    private SailScoresContext _context;
    private BackupService _backupService;
    private Guid _testClubId;
    private bool _useProductionData;
    private string _bacpacPath;

    public BackupServiceIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
        
        // Check for configuration to use production data
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.IntegrationTests.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        _useProductionData = config.GetValue<bool>("IntegrationTests:UseProductionData");
        _bacpacPath = config.GetValue<string>("IntegrationTests:BacpacPath");

        if (_useProductionData && !string.IsNullOrEmpty(_bacpacPath) && !File.Exists(_bacpacPath))
        {
            _output.WriteLine($"WARNING: UseProductionData=true but bacpac file not found at: {_bacpacPath}");
            _useProductionData = false;
        }
    }

    public async Task InitializeAsync()
    {
        _output.WriteLine("Starting SQL Server container...");

        // Create and start SQL Server container with increased resources
        _dbContainer = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithPassword("IntegrationTest123!@#")
            .WithEnvironment("ACCEPT_EULA", "Y")
            .WithEnvironment("MSSQL_PID", "Developer")
            .WithEnvironment("MSSQL_MEMORY_LIMIT_MB", "2048")
            .Build();

        await _dbContainer.StartAsync();
        _output.WriteLine($"Container started. Connection string: {MaskPassword(_dbContainer.GetConnectionString())}");

        if (_useProductionData && !string.IsNullOrEmpty(_bacpacPath))
        {
            // Restore production bacpac
            await RestoreProductionBacpacAsync();
        }
        else
        {
            // Create fresh database with migrations and seed test data
            await CreateFreshDatabaseAsync();
        }

        _backupService = new BackupService(_context);
    }

    public async Task DisposeAsync()
    {
        _context?.Dispose();
        if (_dbContainer != null)
        {
            _output.WriteLine("Stopping SQL Server container...");
            await _dbContainer.DisposeAsync();
        }
    }

    #region Test Setup Methods

    private async Task CreateFreshDatabaseAsync()
    {
        _output.WriteLine("Creating fresh database with migrations...");

        var options = new DbContextOptionsBuilder<SailScoresContext>()
            .UseSqlServer(_dbContainer.GetConnectionString())
            .EnableSensitiveDataLogging()
            .Options;

        _context = new SailScoresContext(options);

        // Apply all migrations
        await _context.Database.MigrateAsync();
        _output.WriteLine("Migrations applied successfully");

        // Seed comprehensive test data
        await SeedTestDataAsync();
        _output.WriteLine("Test data seeded successfully");
    }

    private async Task RestoreProductionBacpacAsync()
    {
        _output.WriteLine($"Restoring production bacpac from: {_bacpacPath}");

        try
        {
            // Create database first
            var masterConnectionString = _dbContainer.GetConnectionString().Replace("Database=master", "Database=master");
            await using (var masterConnection = new SqlConnection(masterConnectionString))
            {
                await masterConnection.OpenAsync();
                var createDbCommand = masterConnection.CreateCommand();
                createDbCommand.CommandText = "IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'sailscores') CREATE DATABASE sailscores";
                await createDbCommand.ExecuteNonQueryAsync();
            }

            // Use SqlPackage to import bacpac
            var sqlPackagePath = FindSqlPackageExecutable();
            if (string.IsNullOrEmpty(sqlPackagePath))
            {
                _output.WriteLine("WARNING: SqlPackage.exe not found. Falling back to fresh database.");
                await CreateFreshDatabaseAsync();
                return;
            }

            var connectionString = _dbContainer.GetConnectionString();
            var importCommand = $"/Action:Import /SourceFile:\"{_bacpacPath}\" /TargetConnectionString:\"{connectionString}\"";
            
            var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = sqlPackagePath,
                Arguments = importCommand,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                _output.WriteLine($"SqlPackage failed with exit code {process.ExitCode}");
                _output.WriteLine($"Error: {error}");
                throw new InvalidOperationException($"Failed to restore bacpac: {error}");
            }

            _output.WriteLine("Bacpac restored successfully");
            _output.WriteLine(output);

            // Create context with restored database
            var options = new DbContextOptionsBuilder<SailScoresContext>()
                .UseSqlServer(connectionString)
                .EnableSensitiveDataLogging()
                .Options;

            _context = new SailScoresContext(options);

            // Get first club for testing
            _testClubId = await _context.Clubs.Select(c => c.Id).FirstAsync();
            _output.WriteLine($"Using production club ID: {_testClubId}");
        }
        catch (Exception ex)
        {
            _output.WriteLine($"Failed to restore bacpac: {ex.Message}");
            _output.WriteLine("Falling back to fresh database with test data");
            await CreateFreshDatabaseAsync();
        }
    }

    private string FindSqlPackageExecutable()
    {
        // Common locations for SqlPackage.exe
        var searchPaths = new[]
        {
            @"C:\Program Files\Microsoft SQL Server\160\DAC\bin\SqlPackage.exe",
            @"C:\Program Files\Microsoft SQL Server\150\DAC\bin\SqlPackage.exe",
            @"C:\Program Files (x86)\Microsoft SQL Server\160\DAC\bin\SqlPackage.exe",
            @"C:\Program Files (x86)\Microsoft SQL Server\150\DAC\bin\SqlPackage.exe",
        };

        foreach (var path in searchPaths)
        {
            if (File.Exists(path))
            {
                _output.WriteLine($"Found SqlPackage at: {path}");
                return path;
            }
        }

        // Try to find via PATH
        var pathVar = Environment.GetEnvironmentVariable("PATH");
        if (pathVar != null)
        {
            var paths = pathVar.Split(Path.PathSeparator);
            foreach (var path in paths)
            {
                var fullPath = Path.Combine(path, "SqlPackage.exe");
                if (File.Exists(fullPath))
                {
                    _output.WriteLine($"Found SqlPackage in PATH: {fullPath}");
                    return fullPath;
                }
            }
        }

        _output.WriteLine("SqlPackage.exe not found in common locations");
        return null;
    }

    private async Task SeedTestDataAsync()
    {
        // Create a test club with comprehensive data
        var club = new Db.Club
        {
            Id = Guid.NewGuid(),
            Name = "Integration Test Sailing Club",
            Initials = "ITSC",
            IsHidden = false,
            Description = "Test club for integration testing",
            Locale = "en-US",
            DefaultScoringSystemId = null
        };
        _context.Clubs.Add(club);
        _testClubId = club.Id;

        // Add boat classes
        var laser = new Db.BoatClass { Id = Guid.NewGuid(), ClubId = club.Id, Name = "Laser", Description = "Standard Laser dinghy" };
        var sunfish = new Db.BoatClass { Id = Guid.NewGuid(), ClubId = club.Id, Name = "Sunfish", Description = "Sunfish sailboat" };
        _context.BoatClasses.AddRange(laser, sunfish);

        // Add seasons
        var season2024 = new Db.Season
        {
            Id = Guid.NewGuid(),
            ClubId = club.Id,
            Name = "2024",
            UrlName = "2024",
            Start = new DateTime(2024, 1, 1),
            End = new DateTime(2024, 12, 31)
        };
        _context.Seasons.Add(season2024);

        // Add scoring system with hierarchy
        var parentSystem = new Db.ScoringSystem
        {
            Id = Guid.NewGuid(),
            ClubId = club.Id,
            Name = "RRS Appendix A",
            DiscardPattern = "0,1,1,1",
            IsSiteDefault = false
        };
        _context.ScoringSystems.Add(parentSystem);

        // Add score codes to parent system
        var dnsCode = new Db.ScoreCode
        {
            Id = Guid.NewGuid(),
            ScoringSystemId = parentSystem.Id,
            Name = "DNS",
            Description = "Did not start",
            Formula = "RacesScored + 1",
            Started = false,
            Finished = false,
            Discardable = true
        };
        _context.ScoreCodes.Add(dnsCode);

        var childSystem = new Db.ScoringSystem
        {
            Id = Guid.NewGuid(),
            ClubId = club.Id,
            Name = "Summer Series System",
            DiscardPattern = "0,0,1",
            ParentSystemId = parentSystem.Id,
            IsSiteDefault = false
        };
        _context.ScoringSystems.Add(childSystem);

        // Add fleet
        var fleet = new Db.Fleet
        {
            Id = Guid.NewGuid(),
            ClubId = club.Id,
            Name = "Thursday Night Fleet",
            ShortName = "Thursday",
            NickName = "TNF",
            IsHidden = false,
            IsActive = true,
            FleetType = Api.Enumerations.FleetType.SelectedBoats
        };
        _context.Fleets.Add(fleet);

        // Associate boat classes with fleet
        _context.FleetBoatClasses.Add(new Db.FleetBoatClass { FleetId = fleet.Id, BoatClassId = laser.Id });
        _context.FleetBoatClasses.Add(new Db.FleetBoatClass { FleetId = fleet.Id, BoatClassId = sunfish.Id });

        // Add competitors
        var competitor1 = new Db.Competitor
        {
            Id = Guid.NewGuid(),
            ClubId = club.Id,
            Name = "John Sailor",
            SailNumber = "12345",
            BoatName = "Fast Wind",
            BoatClassId = laser.Id,
            IsActive = true,
            Created = DateTime.UtcNow
        };
        var competitor2 = new Db.Competitor
        {
            Id = Guid.NewGuid(),
            ClubId = club.Id,
            Name = "Jane Racer",
            SailNumber = "67890",
            AlternativeSailNumber = "ALT-001",
            BoatName = "Sea Breeze",
            BoatClassId = sunfish.Id,
            IsActive = true,
            Created = DateTime.UtcNow
        };
        _context.Competitors.AddRange(competitor1, competitor2);

        // Associate competitors with fleet
        _context.CompetitorFleets.Add(new Db.CompetitorFleet { CompetitorId = competitor1.Id, FleetId = fleet.Id });
        _context.CompetitorFleets.Add(new Db.CompetitorFleet { CompetitorId = competitor2.Id, FleetId = fleet.Id });

        // Add series
        var series = new Db.Series
        {
            Id = Guid.NewGuid(),
            ClubId = club.Id,
            Name = "Summer Series",
            UrlName = "summer",
            Description = "Thursday night summer racing",
            Season = season2024,
            ScoringSystemId = childSystem.Id,
            FleetId = fleet.Id,
            IsImportantSeries = true,
            Type = Db.SeriesType.Standard
        };
        _context.Series.Add(series);

        // Add races with scores
        var race1 = new Db.Race
        {
            Id = Guid.NewGuid(),
            ClubId = club.Id,
            Name = "Race 1",
            Date = new DateTime(2024, 6, 6, 18, 0, 0),
            Fleet = fleet,
            State = Api.Enumerations.RaceState.Raced,
            Order = 1,
            Weather = new Db.Weather
            {
                Id = Guid.NewGuid(),
                Description = "Clear skies",
                WindSpeedMeterPerSecond = 5.5m,
                WindDirectionDegrees = 180m,
                TemperatureDegreesKelvin = 293m,
                CreatedDate = DateTime.UtcNow
            }
        };
        _context.Races.Add(race1);

        // Add scores
        _context.Scores.Add(new Db.Score
        {
            Id = Guid.NewGuid(),
            RaceId = race1.Id,
            CompetitorId = competitor1.Id,
            Place = 1
        });
        _context.Scores.Add(new Db.Score
        {
            Id = Guid.NewGuid(),
            RaceId = race1.Id,
            CompetitorId = competitor2.Id,
            Place = 2
        });

        // Associate race with series
        _context.SeriesRaces.Add(new Db.SeriesRace { SeriesId = series.Id, RaceId = race1.Id });

        // Add regatta
        var regatta = new Db.Regatta
        {
            Id = Guid.NewGuid(),
            ClubId = club.Id,
            Name = "Summer Championship",
            UrlName = "summer-champs",
            Description = "Annual summer championship regatta",
            Season = season2024,
            StartDate = new DateTime(2024, 8, 10),
            EndDate = new DateTime(2024, 8, 11),
            ScoringSystemId = parentSystem.Id
        };
        _context.Regattas.Add(regatta);
        _context.RegattaSeries.Add(new Db.RegattaSeries { RegattaId = regatta.Id, SeriesId = series.Id });
        _context.RegattaFleets.Add(new Db.RegattaFleet { RegattaId = regatta.Id, FleetId = fleet.Id });

        // Add announcement
        var announcement = new Db.Announcement
        {
            Id = Guid.NewGuid(),
            ClubId = club.Id,
            Content = "Welcome to the 2024 sailing season!",
            CreatedDate = DateTime.UtcNow,
            CreatedLocalDate = DateTime.Now,
            CreatedBy = "admin",
            IsDeleted = false
        };
        _context.Announcements.Add(announcement);

        // Add club sequence
        var sequence = new Db.ClubSequence
        {
            Id = Guid.NewGuid(),
            ClubId = club.Id,
            SequenceType = "Competitor",
            NextValue = 100,
            SequencePrefix = "C-"
        };
        _context.ClubSequences.Add(sequence);

        // Save all entities in one transaction
        await _context.SaveChangesAsync();
    }

    private string MaskPassword(string connectionString)
    {
        return System.Text.RegularExpressions.Regex.Replace(
            connectionString, 
            @"(Password|Pwd)=([^;]+)", 
            "$1=****");
    }

    #endregion

    #region Integration Tests

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Category", "Database")]
    [Trait("Category", "Slow")]
    public async Task CreateBackup_WithRealDatabase_ProducesValidBackup()
    {
        // Arrange
        var clubName = await _context.Clubs
            .Where(c => c.Id == _testClubId)
            .Select(c => c.Name)
            .FirstAsync();
        
        _output.WriteLine($"Testing backup for club: {clubName}");

        // Act
        var backup = await _backupService.CreateBackupAsync(_testClubId, "integration-test");

        // Assert
        Assert.NotNull(backup);
        Assert.NotNull(backup.Metadata);
        Assert.Equal(_testClubId, backup.Metadata.SourceClubId);
        Assert.Equal("integration-test", backup.Metadata.CreatedBy);
        
        _output.WriteLine($"Backup created with {backup.BoatClasses?.Count ?? 0} boat classes");
        _output.WriteLine($"Backup created with {backup.Competitors?.Count ?? 0} competitors");
        _output.WriteLine($"Backup created with {backup.Races?.Count ?? 0} races");
        _output.WriteLine($"Backup created with {backup.Series?.Count ?? 0} series");

        // Verify all collections are present
        Assert.NotNull(backup.BoatClasses);
        Assert.NotNull(backup.Seasons);
        Assert.NotNull(backup.Fleets);
        Assert.NotNull(backup.Competitors);
        Assert.NotNull(backup.ScoringSystems);
        Assert.NotNull(backup.Series);
        Assert.NotNull(backup.Races);
        Assert.NotNull(backup.Regattas);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Category", "Database")]
    [Trait("Category", "Slow")]
    public async Task BackupAndRestore_FullRoundTrip_WithRealDatabase_PreservesAllData()
    {
        // Arrange
        var originalCompetitorCount = await _context.Competitors
            .CountAsync(c => c.ClubId == _testClubId);
        var originalRaceCount = await _context.Races
            .CountAsync(r => r.ClubId == _testClubId);
        var originalScoreCount = await _context.Scores
            .CountAsync(s => _context.Races.Any(r => r.Id == s.RaceId && r.ClubId == _testClubId));

        _output.WriteLine($"Original data - Competitors: {originalCompetitorCount}, Races: {originalRaceCount}, Scores: {originalScoreCount}");

        // Act - Create backup
        var backup = await _backupService.CreateBackupAsync(_testClubId, "integration-test");
        _output.WriteLine("Backup created");

        // Act - Restore backup
        var restoreResult = await _backupService.RestoreBackupAsync(_testClubId, backup, preserveClubSettings: true);
        _output.WriteLine("Backup restored");

        // Assert
        Assert.True(restoreResult);

        // Verify counts match
        var restoredCompetitorCount = await _context.Competitors
            .CountAsync(c => c.ClubId == _testClubId);
        var restoredRaceCount = await _context.Races
            .CountAsync(r => r.ClubId == _testClubId);
        var restoredScoreCount = await _context.Scores
            .CountAsync(s => _context.Races.Any(r => r.Id == s.RaceId && r.ClubId == _testClubId));

        _output.WriteLine($"Restored data - Competitors: {restoredCompetitorCount}, Races: {restoredRaceCount}, Scores: {restoredScoreCount}");

        Assert.Equal(originalCompetitorCount, restoredCompetitorCount);
        Assert.Equal(originalRaceCount, restoredRaceCount);
        Assert.Equal(originalScoreCount, restoredScoreCount);
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Category", "Database")]
    [Trait("Category", "Slow")]
    public async Task BackupAndRestore_WithComplexRelationships_MaintainsIntegrity()
    {
        // Arrange - Get fleet with boat classes and competitors
        var originalFleet = await _context.Fleets
            .Include(f => f.FleetBoatClasses)
            .Include(f => f.CompetitorFleets)
            .AsSplitQuery()
            .FirstOrDefaultAsync(f => f.ClubId == _testClubId);

        if (originalFleet != null)
        {
            var originalBoatClassCount = originalFleet.FleetBoatClasses.Count;
            var originalCompetitorCount = originalFleet.CompetitorFleets.Count;

            _output.WriteLine($"Fleet '{originalFleet.Name}' has {originalBoatClassCount} boat classes and {originalCompetitorCount} competitors");

            // Act
            var backup = await _backupService.CreateBackupAsync(_testClubId, "integration-test");
            await _backupService.RestoreBackupAsync(_testClubId, backup, preserveClubSettings: true);

            // Assert - Verify relationships restored
            var restoredFleet = await _context.Fleets
                .Include(f => f.FleetBoatClasses)
                .Include(f => f.CompetitorFleets)
                .AsSplitQuery()
                .FirstOrDefaultAsync(f => f.ClubId == _testClubId && f.Name == originalFleet.Name);

            Assert.NotNull(restoredFleet);
            Assert.Equal(originalBoatClassCount, restoredFleet.FleetBoatClasses.Count);
            Assert.Equal(originalCompetitorCount, restoredFleet.CompetitorFleets.Count);

            _output.WriteLine($"Restored fleet has {restoredFleet.FleetBoatClasses.Count} boat classes and {restoredFleet.CompetitorFleets.Count} competitors");
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Category", "Database")]
    [Trait("Category", "Slow")]
    public async Task BackupAndRestore_WithScoringSystemHierarchy_PreservesParentChildLinks()
    {
        // Arrange - Find scoring systems with parent/child relationships
        var childSystem = await _context.ScoringSystems
            .FirstOrDefaultAsync(ss => ss.ClubId == _testClubId && ss.ParentSystemId.HasValue);

        if (childSystem != null)
        {
            var parentSystemId = childSystem.ParentSystemId.Value;
            _output.WriteLine($"Found child system '{childSystem.Name}' with parent ID: {parentSystemId}");

            // Act
            var backup = await _backupService.CreateBackupAsync(_testClubId, "integration-test");
            await _backupService.RestoreBackupAsync(_testClubId, backup, preserveClubSettings: true);

            // Assert - Verify parent/child relationship preserved
            var restoredChild = await _context.ScoringSystems
                .FirstOrDefaultAsync(ss => ss.ClubId == _testClubId && ss.Name == childSystem.Name);

            Assert.NotNull(restoredChild);
            Assert.NotNull(restoredChild.ParentSystemId);

            var restoredParent = await _context.ScoringSystems
                .FirstOrDefaultAsync(ss => ss.Id == restoredChild.ParentSystemId.Value);

            Assert.NotNull(restoredParent);
            _output.WriteLine($"Restored child system '{restoredChild.Name}' still has parent '{restoredParent.Name}'");
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Category", "Database")]
    [Trait("Category", "Slow")]
    public async Task BackupAndRestore_WithRaceScoresAndWeather_PreservesAllDetails()
    {
        // Arrange
        var originalRace = await _context.Races
            .Include(r => r.Scores)
            .Include(r => r.Weather)
            .FirstOrDefaultAsync(r => r.ClubId == _testClubId);

        if (originalRace != null)
        {
            var hasWeather = originalRace.Weather != null;
            var scoreCount = originalRace.Scores?.Count ?? 0;

            _output.WriteLine($"Race '{originalRace.Name}' has {scoreCount} scores and {(hasWeather ? "has" : "no")} weather data");

            // Act
            var backup = await _backupService.CreateBackupAsync(_testClubId, "integration-test");
            await _backupService.RestoreBackupAsync(_testClubId, backup, preserveClubSettings: true);

            // Assert
            var restoredRace = await _context.Races
                .Include(r => r.Scores)
                .Include(r => r.Weather)
                .FirstOrDefaultAsync(r => r.ClubId == _testClubId && r.Name == originalRace.Name);

            Assert.NotNull(restoredRace);
            Assert.Equal(scoreCount, restoredRace.Scores.Count);

            if (hasWeather)
            {
                Assert.NotNull(restoredRace.Weather);
                _output.WriteLine("Weather data preserved successfully");
            }

            _output.WriteLine($"Restored race has {restoredRace.Scores.Count} scores");
        }
    }

    [Fact(Skip = "Only run when testing with production data")]
    [Trait("Category", "Integration")]
    [Trait("Category", "Database")]
    [Trait("Category", "ProductionData")]
    public async Task BackupAndRestore_WithProductionData_CompletesSuccessfully()
    {
        // This test is skipped by default
        // Remove [Skip] attribute and set UseProductionData=true in appsettings.IntegrationTests.json
        // to run this test with real production data

        if (!_useProductionData)
        {
            _output.WriteLine("Skipping - UseProductionData is false");
            return;
        }

        // Arrange
        var clubCount = await _context.Clubs.CountAsync();
        _output.WriteLine($"Database has {clubCount} clubs");

        var club = await _context.Clubs
            .Include(c => c.WeatherSettings)
            .FirstOrDefaultAsync(c => c.Id == _testClubId);

        _output.WriteLine($"Testing club: {club.Name} ({club.Initials})");

        var originalCounts = new
        {
            Competitors = await _context.Competitors.CountAsync(c => c.ClubId == _testClubId),
            Races = await _context.Races.CountAsync(r => r.ClubId == _testClubId),
            Series = await _context.Series.CountAsync(s => s.ClubId == _testClubId),
            Regattas = await _context.Regattas.CountAsync(r => r.ClubId == _testClubId)
        };

        _output.WriteLine($"Original counts - Competitors: {originalCounts.Competitors}, Races: {originalCounts.Races}, Series: {originalCounts.Series}, Regattas: {originalCounts.Regattas}");

        // Act
        var backup = await _backupService.CreateBackupAsync(_testClubId, "production-test");
        _output.WriteLine("Production backup created successfully");

        await _backupService.RestoreBackupAsync(_testClubId, backup, preserveClubSettings: true);
        _output.WriteLine("Production backup restored successfully");

        // Assert
        var restoredCounts = new
        {
            Competitors = await _context.Competitors.CountAsync(c => c.ClubId == _testClubId),
            Races = await _context.Races.CountAsync(r => r.ClubId == _testClubId),
            Series = await _context.Series.CountAsync(s => s.ClubId == _testClubId),
            Regattas = await _context.Regattas.CountAsync(r => r.ClubId == _testClubId)
        };

        _output.WriteLine($"Restored counts - Competitors: {restoredCounts.Competitors}, Races: {restoredCounts.Races}, Series: {restoredCounts.Series}, Regattas: {restoredCounts.Regattas}");

        Assert.Equal(originalCounts.Competitors, restoredCounts.Competitors);
        Assert.Equal(originalCounts.Races, restoredCounts.Races);
        Assert.Equal(originalCounts.Series, restoredCounts.Series);
        Assert.Equal(originalCounts.Regattas, restoredCounts.Regattas);
    }

    #endregion
}
