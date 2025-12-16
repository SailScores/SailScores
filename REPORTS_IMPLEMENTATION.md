# Reports Section Implementation Guide

## Overview

This document describes the implementation of the Reports section for SailScores, which provides analytical insights for sailing club scorekeepers.

## Feature Summary

The Reports section includes three interactive reports:

1. **Wind Analysis** - Radial scatter chart showing wind direction vs speed
2. **Skipper Statistics** - Comprehensive participation and performance metrics
3. **Participation Trends** - Time-series analysis of distinct skippers by fleet

## Access & Security

- **URL Pattern**: `/{clubInitials}/Reports/`
- **Authentication**: Requires logged-in user
- **Authorization**: User must have edit permissions for the club (scorekeeper role)
- **Navigation**: Accessible from Admin page sidebar

## Architecture

### Service Layer Pattern

```
┌─────────────┐
│   Browser   │
└──────┬──────┘
       │
       ↓
┌──────────────────┐
│ ReportsController│ ← Authorization checks
└──────┬───────────┘
       │
       ↓
┌─────────────────────┐
│ Web.IReportService  │ ← View model transformation
└──────┬──────────────┘
       │
       ↓
┌──────────────────────┐
│ Core.IReportService  │ ← Business logic & queries
└──────┬───────────────┘
       │
       ↓
┌────────────────┐
│ EF Core / SQL  │ ← Data access
└────────────────┘
```

### File Structure

```
SailScores.Core/
├── Services/
│   ├── Interfaces/
│   │   └── IReportService.cs          # Core service interface
│   └── ReportService.cs                # Core business logic
└── Extensions/
    └── DependencyInjectionExtensions.cs # Service registration

SailScores.Web/
├── Controllers/
│   └── ReportsController.cs            # HTTP endpoints
├── Services/
│   ├── Interfaces/
│   │   └── IReportService.cs          # Web service interface
│   └── ReportService.cs                # View model transformation
├── Models/SailScores/
│   ├── ReportsIndexViewModel.cs
│   ├── WindAnalysisViewModel.cs
│   ├── SkipperStatsViewModel.cs
│   └── ParticipationViewModel.cs
└── Views/Reports/
    ├── Index.cshtml                    # Landing page
    ├── WindAnalysis.cshtml             # Chart.js scatter plot
    ├── SkipperStats.cshtml             # DataTables table
    ├── Participation.cshtml            # Chart.js bar chart
    └── README.md                       # Detailed documentation

SailScores.Test.Unit/
└── Core/Services/
    └── ReportServiceTests.cs           # Unit tests
```

## Report Details

### 1. Wind Analysis

**Purpose**: Visualize wind patterns from race days

**Data Processing**:
- Queries races with associated weather records
- Groups by date (one data point per day)
- Averages wind speed and direction for races on same day
- Supports date range filtering

**Visualization**:
- Chart.js scatter plot
- X-axis: Wind direction (0-360 degrees)
- Y-axis: Wind speed (m/s)
- Tooltip: Date, direction, speed, race count

**Use Cases**:
- Identify prevailing wind patterns
- Plan race courses based on historical data
- Validate weather data quality

### 2. Skipper Statistics

**Purpose**: Track individual skipper participation and performance

**Metrics Calculated**:
- **Races Participated**: Distinct races where skipper competed
- **Total Fleet Races**: All races in skipper's fleet during period
- **Boats Beat**: Count of distinct competitors finished behind in any race
- **Participation %**: (Races Participated / Total Fleet Races) × 100

**Features**:
- Sortable and searchable (DataTables)
- Grouped by fleet
- Date range filtering
- Paginated results (25 per page default)

**Use Cases**:
- Identify most active sailors
- Track participation rates
- Recognize competitive sailors
- Recruitment insights

### 3. Participation Trends

**Purpose**: Monitor club activity over time

**Grouping Options**:
- **Day**: Daily participation counts
- **Week**: Weekly aggregation (ISO 8601 weeks)
- **Month**: Monthly aggregation

**Data Processing**:
- Counts distinct skippers per period
- Separates by fleet
- Supports date range filtering

**Visualization**:
- Chart.js grouped bar chart
- Different colors per fleet
- Includes data table view

**Use Cases**:
- Track growth/decline trends
- Identify seasonal patterns
- Compare fleet activity
- Event planning insights

## Technical Details

### Core Service Methods

```csharp
Task<IList<WindDataPoint>> GetWindDataAsync(
    Guid clubId, 
    DateTime? startDate = null, 
    DateTime? endDate = null)

Task<IList<SkipperStatistics>> GetSkipperStatisticsAsync(
    Guid clubId, 
    DateTime? startDate = null, 
    DateTime? endDate = null)

Task<IList<ParticipationMetric>> GetParticipationMetricsAsync(
    Guid clubId, 
    string groupBy = "month",
    DateTime? startDate = null, 
    DateTime? endDate = null)
```

### Authorization Pattern

All report actions follow this pattern:

```csharp
[Authorize]
public async Task<ActionResult> ReportName(string clubInitials, ...)
{
    if (!await _authService.CanUserEdit(User, clubInitials))
    {
        return Unauthorized();
    }
    
    // ... report logic
}
```

### Date Filtering

All reports support optional date range:
- `?startDate=2024-01-01` - Include data from this date onwards
- `?endDate=2024-12-31` - Include data up to this date
- No parameters = all-time data

### Frontend Libraries

- **Bootstrap 5**: Styling and responsive layout
- **Chart.js 4.x**: Interactive charts
- **DataTables 1.13.x**: Sortable/searchable tables
- **jQuery 3.7.x**: DOM manipulation

## Testing

### Unit Tests

Location: `SailScores.Test.Unit/Core/Services/ReportServiceTests.cs`

Tests cover:
- Basic data retrieval
- Date range filtering
- Different grouping options
- Empty result handling

### Manual Testing Checklist

- [ ] Access Reports from Admin page
- [ ] Verify authentication required
- [ ] Verify authorization checks (can only access own club)
- [ ] Test Wind Analysis chart rendering
- [ ] Test date filtering on all reports
- [ ] Test Skipper Stats sorting and searching
- [ ] Test Participation grouping (day/week/month)
- [ ] Test responsive design on mobile
- [ ] Verify chart interactions (tooltips, zoom)

## Deployment Considerations

### Database

No schema changes required. Reports use existing tables:
- `Races`
- `Scores`
- `Competitors`
- `Fleets`
- `Weather`

### Performance

- Queries use EF Core with `.Include()` for eager loading
- Results are not cached (fresh data on each request)
- Date filtering reduces query scope
- Consider adding indexes on:
  - `Races.ClubId, Races.Date`
  - `Scores.RaceId, Scores.CompetitorId`

### Configuration

No additional configuration needed. Uses existing:
- Authentication/authorization infrastructure
- Database connection
- Club route constraint

## Future Enhancements

Potential additions:
1. Export to PDF/Excel
2. Scheduled email reports
3. Custom date range presets
4. Additional metrics (average finish, improvement trends)
5. Fleet comparison overlays
6. Caching for expensive queries
7. Admin configuration for visible reports
8. Report scheduling/automation

## Support

For questions or issues:
1. Check the Views/Reports/README.md for detailed usage
2. Review code comments in service implementations
3. Examine unit tests for expected behavior
4. Refer to this document for architecture overview
