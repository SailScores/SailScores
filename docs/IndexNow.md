# IndexNow Integration

This document describes the IndexNow integration in SailScores.

## Overview

IndexNow is a protocol that allows websites to instantly notify search engines when content is updated. SailScores uses IndexNow to notify search engines when series results are updated, helping keep series pages indexed and current in search results.

## Implementation

### Components

1. **IIndexNowService** (`SailScores.Core/Services/Interfaces/IIndexNowService.cs`)
   - Interface for the IndexNow notification service

2. **IndexNowService** (`SailScores.Core/Services/IndexNowService.cs`)
   - Implementation of the IndexNow notification service
   - Sends HTTP POST requests to `https://api.indexnow.org/indexnow`
   - Checks if club is hidden before sending notifications
   - Handles errors gracefully without breaking the update flow

3. **SeriesService** (`SailScores.Core/Services/SeriesService.cs`)
   - Updated to inject `IIndexNowService`
   - Calls `NotifyIndexNow` after series results are updated
   - Includes error handling to prevent IndexNow failures from affecting series updates

### Configuration

Add the following to `appsettings.json`:

```json
{
  "IndexNow": {
    "ApiKey": "your-indexnow-api-key-here"
  },
  "PreferredHost": "www.sailscores.com"
}
```

**Note:** The `PreferredHost` setting is already used by other parts of SailScores (e.g., sitemap generation). IndexNow uses the same configuration to ensure consistency.

### API Key File

According to IndexNow protocol requirements, you must host a text file at the root of your domain containing just the API key:

1. Create a file named `{your-api-key}.txt` (replace with your actual key)
2. Place it in `SailScores.Web/wwwroot/`
3. The file should contain only the API key with no additional content

Example: If your API key is `abc123def456`, create `wwwroot/abc123def456.txt` containing:
```
abc123def456
```

## How It Works

1. When a race is added or updated, `RaceService.SaveAsync` triggers `SeriesService.UpdateSeriesResults`
2. `UpdateSeriesResults` calculates scores and saves results
3. After saving chart data, it calls `NotifyIndexNow`
4. `NotifyIndexNow` retrieves club and series information from the database
5. If the club is not hidden, `IndexNowService.NotifySeriesUpdate` is called
6. The service sends a POST request to IndexNow API with the series URL
7. Search engines (Bing, Google, etc.) are notified of the updated content

## Hidden Clubs

Clubs marked as `IsHidden = true` will NOT trigger IndexNow notifications. This is useful for:
- Test clubs
- Private clubs
- Development/staging environments

## Error Handling

- IndexNow failures are logged but do not prevent series updates from completing
- Missing API key configuration logs a warning and skips notification
- HTTP errors are logged with status code details
- All exceptions are caught and logged

## Testing

The implementation includes mocked `IIndexNowService` in unit tests:
- `SeriesServiceTests` includes a mock to verify the service is called correctly
- Tests ensure IndexNow integration doesn't break existing functionality

## Search Engine Support

IndexNow is supported by:
- Microsoft Bing
- Yandex
- Naver
- Seznam.cz
- Google (participating as of 2024)

When you submit a URL to one IndexNow endpoint, it's shared with all participating search engines.

## Monitoring

Monitor IndexNow effectiveness through:
- Application Insights logs (search for "IndexNow")
- Search engine webmaster tools
- Indexing speed improvements in search results

## Future Enhancements

Potential improvements:
- Batch notifications for multiple series updates
- Notify for regatta updates
- Notify for competitor page updates
- Queue notifications for retry on failure
- Rate limiting for high-volume clubs
