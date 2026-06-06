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
        
        // Check if Series section exists at all
        var seriesSectionButton = page.Locator("button:has-text('Series')");
        var seriesSectionExists = await seriesSectionButton.CountAsync() > 0;
        
        if (seriesSectionExists)
        {
            // Expand the Series section
            await seriesSectionButton.ClickAsync();
            
            // Race Scorekeeper should NOT see 'New Series' button
            var newSeriesButton = page.Locator("a:has-text('New Series')");
            await Assertions.Expect(newSeriesButton).ToHaveCountAsync(0);

            // Race Scorekeeper should NOT see Series 'Edit' links (which link to /Series/Edit/{id})
            var editSeriesLink = page.Locator("a[href*='/Series/Edit/']");
            await Assertions.Expect(editSeriesLink).ToHaveCountAsync(0);
        }
        else
        {
            // If Series section doesn't exist at all for race scorekeepers, that's also acceptable
            // It means they have no access to series management at all
            await Assertions.Expect(seriesSectionButton).ToHaveCountAsync(0);
        }

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
        
        // Expand the Series section
        await page.Locator("button:has-text('Series')").ClickAsync();
        
        // Series Scorekeeper SHOULD see 'New Series' button
        await Assertions.Expect(page.Locator("a:has-text('New Series')")).ToBeVisibleAsync();
        // Series Scorekeeper SHOULD see Series 'Edit' links (which link to /Series/Edit/{id})
        await Assertions.Expect(page.Locator("a[href*='/Series/Edit/']").First).ToBeVisibleAsync();

        await page.CloseAsync();
    }
}
