# Public API v1 Contracts (Phase 0)

This document defines response and error contracts for `/api/public/v1` endpoints.

## Cross-cutting Rules

- Routes are case-insensitive.
- Standard URL encoding applies to path and query values.
- URL-name route tokens use explicit names such as `seasonUrlName` and `seriesUrlName`.
- A GUID `Id` is always a valid replacement for a URL-name token in public routes.
- Date/time values are UTC and serialized as ISO 8601 round-trip (for example, `2026-01-15T18:42:13.511Z`).
- Known fields with no value return `null`.
- Optional include-only sections are omitted when not requested.
- Error responses use RFC 7807 `ProblemDetails` with `traceId` and `errorCode` extensions.

## Root Discovery

`GET /api/public/v1`

Response contract: `PublicApiRootResponseDto`

```json
{
  "version": "v1",
  "clubsIndexUrl": "/api/public/v1/clubs"
}
```

## Club Detail

`GET /api/public/v1/clubs/{clubToken}`

`clubToken` can be either `clubInitials` or a club `Guid`.

Response contract: `PublicClubDetailResponseDto`

```json
{
  "id": "8f7f9f76-7ee7-4eb0-bc88-2f7057b1d37a",
  "clubInitials": "MYC",
  "name": "My Club",
  "description": "Club Description",
  "url": "/api/public/v1/clubs/MYC",
  "htmlUrl": "/MYC",
  "updatedUtc": null
}
```

## List Responses

All list endpoints use `PublicListResponseDto<TItem>`.

### Paging

- Paging is optional.
- Query parameters:
  - `page` (1-based)
  - `pageSize`
- To enable paging, provide both `page` and `pageSize`.
- If paging parameters are omitted, endpoints return full lists.

### Fields

- `items` (array, required)
- `pagination` (object, optional)
  - included only when paging parameters are provided
  - fields: `page`, `pageSize`, `totalCount`
  - For example, with `page=2` and `pageSize=100`:  
```json
{
  "items": [...],
  "pagination": {
    "page": 2,
    "pageSize": 100,
    "totalCount": 257
  }
}
```

### Sorting Defaults

- Clubs list: alphabetical by `clubInitials`
- Competitor lists: A custom alphanumeric sort on `sailNumber` when present, then `CompetitorName`
- Other lists related to events: most recent first

### Item Contracts

- `PublicClubListItemDto`
- `PublicSeasonListItemDto`
- `PublicSeriesListItemDto`

## Series Detail

`GET /api/public/v1/clubs/{clubInitials}/seasons/{seasonUrlName}/series/{seriesUrlName}`

Response contract: `PublicSeriesDetailResponseDto`

### Required sections

- identity: `id`, `name`, `urlName`
- route identity: `clubInitials`, `seasonName`
- `competitors` (ranked order; each competitor includes standing data such as `rank` and `totalPoints`)

### Optional include sections

- `races` appears only for `?include=races`

## Error Contract

All public API errors should be produced with shared helper methods in `PublicApiControllerBase`.

Example 404:

```json
{
  "type": "https://httpstatuses.com/404",
  "title": "Not Found",
  "status": 404,
  "detail": "Series was not found for the provided route.",
  "instance": "/api/public/v1/clubs/abc/seasons/2026/series/spring",
  "traceId": "00-9b5...",
  "errorCode": "series_not_found"
}
