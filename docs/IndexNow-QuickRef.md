# IndexNow Quick Reference

## TL;DR

IndexNow automatically notifies search engines when series are updated. Hidden clubs are excluded.

## Setup (5 minutes)

1. **Generate API Key**: Use any UUID generator or create a random string (8+ chars)
2. **Create Key File**: `wwwroot/{your-key}.txt` containing just the key
3. **Add to Config**:
   ```json
   "IndexNow": { "ApiKey": "your-key-here" }
   ```
4. **Verify**: Visit `https://yoursite.com/{your-key}.txt`

## How to Disable

Remove or comment out the `IndexNow:ApiKey` setting in `appsettings.json`:
```json
"IndexNow": {
  // "ApiKey": "disabled"
}
```

## Troubleshooting

### "Skipping notification" in logs
- Missing API key → Add to config
- Missing PreferredHost → Add to config  
- Club is hidden → Expected behavior

### Notifications not working
1. Check Application Insights for errors
2. Verify key file is accessible at root URL
3. Ensure `PreferredHost` matches your domain
4. Check that club `IsHidden = false`

### Test in Development

Set breakpoint in `IndexNowService.NotifySeriesUpdate()` and update a race.

## Code Entry Points

- **Trigger**: `SeriesService.UpdateSeriesResults()`
- **Service**: `IndexNowService.NotifySeriesUpdate()`
- **Interface**: `IIndexNowService`
- **DI Registration**: `DependencyInjectionExtensions.cs`

## When IndexNow Fires

✅ Race added or updated → Series results recalculated → IndexNow called
✅ Only for non-hidden clubs
❌ Not called during bulk operations (by design - prevents spam)
❌ Not called if API key is missing (fails gracefully)

## Logs to Watch

```
IndexNow notification sent for series: {url}
Skipping IndexNow notification for hidden club {initials}
IndexNow API key not configured. Skipping notification.
```

## More Info

- Full docs: `docs/IndexNow.md`
- Implementation summary: `docs/IndexNow-Implementation-Summary.md`
- Setup guide: `wwwroot/INDEXNOW-SETUP.md`
