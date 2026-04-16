# SailScores — Claude Code Guide

The primary agent reference is **`.github/copilot-instructions.md`**. Read it first. This file adds Claude-specific notes and test-project guidance.

---

## Quick-start checklist

| Step | Command |
|------|---------|
| Build | `dotnet build SailScores.sln --configuration Release` |
| Unit tests | `dotnet test SailScores.Test.Unit/SailScores.Test.Unit.csproj` |
| Import/export tests | `dotnet test SailScores.ImportExport.Test/SailScores.ImportExport.Test.csproj` |
| Add EF migration | `dotnet ef migrations add <Name> --project SailScores.Database --startup-project SailScores.Web --context SailScoresContext` |
| Remove last migration | `dotnet ef migrations remove --project SailScores.Database --startup-project SailScores.Web --context SailScoresContext` |

**Never hand-edit** migration `.Designer.cs` files or the model snapshot — always use the EF CLI.

---

## Sailing domain glossary

See **`docs/SailingGlossary.md`** for a glossary of sailing terms as they apply to SailScores.

---

## Testing: which project to use

| Scenario | Project | Pattern |
|----------|---------|---------|
| Core service logic (SeriesService, RaceService, etc.) | `SailScores.Test.Unit` | `InMemoryContextBuilder.GetContext()` for the DB context + `Moq` for non-DB dependencies (IScoringService, IMemoryCache, etc.) |
| Web controller behavior | `SailScores.Test.Unit` | Moq **all** dependencies (web services, auth service, UserManager). Use `ControllerTestUtilities` helpers for common mocks. |
| Scoring calculations | `SailScores.Test.Unit` | Pure unit tests — no DB or mocks needed; construct model objects directly. |
| Import/export (CSV/Excel parsing) | `SailScores.ImportExport.Test` | Use real file fixtures from the test project. |
| Scenarios that genuinely require a real SQL Server (e.g., backup/restore, raw SQL) | `SailScores.Test.Integration` | Requires a running database configured in `appsettings.IntegrationTests.json`. |
| End-to-end browser flows | `SailScores.Test.Playwright` | Requires a running application instance. |

### Key test utilities

- **`SailScores.Test.Unit/Utilities/InMemoryContextBuilder.cs`** — Creates a pre-seeded EF in-memory database with a club, fleet, competitor, season, scoring system, series, race, and regatta. Use this as the starting point for core service tests; add extra entities in the test constructor as needed.
- **`SailScores.Test.Unit/Utilities/MapperBuilder.cs`** — Creates a configured `IMapper` (AutoMapper) with the `DbToModelMappingProfile`. Reuse this rather than building a new `MapperConfiguration` in each test.
- **`SailScores.Test.Unit/Web/Controllers/ControllerTestUtilities.cs`** — Factory methods for common web-layer mocks (`MakeCoreClubServiceMock()`, `MakeWebCompetitorServiceMock()`, etc.).

### Test naming convention

```
MethodName_StateUnderTest_ExpectedBehavior
```

Example: `GetSeries_WithValidClubAndSeriesUrl_ReturnsSeriesWithResults`

---

## Authorization — two-level rule

Every admin feature needs **both**:

1. **Controller/action level** — `[Authorize(Policy = AuthorizationPolicies.ClubAdmin)]` (or SeriesScorekeeper / RaceScorekeeper)
2. **View level** — hide UI elements using `Model.IsClubAdmin`, `Model.CanEditSeries`, etc.

Users with `UserClubPermission.CanEditAllClubs` are super-admins and bypass club-specific checks — do not introduce per-club checks that ignore this flag.

---

## Things to watch out for

- **`SeriesRace` is mandatory** — never add a race to a series by navigating `Series.Races` directly; always create a `SeriesRace` entity.
- **`Race.FleetId` is non-nullable** — every race must belong to exactly one fleet.
- **Multi-fleet series** — a series can contain races from different fleets; do not assume all races in a series share a fleet.
- **Shared scoring systems** — have `ClubId = null` and serve as base systems; never delete or modify them. These are carefully curated for rule compliance.
- **libman build errors (LIB002)** — network-related, safe to ignore for backend-only changes.
- **`wwwroot/js/`** is gitignored (except vendor files) — edit TypeScript/JS in `SailScores.Web/Scripts/` instead.
