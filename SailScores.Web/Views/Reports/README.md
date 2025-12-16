# Reports Section

## Overview

The Reports section provides analytical insights for sailing club scorekeepers, featuring wind analysis, skipper statistics, and participation trends.

## Access

- **URL Pattern**: `/{clubInitials}/Reports/`
- **Authentication**: Requires logged-in user with scorekeeper/edit permissions for the club
- **Navigation**: Available from Admin page sidebar under "Reports"

## Reports Available

### 1. Wind Analysis (`/Reports/WindAnalysis`)

Displays wind speed and direction data from race days as an interactive scatter chart.

**Features**:
- Radial scatter plot showing wind direction (degrees) vs wind speed (m/s)
- Each data point represents one race day (races on same day are averaged)
- Date range filtering
- Interactive tooltips showing date, direction, speed, and race count
- Uses Chart.js for visualization

**Data Source**: Weather records associated with races

### 2. Skipper Statistics (`/Reports/SkipperStats`)

Comprehensive statistics for each skipper in the club.

**Features**:
- List of all skippers with metrics:
  - Races participated
  - Total fleet races (during date range)
  - Boats beat (distinct competitors)
  - Participation percentage
- Sortable and searchable table (DataTables)
- Date range filtering
- Grouped by fleet

**Calculations**:
- **Boats Beat**: Count of distinct competitors that this skipper has finished ahead of in any race
- **Participation %**: (Races Participated / Total Fleet Races) × 100

### 3. Participation Trends (`/Reports/Participation`)

Tracks the number of distinct skippers racing over time.

**Features**:
- Bar chart showing participation by fleet
- Group by day, week, or month
- Date range filtering
- Data table view
- Uses Chart.js for visualization

**Use Cases**:
- Track growth/decline in participation
- Identify seasonal patterns
- Compare fleet activity

## Date Filtering

All reports support optional date range filtering:
- **Start Date**: Include races from this date onwards
- **End Date**: Include races up to this date
- Leave blank for all-time data

## Technical Implementation

### Architecture

```
Request Flow:
Browser → ReportsController → Web.IReportService → Core.IReportService → Database

Response Flow:
Database → Core DTOs → Web ViewModels → Razor Views → Browser
```

### Core Services

**Location**: `SailScores.Core/Services/ReportService.cs`

Key methods:
- `GetWindDataAsync(clubId, startDate?, endDate?)`: Aggregates weather data by race day
- `GetSkipperStatisticsAsync(clubId, startDate?, endDate?)`: Calculates competitor metrics
- `GetParticipationMetricsAsync(clubId, groupBy, startDate?, endDate?)`: Groups participation by time period

### Web Services

**Location**: `SailScores.Web/Services/ReportService.cs`

Transforms Core DTOs to view models with club context.

### Controller

**Location**: `SailScores.Web/Controllers/ReportsController.cs`

All actions require `[Authorize]` and validate user has edit permissions for the club via `IAuthorizationService.CanUserEdit()`.

### Views

**Location**: `SailScores.Web/Views/Reports/`

- Uses Bootstrap 5 for styling
- Chart.js 4.x for interactive charts
- DataTables for sortable tables
- Responsive design

## Future Enhancements

Potential additions:
- Export reports to PDF/Excel
- Scheduled email reports
- Custom date range presets (last month, last season, etc.)
- Additional metrics (average finish position, improvement trends)
- Fleet comparison overlays
- Weather correlation with participation
