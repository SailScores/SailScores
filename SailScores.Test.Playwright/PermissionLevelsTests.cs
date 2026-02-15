using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;
using SailScores.Test.Playwright.Utilities;
using System;
using System.Threading.Tasks;
using Xunit;

namespace SailScores.Test.Playwright;

/* 
 * SETUP INSTRUCTIONS:
 * 1. Ensure 'race-scorekeeper@widernets.com' exists in the database with 'Race Scorekeeper' role for the test club.
 * 2. Ensure 'series-scorekeeper@widernets.com' exists in the database with 'Series Scorekeeper' role for the test club.
 * 3. Both users should have the password defined in SailScores:TestPassword in appsettings.json.
 */

[Trait("Category", "Permissions")]
[Collection("Playwright collection")]
public class PermissionLevelsTests
{
    private readonly SailScoresTestConfig configuration;
    private readonly IBrowser browser;

    public PermissionLevelsTests(PlaywrightFixture fixture)
    {
        configuration = fixture.Configuration;
        browser = fixture.Browser;
    }

    private string UrlCombine(string url1, string url2)
    {
        url1 = url1.TrimEnd('/', '\\');
        url2 = url2.TrimStart('/', '\\');
        return $"{url1}/{url2}";
    }

    [Fact]
    public async Task RaceScorekeeper_CanAccessRaceEdit_ButNotSeriesEdit()
    {
        // ARRANGE - Use a specific account that only has RaceScorekeeper permissions
        // Preparation: Ensure this user is assigned 'Race Scorekeeper' level in the club admin
        string raceScorekeeperEmail = configuration.RaceScorekeeperEmail;
        string password = configuration.PermissionTestPassword;

        var page = await browser.NewPageAsync();
        await page.LoginAsync(configuration.BaseUrl, raceScorekeeperEmail, password);

        // ACT & ASSERT - Check Races
        await page.GotoAsync(UrlCombine(configuration.BaseUrl, configuration.TestClubInitials + "/Race"));
        // Race Scorekeeper SHOULD see 'New Race' button and 'Edit' buttons
        await Assertions.Expect(page.Locator("main a:has-text('New Race')")).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator("main a[title='Edit']").First).ToBeVisibleAsync();

        // ACT & ASSERT - Check Series on Admin page
        await page.GotoAsync(UrlCombine(configuration.BaseUrl, configuration.TestClubInitials + "/Admin"));
        // Race Scorekeeper should NOT see 'New Series' button
        var newSeriesButton = page.Locator("a:has-text('New Series')");
        await Assertions.Expect(newSeriesButton).Not.ToBeVisibleAsync();

        // Race Scorekeeper should NOT see Series 'Edit' pencil buttons
        var editSeriesLink = page.Locator(".admin-series-row .fa-pen");
        await Assertions.Expect(editSeriesLink).Not.ToBeVisibleAsync();

        await page.CloseAsync();
    }

    [Fact]
    public async Task SeriesScorekeeper_CanAccessBoth_RaceAndSeriesEdit()
    {
        // ARRANGE - Use a specific account that only has SeriesScorekeeper permissions
        // Preparation: Ensure this user is assigned 'Series Scorekeeper' level in the club admin
        string seriesScorekeeperEmail = configuration.SeriesScorekeeperEmail;
        string password = configuration.PermissionTestPassword;

        var page = await browser.NewPageAsync();
        await page.LoginAsync(configuration.BaseUrl, seriesScorekeeperEmail, password);

        // ACT & ASSERT - Check Races
        await page.GotoAsync(UrlCombine(configuration.BaseUrl, configuration.TestClubInitials + "/Race"));
        await Assertions.Expect(page.Locator("main a:has-text('New Race')")).ToBeVisibleAsync();
        await Assertions.Expect(page.Locator("a[title='Edit']").First).ToBeVisibleAsync();

        // ACT & ASSERT - Check Series on Admin page
        await page.GotoAsync(UrlCombine(configuration.BaseUrl, configuration.TestClubInitials + "/Admin"));
        // Series Scorekeeper SHOULD see 'New Series' button
        await Assertions.Expect(page.Locator("a:has-text('New Series')")).ToBeVisibleAsync();
        // Series Scorekeeper SHOULD see Series 'Edit' pencil buttons
        await Assertions.Expect(page.Locator(".admin-series-row .fa-pen").First).ToBeVisibleAsync();

        await page.CloseAsync();
    }
}
