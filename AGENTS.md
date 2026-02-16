# AI Agent Instructions (AGENTS.md)

This file contains instructions and context for AI agents working on the SailScores codebase.

## Authorization & Permissions

SailScores uses a granular permission system for club administration. When adding or modifying administrative features, ensure the appropriate authorization policies are applied.

### Permission Levels
- `ClubAdministrator` (0): Full access to all club features, including scorekeeper management.
- `SeriesScorekeeper` (1): Can manage series, races, and competitors.
- `RaceScorekeeper` (2): Can only manage races and competitors.

### Authorization Policies
Always prefer using the declarative `[Authorize(Policy = "...")]` attributes over manual checks where possible.

- `AuthorizationPolicies.ClubAdmin`: Limits access to `ClubAdministrator` level. Use this for:
    - User/Scorekeeper management (`UserController`)
    - Club settings and configuration
    - Deleting major entities
- `AuthorizationPolicies.SeriesScorekeeper`: Limits access to `SeriesScorekeeper` or higher. Use for:
    - Creating/Editing series
- `AuthorizationPolicies.RaceScorekeeper`: Limits access to `RaceScorekeeper` or higher. Use for:
    - Creating/Editing races
    - Managing competitors

### UI Visibility
When modifying Razor views (e.g., `Admin/Index.cshtml`), ensure that UI elements (buttons, links, sections) are hidden for users who do not have the required permission level using properties like `Model.IsClubAdmin` or `Model.CanEditSeries`.

## Coding Standards

- Follow the layered architecture: Controller -> Web Service -> Core Service -> Database.
- Always check permissions at both the UI level (visibility) and the API/Controller level (security).
