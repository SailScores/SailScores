# SailScores - Copilot Instructions

## Project Overview

SailScores is a free web service for sharing club sailing scores, hosted at [sailscores.com](https://www.sailscores.com). The application provides modern, mobile-friendly score tracking for sailing clubs, supporting both club series scoring and regatta scoring with accurate Appendix A results.

## Tech Stack

- **Framework**: ASP.NET Core 10 MVC
- **Language**: C# with .NET 10
- **Database**: Microsoft SQL Server
- **ORM**: Entity Framework Core 10
- **Frontend**: Razor views (.cshtml), JavaScript, Bootstrap 5
- **Hosting**: Azure App Service Linux

## Architecture

SailScores follows an ASP.NET Core MVC pattern with layered architecture:

1. **SailScores.Web**: Main web application with controllers, views, and web-specific services
2. **SailScores.Core**: Business logic and core services (scoring, database operations)
3. **SailScores.Database**: Entity Framework database context and models
4. **SailScores.Identity**: Authentication and identity management
5. **SailScores.Api**: API client
6. **SailScores.ImportExport**: Import/export functionality
7. **SailScores.Utility**: Shared utilities

### Request Flow Pattern

1. Browser → Controller (`SailScores.Web/Controllers/`)
2. Controller → Web Service (`SailScores.Web/Services/`)
3. Web Service → Core Service (`SailScores.Core/Services/`)
4. Core Service → Entity Framework (`SailScoresContext`)
5. Database ← Entity Framework
6. Return path: Model → Controller → View (`.cshtml`)

### Key Guidelines

- **All database calls** must go through Core services via `_dbContext`
- **Core services** contain reusable business logic that could be used by different clients
- **Web services** contain logic specific to web views and UI concerns
- **Views** should focus on data presentation, not business logic
- Users with `UserClubPermission.CanEditAllClubs` are treated as super-admins (full admin) who bypass normal club-specific permission checks.
- **Administrative features** must always use restrictive authorization policies (e.g., `[Authorize(Policy = AuthorizationPolicies.ClubAdmin)]`).

## Development Setup

### Prerequisites

- .NET 8 SDK
- Docker (for SQL Server database)
- Visual Studio or VS Code (optional but recommended)

### Database Setup

Run SQL Server in Docker (for local development only):

```bash
docker run -e 'ACCEPT_EULA=Y' -e 'SA_PASSWORD=P@ssw0rd' --name 'SailScoreSql' -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest
```

**Security Note**: The password `P@ssw0rd` is for local development only. Production environments should use secure credential management (Azure Key Vault, environment variables, etc.).

### Connection String

Update `SailScores.Web/appsettings.json` with (for local development):

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=sailscores;User Id=sa;password=P@ssw0rd;MultipleActiveResultSets=true"
}
```

**Security Note**: Never commit real credentials to source control. This connection string is for local development only. Production uses Azure App Service configuration.

### First Run

1. Clone repository
2. Open `SailScores.sln`
3. Restore NuGet packages
4. Run the application
5. Click "Apply Migrations" button to initialize database

## Build and Test

### Build

```bash
dotnet build SailScores.sln --configuration Release
```

### Test

Unit tests:
```bash
dotnet test SailScores.Test.Unit/SailScores.Test.Unit.csproj
```

Integration tests (requires database):
```bash
dotnet test SailScores.Test.Integration/SailScores.Test.Integration.csproj
```

Import/Export tests:
```bash
dotnet test SailScores.ImportExport.Test/SailScores.ImportExport.Test.csproj
```

### Note on libman Errors

The project uses libman for client-side libraries (Bootstrap, jQuery). Build errors related to libman (e.g., `LIB002`) are network-related and can be safely ignored for backend/core changes. The web application will still build successfully.

## Important File Locations

- **Controllers**: `SailScores.Web/Controllers/`
- **Views**: `SailScores.Web/Views/`
- **Core Services**: `SailScores.Core/Services/`
- **Web Services**: `SailScores.Web/Services/`
- **Database Models**: `SailScores.Database/Entities/`
- **Database Context**: `SailScores.Database/SailScoresContext.cs`
- **Migrations**: `SailScores.Database/Migrations/`
- **JavaScript/TypeScript**: `SailScores.Web/Scripts/`

## Code Style and Conventions

- Follow standard C# naming conventions (PascalCase for public members, camelCase for private fields)
- Use dependency injection for services
- Keep controllers thin - delegate business logic to services
- Add comments only when necessary to explain complex logic
- Match the existing code style in each file

### JavaScript Files

- JavaScript and TypeScript files should be created in `SailScores.Web/Scripts/` folder
- Files in this folder are automatically copied to `wwwroot/js/` at build time
- The `wwwroot/js/` folder is gitignored except for vendor files
- Use `.js` for JavaScript files or `.ts` for TypeScript files (which will be compiled)
- Examples: `seriesChart.js`, `raceEditor.ts`

## Common Patterns

### Adding a New Feature

1. Create/modify database entities in `SailScores.Database/Entities/`
2. Add migration: `dotnet ef migrations add MigrationName --project SailScores.Database`
3. Implement business logic in Core services (`SailScores.Core/Services/`)
4. Create web-specific logic in Web services (`SailScores.Web/Services/`)
5. Add controller actions (`SailScores.Web/Controllers/`)
6. Create/modify views (`SailScores.Web/Views/`)
7. Add unit tests in `SailScores.Test.Unit/`

### Copilot and Migrations

- **Always use EF Core tooling** to create migrations. Do not manually create migration files.
- Do not generate or hand-edit migration `.Designer.cs` or model snapshot files. These are generated by EF Core tooling and should not be authored manually.
- Copilot may suggest `Up`/`Down` method code snippets. After accepting suggestions, always create the migration using the EF Core CLI so the `.Designer.cs` and snapshot are produced correctly:
  ```bash
  dotnet ef migrations add MigrationName --project SailScores.Database --startup-project SailScores.Web --context SailScoresContext
  ```
- Ensure the `dotnet-ef` tool version matches the EF Core packages used by the project.
- **If build failures prevent running EF tooling**: Do not create migrations manually. Instead, note the issue in the PR and request that the migration be generated locally by a developer with a working build environment. The migration can be added in a follow-up commit.
- If a migration designer or snapshot is missing or corrupted and the migration has not been applied, prefer regenerating it:
  ```bash
  dotnet ef migrations remove --project SailScores.Database --startup-project SailScores.Web --context SailScoresContext
  ```
  then
  ```bash
  dotnet ef migrations add MigrationName --project SailScores.Database --startup-project SailScores.Web --context SailScoresContext
  ```
- If a migration has already been applied to a database, do not remove it. Create a corrective migration instead.
- Only commit generated `.Designer.cs` and snapshot files that were created by EF Core tooling and verified to build.

### Working with Scores

- Scoring calculations are in `SailScores.Core/Scoring/`
- Supports Appendix A and High Point Percentage systems
- Race results include proper rounding and tie-breaking

## Testing Strategy

- Unit tests should mock database context and services
- Integration tests use actual database connections
- Test naming: `MethodName_StateUnderTest_ExpectedBehavior`
- Use xUnit for test framework

## Additional Resources

- Full development guide: `docs/Development.md`
- API documentation: REST endpoints support public API access
- Sample database: `Sql Utility Scripts/starter.bak`

## License

Mozilla Public License Version 2.0 - Share source for modifications you distribute.
