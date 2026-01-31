# BackupService Integration Tests

## Overview

These integration tests use **Testcontainers** to spin up a real SQL Server instance in Docker and test the BackupService with actual database operations. The tests can run with either:

1. **Seeded test data** (default) - Fast, deterministic tests
2. **Production `.bacpac` file** - Test with real production data

## Prerequisites

### Required
- **Docker Desktop** - Must be running
- **.NET 10 SDK**
- **NuGet packages** (restored automatically):
  - `Testcontainers.MsSql`
  - `Microsoft.EntityFrameworkCore.SqlServer`
  - `xunit`

### Optional (for production data testing)
- **SqlPackage.exe** - For importing `.bacpac` files
  - Installed with SQL Server Management Studio (SSMS)
  - Or installed with SQL Server Data Tools
  - Common locations checked by tests:
    - `C:\Program Files\Microsoft SQL Server\160\DAC\bin\`
    - `C:\Program Files\Microsoft SQL Server\150\DAC\bin\`

## Running Tests

### Option 1: With Seeded Test Data (Default)

```bash
# Run all integration tests
dotnet test SailScores.Test.Integration/SailScores.Test.Integration.csproj

# Run only backup integration tests
dotnet test SailScores.Test.Integration/SailScores.Test.Integration.csproj --filter "FullyQualifiedName~BackupServiceIntegrationTests"

# Run and show detailed output
dotnet test SailScores.Test.Integration/SailScores.Test.Integration.csproj --filter "BackupServiceIntegrationTests" --logger "console;verbosity=detailed"
```

### Option 2: With Production Data

1. **Export production database to `.bacpac`:**

   In Azure Portal → SQL Database → Export:
   - Create a `.bacpac` file of your production database
   - Download to local machine (e.g., `C:\Backups\sailscores-production.bacpac`)

2. **Update configuration:**

   Edit `SailScores.Test.Integration/appsettings.IntegrationTests.json`:

   ```json
   {
     "IntegrationTests": {
       "UseProductionData": true,
       "BacpacPath": "C:\\Backups\\sailscores-production.bacpac"
     }
   }
   ```

3. **Enable the production data test:**

   Remove the `[Skip]` attribute from the test:
   ```csharp
   [Fact] // Remove: [Fact(Skip = "Only run when testing with production data")]
   [Trait("Category", "ProductionData")]
   public async Task BackupAndRestore_WithProductionData_CompletesSuccessfully()
   ```

4. **Run the test:**

   ```bash
   dotnet test --filter "Category=ProductionData"
   ```

## Test Categories

Tests are tagged with traits for selective execution:

- `Integration` - All integration tests
- `Database` - Tests requiring database
- `Slow` - Tests that take longer to run
- `ProductionData` - Tests using production data (skipped by default)

**Run by category:**
```bash
# Run all integration tests
dotnet test --filter "Category=Integration"

# Run only database tests
dotnet test --filter "Category=Database"

