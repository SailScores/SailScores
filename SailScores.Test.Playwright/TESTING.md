# SailScores Playwright Tests - Testing Guide

This document explains how to run and debug Playwright tests for the SailScores application.

## Quick Start

### Running All Tests
```bash
dotnet test SailScores.Test.Playwright.csproj
```

### Running All Tests with Verbose Output
```bash
dotnet test SailScores.Test.Playwright.csproj -vvv
```

### Running a Specific Test
```bash
dotnet test SailScores.Test.Playwright.csproj --filter "TestMethodName"
```

Example:
```bash
dotnet test SailScores.Test.Playwright.csproj --filter "LoginAndGoToHiddenTestClubAsync"
```

### Running Tests by Category/Trait
Tests are organized with traits for easy filtering:

```bash
# Run only LHYC club tests
dotnet test SailScores.Test.Playwright.csproj --filter "Category=LHYC"

# Run only permission tests
dotnet test SailScores.Test.Playwright.csproj --filter "Category=Permissions"

# Run only read-only tests
dotnet test SailScores.Test.Playwright.csproj --filter "ReadOnly=True"
```

## VS Code Test Explorer

### Opening Test Explorer
1. Click the **Test Explorer** icon in the VS Code activity bar (usually on the left)
2. Or press `Ctrl+Shift+T` to open the test explorer

### Running Tests from Explorer
- **Run All**: Click the "Run" icon at the top of the Test Explorer
- **Run Single Test**: Hover over a test name and click the "Run" icon
- **Debug Single Test**: Hover over a test name and click the "Debug" icon
- **Run Failed Tests**: Click the "Rerun Failed Tests" icon

### Setting Breakpoints
1. Open the test file (e.g., `PermissionLevelsTests.cs`)
2. Click in the gutter next to the line number where you want to break
3. Run the test in debug mode from Test Explorer
4. Execution will pause at the breakpoint

## Configuration

### Headless Mode (Browser Window Visibility)

By default, tests run in headless mode (no visible browser window). This is suitable for CI/CD environments.

**To run tests with a visible browser window:**

#### Option 1: Environment Variable
```powershell
$env:SAILSCORES_HEADLESS = "false"
dotnet test
```

#### Option 2: appSettings.json
Edit `appSettings.json`:
```json
{
  "SailScores": {
    "Headless": false,
    ...
  }
}
```

### Screenshot Capture

Tests can capture screenshots for troubleshooting. Screenshots are saved to `bin/Debug/net10.0/screenshots/`.

**Configuration in appSettings.json:**
```json
{
  "SailScores": {
    "CaptureScreenshots": true,
    "ScreenshotPath": "bin/Debug/net10.0/screenshots"
  }
}
```

## Troubleshooting Failed Tests

### Step 1: Run Test with Verbose Output
```bash
dotnet test SailScores.Test.Playwright.csproj --filter "FailingTestName" -vvv
```

### Step 2: Check Screenshots
If `CaptureScreenshots` is enabled (true) in appSettings.json:
1. Navigate to `bin/Debug/net10.0/screenshots/`
2. Look for files prefixed with `FAILURE_` for failed test screenshots
3. Screenshots are timestamped: `FAILURE_yyyy-MM-dd_HH-mm-ss-fff_TestName.png`

### Step 3: Debug in VS Code
1. Open the failing test file
2. Set a breakpoint at the suspicious line
3. Right-click the test name in Test Explorer and select "Debug Test"
4. Inspect variables and page state at the breakpoint
5. If a browser window is open (headless=false), you can also manually inspect it

### Step 4: Manual Browser Inspection
To manually interact with the test application:
1. Set `Headless: false` in appSettings.json
2. Set a breakpoint where you want to pause
3. Run test in debug mode
4. When paused at breakpoint, interact with the visible browser window
5. Continue execution with F5 or step through with F10

## Login Test Updates

The login page now has multiple submit buttons due to external authentication providers (Google, Microsoft, Facebook).

**The test framework has been updated to:**
- Target the **primary "Log In" button** specifically (not external provider buttons)
- Use selector: `form[action*="/Account/Login"] button[type=submit]`
- Fill email and password as before

If you see "element does not handle pointer events" or similar errors during login, it likely means the selector is hitting the wrong button. See [SailScores-Playwright-Setup.md](../memories/repo/SailScores-Playwright-Setup.md) for architecture details.

## Common Issues

### "Browser not found" Error
The Playwright dependencies should auto-install browsers. If this fails:
```bash
pwsh bin/Debug/net10.0/playwright.ps1 install
```

### "Element does not handle pointer events" During Login
- Ensure screenshot files are checked for what was visible at failure
- Verify selector in PageExtensions.LoginAsync() targets correct form
- Try with `Headless: false` to see exactly what's on screen

### Tests Timeout
- Default timeout is usually sufficient, but long operations may need adjustment
- Check for console output indicating what the test was doing when it timed out

## Advanced: Custom Test Run Configuration

Create a `.vscode/launch.json` configuration for debugging:

```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": ".NET Attach",
      "type": "coreclr",
      "request": "attach",
      "processId": "${command:pickProcess}"
    }
  ]
}
```

Then use Test Explorer's Debug feature to attach.

## File Locations

- **Test files**: `*.cs` in workspace root (HomeAnonymousTests.cs, PermissionLevelsTests.cs, etc.)
- **Utilities**: `Utilities/` folder
  - `PageExtensions.cs` - Helper methods for page interactions
  - `SailScoresTestConfig.cs` - Configuration model
  - `TestHelper.cs` - Configuration loading
- **Configuration**: `appSettings.json`
- **Screenshots**: `bin/Debug/net10.0/screenshots/`
- **Project file**: `SailScores.Test.Playwright.csproj`

## Next Steps

- For test infrastructure details, see [SailScores-Playwright-Setup.md](../memories/repo/SailScores-Playwright-Setup.md)
- To add new tests, follow the pattern in existing test files
- Use `[Trait("Category", "YourCategory")]` to organize new tests
