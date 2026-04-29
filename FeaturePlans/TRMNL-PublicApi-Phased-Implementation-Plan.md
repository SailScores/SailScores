# TRMNL / AI-Friendly Public JSON API - Phased Implementation Plan

## Goal

Provide clean, discoverable, stable JSON endpoints for public sailing results, starting with series results and
expanding to races and competitors.

This plan keeps API concerns out of `AuthController` and uses dedicated public API routes.

## Guiding Decisions

- Canonical JSON endpoints live under `/api/public/v1/...`.
- Route tokens are case-insensitive.
- Path and query values use standard URL encoding.
- Route token names use explicit URL-name terms: `{seasonUrlName}` and `{seriesUrlName}`.
- For public routes, a GUID `Id` is always a valid replacement for a URL-name token.
- Optional friendly `.json` aliases may be added later for key human-facing pages.
- Public read endpoints are anonymous; write/admin endpoints remain JWT/policy protected.
- Controllers remain thin; business logic stays in services.
- Series detail can include competitors and/or races via `?include=competitors`,
  `?include=races`, or `?include=competitors,races`.
- Competitors are returned in ranked order, with standing fields embedded in each competitor item.
- Pre-ship contract evolution can prioritize simplicity; strict backward compatibility is required at launch.

---

## Phase 0 - Foundation (Design + Contracts)

### Objectives
- Lock route and response contracts before implementation.
- Ensure TRMNL-friendly payload shape (small, predictable, linkable).

### Deliverables
- Route map for v1:
  - `GET /api/public/v1`
  - `GET /api/public/v1/clubs`
  - `GET /api/public/v1/clubs/{clubInitials}/seasons`
  - `GET /api/public/v1/clubs/{clubInitials}/series`
  - `GET /api/public/v1/clubs/{clubInitials}/seasons/{seasonUrlName}/series`
  - `GET /api/public/v1/clubs/{clubInitials}/seasons/{seasonUrlName}/series/{seriesUrlName}`
- Response contract docs (fields + examples).
- Date/time contract:
  - Use UTC only.
  - Serialize using ISO 8601 round-trip format (for example, `2026-01-15T18:42:13.511Z`).
- Nullability contract:
  - Use `null` for known fields with no value.
  - Omit sections that are only present via optional includes (`competitors`, `races`) when not requested.
- Error contract (`404`, `400`, `429`, `500`) using RFC 7807 `ProblemDetails` with:
  - `type`, `title`, `status`, `detail`, `instance`
  - extension fields: `traceId`, `errorCode` (stable machine-readable code)
- Shared helper methods in public API controller base/helper to produce consistent error responses.

### Notes
- Do not add these endpoints under `AuthController`.
- Place in dedicated public API controllers under `SailScores.Web/Areas/Api/Controllers/`.

---

## Phase 1 - Series Results Detail (Test of concept)

### Objectives
- Develop one reliable public endpoint for full series results and allow testing of model.

### Endpoints
- `GET /api/public/v1`
- `GET /api/public/v1/clubs/{clubInitials}/seasons/{seasonUrlName}/series/{seriesUrlName}`
- `GET /api/public/v1/clubs/{clubInitials}/seasons/{seasonUrlName}/series/{seriesUrlName}?include=competitors`

### Behavior
- `[AllowAnonymous]`
- Default response (without `include`, or without `competitors` in `include`):
  - Series identity (`id`, `name`, `urlName`)
  - Club + season identifiers
  - Last updated metadata
  - Summary standings count (for example, total number of competitors ranked)
- With `include=competitors`:
  - All default fields plus a ranked competitors list
  - Each competitor includes standing fields and is sorted by rank (ascending)
- With `include=races`:
  - Include race metadata and score codes
  - Omit race competitor results unless `competitors` is also included
- Add cache-control headers appropriate for public read endpoints.

### Implementation Tasks
1. Add `PublicSeriesController` (or equivalent) in `Areas/Api/Controllers`.
2. Add public API root endpoint listing available v1 resources.
3. Add web service adapter for public result projection (if needed), backed by existing series services.
4. Map domain model to a dedicated public response DTO (do not expose internal entities directly).
5. Implement `include` query parameter handling:
   - When omitted: return lightweight series summary without competitor or race arrays
   - When `include=competitors`: include full ranked competitors list with standings embedded in each competitor
   - When `include=races`: include race details and score codes, but omit race competitor results
   - When `include=competitors,races`: include both sections and include race results in both views
6. Add shared helper methods for consistent `ProblemDetails` responses.
7. Add unit tests for:
   - valid series lookup with no include values
   - valid series lookup with `include=competitors`
   - valid series lookup with `include=races`
   - valid series lookup with `include=competitors,races`
   - competitor ranking order in response payload when included
   - not found behavior
   - hidden/non-public club handling (if applicable)

