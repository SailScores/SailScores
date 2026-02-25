using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using SailScores.Core.Services;
using SailScores.Database;
using System;
using System.Collections.Generic;
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
    private static readonly string _logFilePath = Path.Combine(Path.GetTempPath(), "SailScores_IntegrationTest.log");

    private void Log(string message)
    {
        // Always write to file first (most reliable in Test Explorer)
        try
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            File.AppendAllText(_logFilePath, $"[{timestamp}] {message}{Environment.NewLine}");

            // Force immediate flush to disk
            System.GC.Collect();
        }
        catch
        {
            // File write might fail, continue anyway
        }

        // Then try to write to Test Explorer output
        try
        {
            _output.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");
        }
        catch
        {
            // Output helper might fail in Test Explorer, but we have file fallback
        }
    }

    /// <summary>
    /// Emergency logging for when Test Explorer output fails.
    /// Writes directly to console and file without buffering.
    /// </summary>
    private void LogDirect(string message)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var fullMessage = $"[{timestamp}] {message}";

        // Write to file
        try
        {
            File.AppendAllText(_logFilePath, fullMessage + Environment.NewLine);
        }
        catch { }

        // Write to console (visible in Test Explorer output)
        try
        {
            Console.WriteLine(fullMessage);
            Console.Out.Flush();
        }
        catch { }

        // Write to Test Explorer
        try
        {
            _output.WriteLine(fullMessage);
        }
        catch { }
    }

    public BackupServiceIntegrationTests(ITestOutputHelper output)
    {
        _output = output;
        Log("[INIT] Constructor starting...");
        Log($"[INIT] Log file: {_logFilePath}");

        // Check for configuration to use production data
        Log("[INIT] Building configuration...");
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.IntegrationTests.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        _useProductionData = config.GetValue<bool>("IntegrationTests:UseProductionData");
        _bacpacPath = config.GetValue<string>("IntegrationTests:BacpacPath");

        Log($"[INIT] UseProductionData: {_useProductionData}");
        Log($"[INIT] BacpacPath: {(_bacpacPath ?? "NOT SET")}");
        Log($"[INIT] Current directory: {Directory.GetCurrentDirectory()}");

        if (_useProductionData && !string.IsNullOrEmpty(_bacpacPath) && !File.Exists(_bacpacPath))
        {
            Log($"[INIT] WARNING: UseProductionData=true but bacpac file not found at: {_bacpacPath}");
            _useProductionData = false;
        }

        Log("[INIT] Constructor complete");
    }

    public async Task InitializeAsync()
    {
        LogDirect("========== TEST INITIALIZATION STARTING ==========");
        Log("[INIT] InitializeAsync starting...");
        Log("Starting SQL Server container...");

        try
        {
            // Create and start SQL Server container with increased resources
            Log("[INIT] Creating MsSql container...");
            LogDirect(">>> Creating Docker container...");

            _dbContainer = new MsSqlBuilder()
                .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
                .WithPassword("IntegrationTest123!@#")
                .WithEnvironment("ACCEPT_EULA", "Y")
                .WithEnvironment("MSSQL_PID", "Developer")
                .WithEnvironment("MSSQL_MEMORY_LIMIT_MB", "2048")
                .WithEnvironment("MSSQL_ENABLE_HADR", "0")
                .Build();

            Log("[INIT] Starting container...");
            LogDirect(">>> Waiting for Docker container to start (this takes 10-30 seconds)...");
            await _dbContainer.StartAsync();
            LogDirect(">>> Docker container started successfully");
            Log($"Container started. Connection string: {MaskPassword(_dbContainer.GetConnectionString())}");

            Log($"[INIT] _useProductionData={_useProductionData}, _bacpacPath={(_bacpacPath ?? "NULL")}");

            if (_useProductionData && !string.IsNullOrEmpty(_bacpacPath))
            {
                LogDirect(">>> Restoring production bacpac (this may take 5-30+ minutes)...");
                Log("[INIT] About to restore production bacpac...");

                // Restore production bacpac
                await RestoreProductionBacpacAsync();

                LogDirect(">>> Production bacpac restored successfully");
            }
            else
            {
                LogDirect(">>> Creating fresh database with test data...");
                Log("[INIT] Creating fresh database...");

                // Create fresh database with migrations and seed test data
                await CreateFreshDatabaseAsync();

                LogDirect(">>> Fresh database created successfully");
            }

            Log("[INIT] Creating BackupService...");
            _backupService = new BackupService(_context, NullLogger<BackupService>.Instance);

            LogDirect("========== TEST INITIALIZATION COMPLETE ==========");
            Log("[INIT] InitializeAsync complete");
        }
        catch (Exception ex)
        {
            LogDirect($"!!! INITIALIZATION FAILED: {ex.Message}");
            Log($"[INIT] EXCEPTION: {ex}");
            throw;
        }
    }

    public async Task DisposeAsync()
    {
        LogDirect("========== TEST CLEANUP STARTING ==========");
        Log("[DISPOSE] Starting cleanup...");

        try
        {
            _context?.Dispose();
            if (_dbContainer != null)
            {
                LogDirect(">>> Stopping Docker container...");
                Log("Stopping SQL Server container...");

                // Use a timeout for container disposal to prevent hanging
                using (var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(30)))
                {
                    try
                    {
                        await _dbContainer.DisposeAsync().AsTask().ConfigureAwait(false);
                        LogDirect(">>> Docker container stopped");
                    }
                    catch (OperationCanceledException)
                    {
                        LogDirect("!!! Docker container cleanup timeout - forcing cleanup");
                        Log("[DISPOSE] Container cleanup timed out after 30 seconds");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            LogDirect($"!!! ERROR DURING CLEANUP: {ex.Message}");
            Log($"[DISPOSE] EXCEPTION: {ex}");
        }

        LogDirect("========== TEST CLEANUP COMPLETE ==========");
        Log("[DISPOSE] Cleanup complete");
        Log($"[DISPOSE] Full log available at: {_logFilePath}");
        LogDirect($"Log file: {_logFilePath}");
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
        Log($"Restoring production bacpac from: {_bacpacPath}");

        try
        {
            // Create database first
            Log("Creating database...");
            var masterConnectionString = _dbContainer.GetConnectionString().Replace("Database=sailscores", "Database=master");
            await using (var masterConnection = new SqlConnection(masterConnectionString))
            {
                await masterConnection.OpenAsync();
                var createDbCommand = masterConnection.CreateCommand();
                createDbCommand.CommandText = "IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'sailscores') CREATE DATABASE sailscores";
                await createDbCommand.ExecuteNonQueryAsync();
            }
            Log("Database created successfully");

            // Use SqlPackage to import bacpac
            Log("Finding SqlPackage executable...");
            var sqlPackagePath = FindSqlPackageExecutable();
            if (string.IsNullOrEmpty(sqlPackagePath))
            {
                Log("WARNING: SqlPackage.exe not found. Falling back to fresh database.");
                await CreateFreshDatabaseAsync();
                return;
            }

            var connectionString = _dbContainer.GetConnectionString().Replace("Database=master", "Database=sailscores");
            var importCommand = $"/Action:Import /SourceFile:\"{_bacpacPath}\" /TargetConnectionString:\"{connectionString}\" /p:CommandTimeout=600";

            Log($"Starting SqlPackage with timeout of 600 seconds...");
            Log($"Command: {sqlPackagePath} {importCommand}");

            var processInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = sqlPackagePath,
                Arguments = importCommand,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = System.Diagnostics.Process.Start(processInfo))
            {
                if (process == null)
                {
                    throw new InvalidOperationException("Failed to start SqlPackage process");
                }

                Log("SqlPackage process started. Waiting for completion (this may take several minutes for large backups)...");

                // FIX: Read output asynchronously to avoid deadlock
                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();

                // Wait for process to exit with a timeout of 10 minutes
                var completed = await Task.Run(() => process.WaitForExit(600000));

                if (!completed)
                {
                    Log("ERROR: SqlPackage timeout - operation exceeded 10 minutes");
                    process.Kill();
                    throw new TimeoutException("SqlPackage import exceeded 10 minute timeout");
                }

                var output = await outputTask;
                var error = await errorTask;

                Log($"SqlPackage process exited with code: {process.ExitCode}");

                if (!string.IsNullOrEmpty(output))
                {
                    Log("SqlPackage output:");
                    Log(output);
                }

                if (process.ExitCode != 0)
                {
                    Log($"ERROR: SqlPackage failed with exit code {process.ExitCode}");
                    if (!string.IsNullOrEmpty(error))
                    {
                        Log("SqlPackage error:");
                        Log(error);
                    }
                    throw new InvalidOperationException($"Failed to restore bacpac. Exit code: {process.ExitCode}. Error: {error}");
                }

                Log("✓ Bacpac restored successfully");
            }

            // Create context with restored database
            Log("Creating EF Core context for restored database...");
            var options = new DbContextOptionsBuilder<SailScoresContext>()
                .UseSqlServer(connectionString, sqlOptions => sqlOptions.CommandTimeout(600))
                .EnableSensitiveDataLogging()
                .Options;

            _context = new SailScoresContext(options);

            // Verify connection and get club count
            Log("Verifying database connection...");
            var clubCount = await _context.Clubs.CountAsync();
            Log($"✓ Database connection verified. Found {clubCount} clubs");

            // Get first club for testing
            _testClubId = await _context.Clubs.Select(c => c.Id).FirstAsync();
            Log($"✓ Using production club ID: {_testClubId}");
        }
        catch (TimeoutException ex)
        {
            Log($"TIMEOUT: {ex.Message}");
            Log("Falling back to fresh database with test data");
            await CreateFreshDatabaseAsync();
        }
        catch (Exception ex)
        {
            Log($"ERROR: Failed to restore bacpac: {ex.Message}");
            Log($"Exception type: {ex.GetType().Name}");
            Log($"Stack trace: {ex.StackTrace}");
            Log("Falling back to fresh database with test data");
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
                Log($"Found SqlPackage at: {path}");
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
                    Log($"Found SqlPackage in PATH: {fullPath}");
                    return fullPath;
                }
            }
        }

        Log("SqlPackage.exe not found in common locations");
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
        var restoreResult = await _backupService.RestoreBackupAsync(_testClubId, backup, preserveClubName: true);
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
            await _backupService.RestoreBackupAsync(_testClubId, backup, preserveClubName: true);

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
            await _backupService.RestoreBackupAsync(_testClubId, backup, preserveClubName: true);

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
            await _backupService.RestoreBackupAsync(_testClubId, backup, preserveClubName: true);

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

    [Fact] //[Fact(Skip = "Only run when testing with production data")]
    [Trait("Category", "Integration")]
    [Trait("Category", "Database")]
    [Trait("Category", "ProductionData")]
    [Trait("Category", "Slow")]
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

        _output.WriteLine("=== PRODUCTION DATA BACKUP/RESTORE TEST ===");

        // Arrange
        _output.WriteLine("\n[1/5] Finding test club...");
        var testClubId = await _context.Clubs
            .Where(c => c.Initials == "LHYC")
            .Select(c => c.Id)
            .FirstOrDefaultAsync();

        if (testClubId == Guid.Empty)
        {
            _output.WriteLine("SKIP: Club with initials 'LHYC' not found");
            return;
        }

        var clubCount = await _context.Clubs.CountAsync();
        _output.WriteLine($"✓ Found {clubCount} total clubs");

        var club = await _context.Clubs
            .Include(c => c.WeatherSettings)
            .FirstOrDefaultAsync(c => c.Id == testClubId);

        _output.WriteLine($"✓ Testing club: {club.Name} ({club.Initials})");

        _output.WriteLine("\n[2/5] Capturing baseline data...");
        var originalCounts = new
        {
            Competitors = await _context.Competitors.CountAsync(c => c.ClubId == testClubId),
            Races = await _context.Races.CountAsync(r => r.ClubId == testClubId),
            Series = await _context.Series.CountAsync(s => s.ClubId == testClubId),
            Regattas = await _context.Regattas.CountAsync(r => r.ClubId == testClubId)
        };

        _output.WriteLine($"  Competitors: {originalCounts.Competitors}");
        _output.WriteLine($"  Races: {originalCounts.Races}");
        _output.WriteLine($"  Series: {originalCounts.Series}");
        _output.WriteLine($"  Regattas: {originalCounts.Regattas}");

        var originalSeriesUpdateDates = await _context.Series
            .Where(s => s.ClubId == testClubId)
            .Select(s => s.UpdatedDate)
            .OrderBy(d => d)
            .ToListAsync();

        // Act - Create backup
        _output.WriteLine("\n[3/5] Creating backup (this may take a while with large production data)...");
        var backupStopwatch = System.Diagnostics.Stopwatch.StartNew();
        var backup = await _backupService.CreateBackupAsync(testClubId, "production-test");
        backupStopwatch.Stop();
        _output.WriteLine($"✓ Backup created in {backupStopwatch.ElapsedMilliseconds}ms");

        // Find data to modify
        var raceToRemove = await _context.Races
            .Where(r => r.ClubId == testClubId)
            .OrderByDescending(r => r.Date)
            .FirstOrDefaultAsync();
        var seriesToRemove = await _context.Series
            .Where(s => s.ClubId == testClubId)
            .OrderBy(s => s.Name)
            .FirstOrDefaultAsync();

        if (raceToRemove != null && seriesToRemove != null)
        {
            _output.WriteLine($"\n[4/5] Applying test modifications...");
            _output.WriteLine($"  Removing race: {raceToRemove.Name} ({raceToRemove.Date:d})");
            _output.WriteLine($"  Removing series: {seriesToRemove.Name}");

            _context.Races.Remove(raceToRemove);
            _context.SeriesRaces.RemoveRange(_context.SeriesRaces.Where(sr => sr.SeriesId == seriesToRemove.Id));
            _context.Series.Remove(seriesToRemove);
            await _context.SaveChangesAsync();
            _output.WriteLine($"  ✓ Modifications applied");
        }
        else
        {
            _output.WriteLine($"\n[4/5] Skipping modifications (not enough data to safely modify)");
        }

        // Act - Restore backup
        _output.WriteLine("\n[5/5] Restoring from backup (this may take a while)...");
        var restoreStopwatch = System.Diagnostics.Stopwatch.StartNew();
        var restoreSuccess = await _backupService.RestoreBackupAsync(testClubId, backup, preserveClubName: true);
        restoreStopwatch.Stop();
        _output.WriteLine($"✓ Restore completed in {restoreStopwatch.ElapsedMilliseconds}ms");

        // Assert
        Assert.True(restoreSuccess);

        var restoredCounts = new
        {
            Competitors = await _context.Competitors.CountAsync(c => c.ClubId == testClubId),
            Races = await _context.Races.CountAsync(r => r.ClubId == testClubId),
            Series = await _context.Series.CountAsync(s => s.ClubId == testClubId),
            Regattas = await _context.Regattas.CountAsync(r => r.ClubId == testClubId)
        };

        _output.WriteLine("\n=== VERIFICATION ===");
        _output.WriteLine($"Competitors: {originalCounts.Competitors} -> {restoredCounts.Competitors} {(originalCounts.Competitors == restoredCounts.Competitors ? "✓" : "✗")}");
        _output.WriteLine($"Races: {originalCounts.Races} -> {restoredCounts.Races} {(originalCounts.Races == restoredCounts.Races ? "✓" : "✗")}");
        _output.WriteLine($"Series: {originalCounts.Series} -> {restoredCounts.Series} {(originalCounts.Series == restoredCounts.Series ? "✓" : "✗")}");
        _output.WriteLine($"Regattas: {originalCounts.Regattas} -> {restoredCounts.Regattas} {(originalCounts.Regattas == restoredCounts.Regattas ? "✓" : "✗")}");

        Assert.Equal(originalCounts.Competitors, restoredCounts.Competitors);
        Assert.Equal(originalCounts.Races, restoredCounts.Races);
        Assert.Equal(originalCounts.Series, restoredCounts.Series);
        Assert.Equal(originalCounts.Regattas, restoredCounts.Regattas);

        var restoredSeriesUpdateDates = await _context.Series
            .Where(s => s.ClubId == testClubId)
            .Select(s => s.UpdatedDate)
            .OrderBy(d => d)
            .ToListAsync();
        Assert.Equal(originalSeriesUpdateDates, restoredSeriesUpdateDates);

        _output.WriteLine("\n✓✓✓ PRODUCTION DATA TEST PASSED ✓✓✓");
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Category", "Database")]
    [Trait("Category", "ProductionData")]
    public async Task BackupAndRestore_AllClubs_RoundTripPreservesData()
    {
        if (!_useProductionData)
        {
            _output.WriteLine("Skipping - UseProductionData is false");
            return;
        }

        var clubs = await _context.Clubs
            .Select(c => new { c.Id, c.Name, c.Initials })
            .ToListAsync();

        _output.WriteLine($"Testing round-trip backup for {clubs.Count} clubs");
        var failures = new List<string>();

        foreach (var club in clubs)
        {
            try
            {
                _output.WriteLine($"--- Testing club: {club.Name} ({club.Initials}) ---");
                var before = await TakeClubSnapshotAsync(club.Id);

                var backup = await _backupService.CreateBackupAsync(club.Id, "multi-club-test");

                // Validate metadata counts match the backup content
                Assert.Equal(backup.BoatClasses?.Count ?? 0, backup.Metadata.BoatClassCount);
                Assert.Equal(backup.Competitors?.Count ?? 0, backup.Metadata.CompetitorCount);
                Assert.Equal(backup.Races?.Count ?? 0, backup.Metadata.RaceCount);
                Assert.Equal(backup.Series?.Count ?? 0, backup.Metadata.SeriesCount);

                await _backupService.RestoreBackupAsync(club.Id, backup, preserveClubName: true);
                var after = await TakeClubSnapshotAsync(club.Id);

                var diffs = CompareSnapshots(before, after);
                if (diffs.Count > 0)
                {
                    foreach (var diff in diffs)
                    {
                        _output.WriteLine($"  DIFF: {diff}");
                    }
                    failures.Add($"{club.Name} ({club.Initials}): {string.Join("; ", diffs)}");
                }
                else
                {
                    _output.WriteLine($"  OK - all counts match");
                }
            }
            catch (Exception ex)
            {
                var msg = $"{club.Name} ({club.Initials}): Exception - {ex.Message}";
                _output.WriteLine($"  FAIL: {msg}");
                failures.Add(msg);
            }
        }

        if (failures.Count > 0)
        {
            Assert.Fail($"Round-trip failures in {failures.Count} club(s):\n" +
                string.Join("\n", failures));
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    [Trait("Category", "Database")]
    [Trait("Category", "Slow")]
    public async Task BackupAndRestore_WithMassiveDataModification_FullRecovery()
    {
        // This test validates the backup/restore cycle by simulating massive data modifications.
        // Works with both fresh test databases and production data.
        // FIXED: BackupService.DeleteExistingClubDataAsync now uses ExecuteDeleteAsync() for relational
        // databases to avoid concurrency token issues, with fallback to load-and-remove for InMemory.

        // This test validates the backup/restore cycle by:
        // 1. Taking a snapshot of all data before modifications
        // 2. Creating a backup
        // 3. Deleting and modifying significant amounts of data
        // 4. Restoring from backup
        // 5. Comparing pre-backup state with post-restore state

        // Arrange - Take snapshot of original data
        var originalSnapshot = await TakeClubSnapshotAsync(_testClubId);

        _output.WriteLine("=== ORIGINAL DATA ===");
        _output.WriteLine($"BoatClasses: {originalSnapshot.BoatClassCount}");
        _output.WriteLine($"Seasons: {originalSnapshot.SeasonCount}");
        _output.WriteLine($"Fleets: {originalSnapshot.FleetCount}");
        _output.WriteLine($"Competitors: {originalSnapshot.CompetitorCount}");
        _output.WriteLine($"ScoringSystems: {originalSnapshot.ScoringSystemCount}");
        _output.WriteLine($"Series: {originalSnapshot.SeriesCount}");
        _output.WriteLine($"Races: {originalSnapshot.RaceCount}");
        _output.WriteLine($"Scores: {originalSnapshot.ScoreCount}");
        _output.WriteLine($"Regattas: {originalSnapshot.RegattaCount}");
        _output.WriteLine($"Announcements: {originalSnapshot.AnnouncementCount}");
        _output.WriteLine($"Documents: {originalSnapshot.DocumentCount}");

        // Act - Create backup of original state
        var backup = await _backupService.CreateBackupAsync(_testClubId, "modification-recovery-test");

        _output.WriteLine("\n=== BACKUP CREATED ===");
        _output.WriteLine($"Backup contains {backup.BoatClasses?.Count ?? 0} boat classes");
        _output.WriteLine($"Backup contains {backup.Competitors?.Count ?? 0} competitors");
        _output.WriteLine($"Backup contains {backup.Series?.Count ?? 0} series");
        _output.WriteLine($"Backup contains {backup.Races?.Count ?? 0} races");
        _output.WriteLine($"Backup contains {backup.Regattas?.Count ?? 0} regattas");
        _output.WriteLine($"Backup contains {backup.Announcements?.Count ?? 0} announcements");
        _output.WriteLine($"Backup contains {backup.Documents?.Count ?? 0} documents");
        _output.WriteLine($"Backup contains {backup.ChangeTypes?.Count ?? 0} change types");
        _output.WriteLine($"Backup contains {backup.CompetitorChanges?.Count ?? 0} competitor changes");

        // Simulate massive data modifications
        _output.WriteLine("\n=== APPLYING MODIFICATIONS ===");

        // Step 1: Delete races and their scores (in order: scores first, then races)
        // This mimics a user deleting old race data
        var racesToDelete = await _context.Races
            .Where(r => r.ClubId == _testClubId)
            .OrderBy(r => r.Date)
            .Take(Math.Max(1, originalSnapshot.RaceCount / 3))  // Delete 1/3 instead of 1/2
            .ToListAsync();

        if (racesToDelete.Count > 0)
        {
            _output.WriteLine($"Deleting {racesToDelete.Count} races and their associated scores...");
            var raceIdsToDelete = racesToDelete.Select(r => r.Id).ToList();

            // Delete scores for these races
            await _context.Scores
                .Where(s => raceIdsToDelete.Contains(s.RaceId))
                .ExecuteDeleteAsync();

            // Delete series-race associations
            await _context.SeriesRaces
                .Where(sr => raceIdsToDelete.Contains(sr.RaceId))
                .ExecuteDeleteAsync();

            // Delete races
            _context.Races.RemoveRange(racesToDelete);
            await _context.SaveChangesAsync();
        }

        // Step 2: Delete some competitors
        // This mimics a user removing sailors/competitors from the club
        var competitorsToDelete = await _context.Competitors
            .Where(c => c.ClubId == _testClubId)
            .OrderBy(c => c.Name)
            .Take(Math.Max(1, originalSnapshot.CompetitorCount / 3))
            .ToListAsync();

        if (competitorsToDelete.Count > 0)
        {
            _output.WriteLine($"Deleting {competitorsToDelete.Count} competitors...");
            var competitorIdsToDelete = competitorsToDelete.Select(c => c.Id).ToList();

            // Delete all scores for these competitors (respects FK constraint)
            await _context.Scores
                .Where(s => competitorIdsToDelete.Contains(s.CompetitorId))
                .ExecuteDeleteAsync();

            // Delete competitor-fleet associations
            await _context.CompetitorFleets
                .Where(cf => competitorIdsToDelete.Contains(cf.CompetitorId))
                .ExecuteDeleteAsync();

            // Delete competitor changes
            await _context.CompetitorChanges
                .Where(cc => competitorIdsToDelete.Contains(cc.CompetitorId))
                .ExecuteDeleteAsync();

            // Delete competitors themselves
            await _context.Competitors
                .Where(c => competitorIdsToDelete.Contains(c.Id))
                .ExecuteDeleteAsync();

            _output.WriteLine("Competitors deleted");
        }

        // Step 3: Delete some series
        // This mimics a user removing racing series/events
        var seriesToDelete = await _context.Series
            .Where(s => s.ClubId == _testClubId)
            .OrderBy(s => s.Name)
            .Take(Math.Max(1, originalSnapshot.SeriesCount / 4))
            .ToListAsync();

        if (seriesToDelete.Count > 0)
        {
            _output.WriteLine($"Deleting {seriesToDelete.Count} series...");
            var seriesIdsToDelete = seriesToDelete.Select(s => s.Id).ToList();

            // Delete series-race associations
            await _context.SeriesRaces
                .Where(sr => seriesIdsToDelete.Contains(sr.SeriesId))
                .ExecuteDeleteAsync();

            // Delete regatta-series associations
            await _context.RegattaSeries
                .Where(rs => seriesIdsToDelete.Contains(rs.SeriesId))
                .ExecuteDeleteAsync();

            // Delete series chart results
            await _context.SeriesChartResults
                .Where(scr => seriesIdsToDelete.Contains(scr.SeriesId))
                .ExecuteDeleteAsync();

            // Delete historical results
            await _context.HistoricalResults
                .Where(hr => seriesIdsToDelete.Contains(hr.SeriesId))
                .ExecuteDeleteAsync();

            // Delete series
            _context.Series.RemoveRange(seriesToDelete);
            await _context.SaveChangesAsync();
        }

        // Step 4: Skip adding/modifying competitors for production data (to avoid FK constraint issues with boat classes)
        if (!_useProductionData)
        {
            // Add and modify data only for fresh test database
            _output.WriteLine("Adding new competitor and modifying existing competitor...");

            var newCompetitor = new Db.Competitor
            {
                Id = Guid.NewGuid(),
                ClubId = _testClubId,
                Name = "Temporary Test Competitor - " + Guid.NewGuid().ToString().Substring(0, 8),
                SailNumber = "TEMP-" + Guid.NewGuid().ToString().Substring(0, 6),
                IsActive = true,
                Created = DateTime.UtcNow
            };
            _context.Competitors.Add(newCompetitor);

            // Modify a remaining competitor
            var competitorToModify = await _context.Competitors
                .Where(c => c.ClubId == _testClubId)
                .OrderByDescending(c => c.Created)
                .FirstOrDefaultAsync();

            if (competitorToModify != null)
            {
                competitorToModify.Name = "MODIFIED - " + Guid.NewGuid().ToString().Substring(0, 8);
                _output.WriteLine($"Modified competitor '{competitorToModify.Name}'");
            }

            await _context.SaveChangesAsync();
        }
        else
        {
            _output.WriteLine("Skipping competitor add/modify for production data (avoiding FK constraints)");
        }

        _output.WriteLine("\n=== MODIFICATIONS SAVED ===");

        // Verify modifications were applied
        var modifiedSnapshot = await TakeClubSnapshotAsync(_testClubId);
        _output.WriteLine($"After modifications - Competitors: {modifiedSnapshot.CompetitorCount}, Races: {modifiedSnapshot.RaceCount}, Series: {modifiedSnapshot.SeriesCount}");

        // Act - Restore from backup
        _output.WriteLine("\n=== RESTORING FROM BACKUP ===");
        var restoreSuccess = await _backupService.RestoreBackupAsync(_testClubId, backup, preserveClubName: true);
        Assert.True(restoreSuccess, "Restore operation should complete successfully");

        _output.WriteLine("Restore completed");

        // Assert - Verify restored state matches original state
        var restoredSnapshot = await TakeClubSnapshotAsync(_testClubId);

        _output.WriteLine("\n=== RESTORED DATA ===");
        _output.WriteLine($"BoatClasses: {restoredSnapshot.BoatClassCount}");
        _output.WriteLine($"Seasons: {restoredSnapshot.SeasonCount}");
        _output.WriteLine($"Fleets: {restoredSnapshot.FleetCount}");
        _output.WriteLine($"Competitors: {restoredSnapshot.CompetitorCount}");
        _output.WriteLine($"ScoringSystems: {restoredSnapshot.ScoringSystemCount}");
        _output.WriteLine($"Series: {restoredSnapshot.SeriesCount}");
        _output.WriteLine($"Races: {restoredSnapshot.RaceCount}");
        _output.WriteLine($"Scores: {restoredSnapshot.ScoreCount}");
        _output.WriteLine($"Regattas: {restoredSnapshot.RegattaCount}");
        _output.WriteLine($"Announcements: {restoredSnapshot.AnnouncementCount}");
        _output.WriteLine($"Documents: {restoredSnapshot.DocumentCount}");

        // Compare snapshots
        var differences = CompareSnapshots(originalSnapshot, restoredSnapshot);
        if (differences.Any())
        {
            _output.WriteLine("\n=== DIFFERENCES FOUND ===");
            foreach (var diff in differences)
            {
                _output.WriteLine($"  {diff}");
            }
        }
        else
        {
            _output.WriteLine("\n✓ All data matches perfectly!");
        }

        // Assert specific counts
        Assert.Equal(originalSnapshot.BoatClassCount, restoredSnapshot.BoatClassCount); 
        Assert.Equal(originalSnapshot.CompetitorCount, restoredSnapshot.CompetitorCount);
        Assert.Equal(originalSnapshot.SeriesCount, restoredSnapshot.SeriesCount);
        Assert.Equal(originalSnapshot.RaceCount, restoredSnapshot.RaceCount);
        Assert.Equal(originalSnapshot.ScoreCount, restoredSnapshot.ScoreCount);
        Assert.Equal(originalSnapshot.RegattaCount, restoredSnapshot.RegattaCount);
        Assert.Equal(originalSnapshot.AnnouncementCount, restoredSnapshot.AnnouncementCount);
        Assert.Equal(originalSnapshot.DocumentCount, restoredSnapshot.DocumentCount);

        // Verify names match (detects if wrong entities were restored)
        Assert.True(originalSnapshot.CompetitorNames.SequenceEqual(restoredSnapshot.CompetitorNames),
            "Competitor names don't match");
        Assert.True(originalSnapshot.SeriesNames.SequenceEqual(restoredSnapshot.SeriesNames),
            "Series names don't match");
    }

    #endregion

    #region Snapshot Helpers

    private record ClubSnapshot(
        int BoatClassCount,
        int SeasonCount,
        int FleetCount,
        int CompetitorCount,
        int ScoringSystemCount,
        int SeriesCount,
        int RaceCount,
        int ScoreCount,
        int RegattaCount,
        int AnnouncementCount,
        int DocumentCount,
        int ChangeTypeCount,
        int CompetitorChangeCount,
        List<string> CompetitorNames,
        List<string> SeriesNames,
        List<DateTime?> RaceDates,
        List<DateTime?> SeriesUpdateDates);

    private async Task<ClubSnapshot> TakeClubSnapshotAsync(Guid clubId)
    {
        var raceIds = await _context.Races
            .Where(r => r.ClubId == clubId)
            .Select(r => r.Id)
            .ToListAsync();

        var competitorIds = await _context.Competitors
            .Where(c => c.ClubId == clubId)
            .Select(c => c.Id)
            .ToListAsync();

        return new ClubSnapshot(
            BoatClassCount: await _context.BoatClasses.CountAsync(bc => bc.ClubId == clubId),
            SeasonCount: await _context.Seasons.CountAsync(s => s.ClubId == clubId),
            FleetCount: await _context.Fleets.CountAsync(f => f.ClubId == clubId),
            CompetitorCount: await _context.Competitors.CountAsync(c => c.ClubId == clubId),
            ScoringSystemCount: await _context.ScoringSystems.CountAsync(ss => ss.ClubId == clubId),
            SeriesCount: await _context.Series.CountAsync(s => s.ClubId == clubId),
            RaceCount: raceIds.Count,
            ScoreCount: await _context.Scores.CountAsync(s => raceIds.Contains(s.RaceId)),
            RegattaCount: await _context.Regattas.CountAsync(r => r.ClubId == clubId),
            AnnouncementCount: await _context.Announcements.CountAsync(a => a.ClubId == clubId),
            DocumentCount: await _context.Documents.CountAsync(d => d.ClubId == clubId),
            ChangeTypeCount: await _context.ChangeTypes.CountAsync(),
            CompetitorChangeCount: await _context.CompetitorChanges.CountAsync(cc => competitorIds.Contains(cc.CompetitorId)),
            CompetitorNames: await _context.Competitors
                .Where(c => c.ClubId == clubId)
                .OrderBy(c => c.Name)
                .Select(c => c.Name)
                .ToListAsync(),
            SeriesNames: await _context.Series
                .Where(s => s.ClubId == clubId)
                .OrderBy(s => s.Name)
                .Select(s => s.Name)
                .ToListAsync(),
            RaceDates: await _context.Races
                .Where(r => r.ClubId == clubId)
                .OrderBy(r => r.Date)
                .Select(r => r.Date)
                .ToListAsync(),
            SeriesUpdateDates: await _context.Series
                .Where(s => s.ClubId == clubId)
                .OrderBy(s => s.UpdatedDate)
                .Select(s => s.UpdatedDate)
                .ToListAsync()
        );
    }

    private static List<string> CompareSnapshots(ClubSnapshot before, ClubSnapshot after)
    {
        var diffs = new List<string>();

        if (before.BoatClassCount != after.BoatClassCount)
            diffs.Add($"BoatClasses: {before.BoatClassCount} -> {after.BoatClassCount}");
        if (before.SeasonCount != after.SeasonCount)
            diffs.Add($"Seasons: {before.SeasonCount} -> {after.SeasonCount}");
        if (before.FleetCount != after.FleetCount)
            diffs.Add($"Fleets: {before.FleetCount} -> {after.FleetCount}");
        if (before.CompetitorCount != after.CompetitorCount)
            diffs.Add($"Competitors: {before.CompetitorCount} -> {after.CompetitorCount}");
        if (before.ScoringSystemCount != after.ScoringSystemCount)
            diffs.Add($"ScoringSystems: {before.ScoringSystemCount} -> {after.ScoringSystemCount}");
        if (before.SeriesCount != after.SeriesCount)
            diffs.Add($"Series: {before.SeriesCount} -> {after.SeriesCount}");
        if (before.RaceCount != after.RaceCount)
            diffs.Add($"Races: {before.RaceCount} -> {after.RaceCount}");
        if (before.ScoreCount != after.ScoreCount)
            diffs.Add($"Scores: {before.ScoreCount} -> {after.ScoreCount}");
        if (before.RegattaCount != after.RegattaCount)
            diffs.Add($"Regattas: {before.RegattaCount} -> {after.RegattaCount}");
        if (before.AnnouncementCount != after.AnnouncementCount)
            diffs.Add($"Announcements: {before.AnnouncementCount} -> {after.AnnouncementCount}");
        if (before.DocumentCount != after.DocumentCount)
            diffs.Add($"Documents: {before.DocumentCount} -> {after.DocumentCount}");
        if (before.ChangeTypeCount != after.ChangeTypeCount)
            diffs.Add($"ChangeTypes: {before.ChangeTypeCount} -> {after.ChangeTypeCount}");
        if (before.CompetitorChangeCount != after.CompetitorChangeCount)
            diffs.Add($"CompetitorChanges: {before.CompetitorChangeCount} -> {after.CompetitorChangeCount}");

        if (!before.CompetitorNames.SequenceEqual(after.CompetitorNames))
            diffs.Add("CompetitorNames differ");
        if (!before.SeriesNames.SequenceEqual(after.SeriesNames))
            diffs.Add("SeriesNames differ");
        if (!before.RaceDates.SequenceEqual(after.RaceDates))
            diffs.Add("RaceDates differ");
        if (!before.SeriesUpdateDates.SequenceEqual(after.SeriesUpdateDates))
            diffs.Add("SeriesUpdateDates differ");

        return diffs;
    }

    #endregion
}
