# Custom Competitor Fields Implementation Plan

## Goal
Add club-defined custom fields to competitors, with values entered during competitor editing and support for date-ranged values that can be used in series results templates.

## Scope
- Custom fields are defined at the club level.
- All competitors in a club share the same available custom fields.
- Field values are entered when editing a competitor.
- Custom fields can be surfaced in series results templates.
- Each custom field can optionally support date ranges.
- When a competitor raced across multiple date ranges in a series, results should display `-multiple-`.

## Key Design Decisions
- New club-level custom field definitions, stored per club.
- New per-competitor values, with optional effective-date ranges.
- New template associations so each template can choose which custom fields to display.
- Custom field values are optional.
- Custom fields are hidden by default unless enabled for the club.
- Display order is configurable.
- Field headers are configured per field definition, not per template.
- Custom field values use the existing competitor-change history approach.

## Data Model
### New entities
- `CompetitorFieldDefinition`
  - `Id`
  - `ClubId`
  - `Name`
  - `DisplayHeader`
  - `DataType` (`Text` or `Number`)
  - `DisplayOrder`
  - `IsActive`

- `CompetitorFieldValue`
  - `Id`
  - `CompetitorId`
  - `FieldDefinitionId`
  - `Value`
  - `EffectiveFrom`
  - `EffectiveTo`

- `SeriesResultsTemplateCustomField`
  - `Id`
  - `SeriesResultsTemplateId`
  - `FieldDefinitionId`
  - `Visibility`
  - `DisplayOrder`

### Modified entities
- `Club`
  - add `EnableCustomCompetitorFields`

## UX / Workflow
1. Club admin creates and manages custom field definitions from a dedicated “Custom Fields” page.
2. Competitor edit page includes a separate custom fields section for entering values.
3. Date-range support is available for each field value (optional).
4. Series results template editor lets admins select which custom fields to show and the visibility for each.
5. Results rendering resolves values per series based on the races the competitor participated in:
   - if all races fall in a single effective range, show that value
   - if multiple ranges are involved, show `-multiple-`

## Implementation Phases
### Phase 1 – Database and Services
- Add entities and relationships
- Add migrations
- Create core services for CRUD and value resolution

### Phase 2 – Club Admin UI
- Add club-level setting
- Add dedicated Custom Fields management page

### Phase 3 – Competitor Editing
- Extend competitor edit form
- Support field values and date ranges

### Phase 4 – Results Templates
- Extend template configuration
- Support visibility and ordering for custom fields

### Phase 5 – Results Rendering
- Render custom field columns in results tables
- Resolve and display date-range values correctly

### Phase 6 – Testing
- Unit tests for resolution rules
- Integration tests for CRUD and rendering
- Manual verification for edge cases and usability

## Notes
This feature is intentionally advanced and should stay unobtrusive for clubs that do not define custom fields.
