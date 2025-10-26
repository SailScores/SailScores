# System Alert Banner Feature

## Overview

This feature provides a dismissable banner system for displaying system-wide alerts, such as upcoming deployment downtime notifications. The alerts appear at the top of all pages (both main site and club pages) just below the header.

## Database Schema

The feature uses a new `SystemAlerts` table with the following columns:

- `Id` (uniqueidentifier, PK) - Unique identifier for the alert
- `Content` (nvarchar(max)) - The alert message in Markdown format
- `ExpiresUtc` (datetime2) - UTC timestamp after which the alert will no longer be shown
- `CreatedDateUtc` (datetime2) - UTC timestamp when the alert was created
- `CreatedBy` (nvarchar(128)) - Username of who created the alert
- `IsDeleted` (bit) - Soft delete flag

## How to Use

### Creating an Alert

To create a new deployment alert, insert a record directly into the `SystemAlerts` table:

```sql
INSERT INTO SystemAlerts (Id, Content, ExpiresUtc, CreatedDateUtc, CreatedBy, IsDeleted)
VALUES (
    NEWID(),
    'Scheduled maintenance on **[DAY], [DATE]** from **[START_TIME] to [END_TIME] [TIMEZONE]**. The site will be unavailable during this time.',
    '[EXPIRATION_UTC_DATETIME]',  -- Alert expires after deployment completes (e.g., '2025-11-04 03:00:00')
    GETUTCDATE(),
    'admin',
    0
);
```

### Content Formatting

The `Content` field supports full Markdown syntax, including:

- **Bold text** using `**text**`
- *Italic text* using `*text*`
- Links using `[text](url)`
- Lists, headers, and more

Example with formatting:
```markdown
⚠️ **Scheduled Deployment**

We will be deploying updates on **[DAY], [DATE]** from **[START_TIME] to [END_TIME] [TIMEZONE]**.

During this time:
- The site will be temporarily unavailable
- All data will be preserved
- Updates will include performance improvements

Thank you for your patience!
```

### Alert Display Logic

Alerts are displayed when:
- `IsDeleted` is `false` (or 0)
- `ExpiresUtc` is greater than the current UTC time

Multiple active alerts can be shown simultaneously, and they are ordered by creation date.

### User Interaction

- Users can dismiss an alert by clicking the X button
- Dismissed alerts remain hidden for the duration of the user's session (using browser sessionStorage)
- The alert will reappear in a new session if it's still active

### Removing an Alert

To stop showing an alert before its expiration:

```sql
UPDATE SystemAlerts
SET IsDeleted = 1
WHERE Id = 'your-alert-id';
```

Or update the expiration:

```sql
UPDATE SystemAlerts
SET ExpiresUtc = GETUTCDATE()
WHERE Id = 'your-alert-id';
```

## Security

- All markdown content is sanitized using HtmlSanitizer before rendering to prevent XSS attacks
- Alert IDs are passed via data attributes to avoid script injection
- User dismissal state is stored client-side only and cannot affect other users

## Technical Details

### Architecture

- **Database Layer**: `SystemAlert` entity in `SailScores.Database`
- **Core Layer**: `SystemAlertService` with `ISystemAlertService` interface
- **Web Layer**: `SystemAlertService` wrapper and `_SystemAlerts.cshtml` partial view
- **UI Integration**: Injected into `_Layout.cshtml` and `_ClubLayout.cshtml`

### Migration

The database migration `20251026153533_AddSystemAlert.cs` creates the SystemAlerts table. Apply it using:

```bash
dotnet ef database update --project SailScores.Database --startup-project SailScores.Web
```

## Future Enhancements

Potential improvements for future versions:

- Admin UI for creating/managing alerts
- Support for targeting specific clubs
- Scheduling alerts in advance
- Alert templates for common scenarios
- Email notifications option
