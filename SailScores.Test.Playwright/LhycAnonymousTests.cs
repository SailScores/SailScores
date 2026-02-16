using Microsoft.Playwright;
using SailScores.Test.Playwright.Utilities;
using System;
using System.Threading.Tasks;
using Xunit;

namespace SailScores.Test.Playwright;

[Trait("Club", "LHYC")]
[Collection("Playwright collection")]
public class LhycAnonymousTests
{
    private readonly SailScoresTestConfig configuration;
    private readonly IBrowser browser;
    private readonly IPlaywright playwright;

    public LhycAnonymousTests(PlaywrightFixture fixture)
    {
        configuration = fixture.Configuration;
        browser = fixture.Browser;
        playwright = fixture.Playwright;
    }

    [Trait("Read Only", "True")]
    [Fact]
    public async Task LhycSeries()
    {
        var page = await browser.NewPageAsync();
        await page.GotoAsync(configuration.BaseUrl);

        await page.GetByText("Lake Harriet Yacht Club").First.ClickAsync(new() { Force = true });
        await page.WaitForURLAsync("**/LHYC");

        Assert.EndsWith("/LHYC", page.Url);

        await page.GetByRole(AriaRole.Link, new() { Name = "Series", Exact = true }).ClickAsync();
        await page.Locator("a[href*='LHYC/2019/MC Season Champ']").ClickAsync();

        Assert.True(await page.GetByRole(AriaRole.Table, new() { Name = "Results for series" }).IsVisibleAsync());
        var rows = await page.Locator("tr").AllAsync();
        Assert.True(rows.Count > 25, "At least 25 rows expected in 2019 season champ results");
        var headers = await page.Locator("thead th").AllAsync();
        Assert.True(headers.Count > 25, "At least 25 headers expected");
        await page.CloseAsync();
    }

    [Trait("Read Only", "True")]
    [Fact]
    public async Task LhycRace()
    {
        var page = await browser.NewPageAsync();
        await page.GotoAsync(configuration.BaseUrl);

        await page.GetByText("Lake Harriet Yacht Club").First.ClickAsync(new() { Force = true });
        await page.WaitForURLAsync("**/LHYC");

        Assert.EndsWith("/LHYC", page.Url);
        await Task.Delay(300);

        await page.GetByText("Races", new() { Exact = true }).ClickAsync();
        await page.GetByRole(AriaRole.Link, new() { Name = "2020" }).ClickAsync();
        await page.Locator("#racelink_5e191bc2-04aa-4c5a-8a19-76b1484a95bb").ClickAsync();

        var groschElement = page.Locator("//*[contains(.,'Grosch, Ryan')]");
        Assert.True(await groschElement.CountAsync() > 0);
        var blackCatElement = page.Locator("//*[contains(.,'Black Cat')]");
        Assert.True(await blackCatElement.CountAsync() > 0);
        await page.CloseAsync();
    }

    [Trait("Read Only", "True")]
    [Fact]
    public async Task LhycRegatta()
    {
        var page = await browser.NewPageAsync();
        await page.GotoAsync(configuration.BaseUrl);

        await page.GetByText("Lake Harriet Yacht Club").First.ClickAsync(new() { Force = true });
        await page.WaitForURLAsync("**/LHYC");

        Assert.EndsWith("/LHYC", page.Url);
        await Task.Delay(300);

        await page.Locator("a:has-text('Regattas')").ClickAsync();
        await page.Locator("a[href*= '/2020/DieHard']").ClickAsync();

        // check that the page contains expected racer
        Assert.True(await page.GetByRole(AriaRole.Rowheader, new() { Name = "Grosch, Ryan" })
            .IsVisibleAsync());
        await page.CloseAsync();
    }
}
