using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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

        _backupService = new BackupService(_context, NullLogger<BackupService>.Instance);
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

            var connectionString = _dbContainer.GetConnectionString().Replace("Database=master", "Database=sailscores");
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
        var testClubId = await _context.Clubs
            .Where(c => c.Initials == "LHYC")
            .Select(c => c.Id)
            .FirstOrDefaultAsync();

        var clubCount = await _context.Clubs.CountAsync();
        _output.WriteLine($"Database has {clubCount} clubs");

        var club = await _context.Clubs
            .Include(c => c.WeatherSettings)
            .FirstOrDefaultAsync(c => c.Id == _testClubId);

        _output.WriteLine($"Testing club: {club.Name} ({club.Initials})");

        var originalCounts = new
        {
            Competitors = await _context.Competitors.CountAsync(c => c.ClubId == testClubId),
            Races = await _context.Races.CountAsync(r => r.ClubId == testClubId),
            Series = await _context.Series.CountAsync(s => s.ClubId == testClubId),
            Regattas = await _context.Regattas.CountAsync(r => r.ClubId == testClubId)
        };
        // grab dates of all series updates:
        var originalSeriesUpdateDates = await _context.Series
            .Where(s => s.ClubId == testClubId)
            .Select(s => s.UpdatedDate)
            .OrderBy(d => d)
            .ToListAsync();


        var raceToRemove = await _context.Races
            .Where(r => r.ClubId == testClubId)
            .OrderByDescending(r => r.Date)
            .FirstOrDefaultAsync();
        var seriesToRemove = await _context.Series
            .Where(s => s.ClubId == testClubId)
            .OrderBy(s => s.Name)
            .FirstOrDefaultAsync();

        _output.WriteLine($"Original counts - Competitors: {originalCounts.Competitors}, Races: {originalCounts.Races}, Series: {originalCounts.Series}, Regattas: {originalCounts.Regattas}");

        // Act
        var backup = await _backupService.CreateBackupAsync(testClubId, "production-test");
        _output.WriteLine("Production backup created successfully");

        if(raceToRemove != null)
        {
            _context.Races.Remove(raceToRemove);
            _context.SeriesRaces.RemoveRange(_context.SeriesRaces.Where(sr => seriesToRemove.Id == sr.SeriesId));
            _context.Series.Remove(seriesToRemove);
            await _context.SaveChangesAsync();
        }

        await _backupService.RestoreBackupAsync(testClubId, backup, preserveClubName: true);
        _output.WriteLine("Production backup restored successfully");

        // Assert
        var restoredCounts = new
        {
            Competitors = await _context.Competitors.CountAsync(c => c.ClubId == testClubId),
            Races = await _context.Races.CountAsync(r => r.ClubId == testClubId),
            Series = await _context.Series.CountAsync(s => s.ClubId == testClubId),
            Regattas = await _context.Regattas.CountAsync(r => r.ClubId == testClubId)
        };

        _output.WriteLine($"Restored counts - Competitors: {restoredCounts.Competitors}, Races: {restoredCounts.Races}, Series: {restoredCounts.Series}, Regattas: {restoredCounts.Regattas}");

        Assert.Equal(originalCounts.Competitors, restoredCounts.Competitors);
        Assert.Equal(originalCounts.Races, restoredCounts.Races);
        Assert.Equal(originalCounts.Series, restoredCounts.Series);
        Assert.Equal(originalCounts.Regattas, restoredCounts.Regattas);

        //compare series update dates
        var restoredSeriesUpdateDates = await _context.Series
            .Where(s => s.ClubId == testClubId)
            .Select(s => s.UpdatedDate)
            .OrderBy(d => d)
            .ToListAsync();
        Assert.Equal(originalSeriesUpdateDates, restoredSeriesUpdateDates);
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