### Exit Criteria
- Endpoint returns stable JSON for known public series and is test-covered.
- Default response is lightweight and fast.
- `include=competitors` returns ranked competitor standings.
- API root can be used to discover primary resources.

---

## Phase 2 - Indexes for TRMNL Discovery

### Objectives
- Add key list endpoints needed for browsing/integration workflows.

### Endpoints
- `GET /api/public/v1/clubs`
- `GET /api/public/v1/clubs/{clubInitials}/seasons`
- `GET /api/public/v1/clubs/{clubInitials}/series`
- `GET /api/public/v1/clubs/{clubInitials}/seasons/{seasonUrlName}/series`

### Behavior
- Return lightweight lists with stable keys:
  - `clubInitials`, `seasonName`, `seriesName`
  - canonical URL
  - updated timestamp where available
- Sorting defaults:
  - clubs list: alphabetical by `clubInitials`
  - most other lists: most recent first
- Pagination via URL parameters.
- Default is full-list response when pagination parameters are omitted.
- Hidden clubs do not appear in index/list responses.
- Add cache-control headers for list endpoints.

### Implementation Tasks
1. Public clubs list should use public visibility semantics (`GetClubs(false)` behavior).
2. Add season list projection per club.
3. Add series list projection per club and per season.
4. Add pagination parameter handling and tests.
5. Add integration tests for list ordering and filtering rules.

### Exit Criteria
- TRMNL can discover clubs → seasons → series without custom scraping.

---

## Phase 3 - Optional Expansions (Races)

### Objectives
- Extend detail payloads without breaking existing clients once the contract is considered stable.

### Approach
- Keep base series response lean.
- Add opt-in expansions via a single `include` query parameter:
  - `?include=races`
  - `?include=competitors`
  - `?include=competitors,races`

### Implementation Tasks
1. Add response DTO section for races.
2. Add `include=races` handling.
3. Ensure each optional include (`competitors`, `races`) can be toggled independently.
4. Keep default response unchanged once launch contract is frozen.
5. Add tests for:
   - `include=races` only
   - `include=competitors` only
   - `include=competitors,races`
   - payload size sanity for all combinations

### Exit Criteria
- Consumers can request race details when needed, while default remains fast and small.
- Multiple include values work independently and in combination.

---

## Phase 4 - Discoverability + Friendly Aliases

### Objectives
- Improve human discoverability while preserving canonical API.

### Optional Enhancements
- Add `.json` aliases for key public pages:
  - `/{clubInitials}/{seasonName}/{seriesName}.json`
- Add `rel="alternate"` JSON links in corresponding HTML pages.

### Exit Criteria
- Humans and bots can discover JSON endpoints from both API root and page-level links.

---

## Phase 5 - Operational Hardening (Public Internet Readiness)

### Objectives
- Protect service reliability and control abuse.

### Tasks
1. Rate limiting for `/api/public/v1/*`.
2. Response caching strategy validation and cache-header tuning for list/detail endpoints.
3. Structured request logging and telemetry for public routes.
4. Optional API key tier for higher quota (keep basic reads public if desired).

### Exit Criteria
- Public endpoints are resilient, observable, and abuse-tolerant.

---

## Authentication and Access Strategy

- **Public Read API (`/api/public/v1/*`)**: anonymous by default.
- **Private/Edit API (`/api/*` existing controllers)**: keep JWT/policy authorization.
- Hidden clubs are excluded from index/list endpoints, but direct known routes can still return public content.
- If public abuse appears, introduce optional API keys for elevated limits, not mandatory for basic read access.

---

## Suggested File/Component Additions

- `SailScores.Web/Areas/Api/Controllers/PublicSeriesController.cs`
- `SailScores.Web/Areas/Api/Controllers/PublicIndexController.cs`
- `SailScores.Web/Areas/Api/Controllers/PublicApiControllerBase.cs` (optional shared error helpers)
- `SailScores.Web/Services/Interfaces/IPublicApiService.cs` (optional)
- `SailScores.Web/Services/PublicApiService.cs` (optional)
- `SailScores.ApiClient/Dtos/Public/*` (or Web-layer DTO folder)
- Unit/integration tests in `SailScores.Test.Unit` and `SailScores.Test.Integration`

---

## Versioning and Compatibility

- Start at `/api/public/v1`.
- Before public launch, contracts may be simplified as needed.
- After launch, add fields additively only within v1.
- Reserve `/v2` for breaking contract changes.

---

## Milestone Summary

- **M1:** API root + series detail endpoint live with `include=competitors` support (Phase 1)
- **M2:** Clubs/seasons/series indexes live (Phase 2)
- **M3:** `include=races` expansion live, with combined `include=competitors,races` support (Phase 3)
- **M4:** discoverability aliases + hardening complete (Phases 4-5)