# Exclude slow tests
dotnet test --filter "Category!=Slow"
```

## What Gets Tested

### Standard Integration Tests (with seeded data)

1. **CreateBackup_WithRealDatabase_ProducesValidBackup**
   - Verifies backup creates valid structure
   - Checks all collections are populated

2. **BackupAndRestore_FullRoundTrip_WithRealDatabase_PreservesAllData**
   - Full backup and restore cycle
   - Verifies entity counts match exactly

3. **BackupAndRestore_WithComplexRelationships_MaintainsIntegrity**
   - Tests Fleet ↔ BoatClass ↔ Competitor relationships
   - Verifies join tables restored correctly

4. **BackupAndRestore_WithScoringSystemHierarchy_PreservesParentChildLinks**
   - Tests scoring system parent/child relationships
   - Verifies GUID remapping works correctly

5. **BackupAndRestore_WithRaceScoresAndWeather_PreservesAllDetails**
   - Tests Race → Scores and Race → Weather relationships
   - Verifies all nested data preserved

### Production Data Test (opt-in)

6. **BackupAndRestore_WithProductionData_CompletesSuccessfully**
   - Uses real production database
   - Verifies backup/restore with actual club data
   - Tests at scale with real-world complexity

## Seeded Test Data

The default tests seed a comprehensive test club with:

- ✅ Club with weather settings
- ✅ 2 Boat classes (Laser, Sunfish)
- ✅ 1 Season (2024)
- ✅ 2 Scoring systems (parent + child hierarchy)
- ✅ Score codes (DNS, etc.)
- ✅ 1 Fleet with boat class associations
- ✅ 2 Competitors with fleet memberships
- ✅ 1 Series linked to season and scoring system
- ✅ 1 Race with:
  - Weather data
  - 2 Scores
  - Series association
- ✅ 1 Regatta with series and fleet links
- ✅ 1 Announcement
- ✅ 1 Club sequence

This provides comprehensive coverage of all entity types and relationships.

## Troubleshooting

### Docker not running
```
Error: Docker is not running
```
**Solution:** Start Docker Desktop

### SqlPackage.exe not found
```
WARNING: SqlPackage.exe not found. Falling back to fresh database.
```
**Solution:** 
- Install SQL Server Management Studio (SSMS), or
- Install SQL Server Data Tools, or
- Continue with seeded test data (tests still work)

### Bacpac file not found
```
WARNING: UseProductionData=true but bacpac file not found at: ...
```
**Solution:** 
- Check the path in `appsettings.IntegrationTests.json`
- Ensure file exists and path uses double backslashes (`\\`)

### Container startup timeout
```
Testcontainers.MsSql timeout
```
**Solution:**
- Increase Docker Desktop resources (Memory/CPU)
- Ensure no other SQL containers are using ports
- Wait for Docker to fully start

### Tests are slow
**Expected behavior:** Integration tests with Docker containers take longer than unit tests.

**Typical timings:**
- Container startup: 15-30 seconds (once per test class)
- Seeded data tests: 5-10 seconds each
- Production data restore: 1-5 minutes (depends on bacpac size)

**To speed up:**
- Use test collection fixtures to share containers
- Run only specific tests during development
- Use `--filter` to exclude slow tests

## CI/CD Integration

### GitHub Actions Example

```yaml
name: Integration Tests

on: [push, pull_request]

jobs:
  integration-tests:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '10.0.x'
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Run integration tests
      run: dotnet test SailScores.Test.Integration --filter "Category=Integration&Category!=ProductionData"
```

**Note:** Production data tests are excluded from CI/CD by default (use `Category!=ProductionData` filter).

## Best Practices

### During Development
- Run unit tests first (fast feedback)
- Run integration tests before commits
- Use `--filter` to run specific test categories

### Before Release
- Run all integration tests including slow ones
- Optionally test with production bacpac
- Verify all entity types are backed up/restored

### When Database Schema Changes
- Update seeded test data if new entities added
- Update `BackupServiceTests` field coverage tests
- Run integration tests to verify migrations work

## Performance Tips

1. **Share containers across tests** (future enhancement):
   ```csharp
   [CollectionDefinition("Database collection")]
   public class DatabaseCollection : ICollectionFixture<DatabaseFixture> { }
   ```

2. **Use test output for debugging:**
   ```csharp
   _output.WriteLine($"Debug info: {someValue}");
   ```

3. **Run specific tests:**
   ```bash
   dotnet test --filter "FullyQualifiedName~BackupAndRestore_FullRoundTrip"
   ```

## Security Notes

- ⚠️ **Never commit production `.bacpac` files** to source control
- ⚠️ Add `*.bacpac` to `.gitignore`
- ⚠️ Production bacpac files may contain sensitive data
- ✅ Container passwords are for local testing only
- ✅ Containers are destroyed after tests complete

## Additional Resources

- [Testcontainers Documentation](https://dotnet.testcontainers.org/)
- [SqlPackage Documentation](https://docs.microsoft.com/sql/tools/sqlpackage)
- [xUnit Documentation](https://xunit.net/)
