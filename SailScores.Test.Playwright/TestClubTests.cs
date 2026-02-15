using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;
using SailScores.Test.Playwright.Utilities;
using System;
using System.Threading.Tasks;
using Xunit;

namespace SailScores.Test.Playwright;

[Trait("Club", "TEST")]
[Trait("WritesData", "True")]
[Collection("Playwright collection")]
public class TestClubTests
{
    private readonly SailScoresTestConfig configuration;
    private readonly IBrowser browser;
    private readonly IPlaywright playwright;

    public TestClubTests(PlaywrightFixture fixture)
    {
        configuration = fixture.Configuration;
        browser = fixture.Browser;
        playwright = fixture.Playwright;
    }


    private async Task LoginAndGoToHiddenTestClubAsync(IPage page)
    {
        await page.LoginAsync(configuration.BaseUrl, configuration.TestEmail, configuration.TestPassword);
        await page.GotoAsync(UrlCombine(configuration.BaseUrl, configuration.TestClubInitials));
    }

    private string UrlCombine(string url1, string url2)
    {
        if (url1.Length == 0)
        {
            return url2;
        }
        if (url2.Length == 0)
        {
            return url1;
        }
        url1 = url1.TrimEnd('/', '\\');
        url2 = url2.TrimStart('/', '\\');
        return string.Format("{0}/{1}", url1, url2);
    }

    [Fact]
    public async Task AddAndDeleteBoatClass()
    {
        var page = await browser.NewPageAsync();
        await LoginAndGoToHiddenTestClubAsync(page);

        var sectionId = "classes";
        await page.Locator("a:has-text('Admin page')").ClickAsync();
        await page.Locator($"#{sectionId}").ClickAsync();
        var createLink = page.Locator("a:has-text('New Class')");
        await createLink.ClickAsync();
        var className = $"AutoTest Class {DateTime.Now.ToString("yyyyMMdd Hmmss")}";
        await page.Locator("#Name").FillAsync(className);
        await page.Locator("input[value='Create']").ClickAsync();
        await page.Locator("#classes").ClickAsync();
        var deleteButton = await GetDeleteButtonForRowAsync(page, sectionId, className);
        await deleteButton.ClickAsync();
        await page.Locator("input[value='Delete']").ClickAsync();
        Assert.Contains("/TEST/Admin", page.Url);
        await page.CloseAsync();
    }

    [Fact]
    public async Task AddEditDeleteCompetitor()
    {
        var page = await browser.NewPageAsync();
        await LoginAndGoToHiddenTestClubAsync(page);

        await page.Locator("a:has-text('Admin page')").ClickAsync();
        await page.Locator("a:has-text('Competitor page')").ClickAsync();
        var createLink = page.Locator("a:has-text('Create')");
        await createLink.ClickAsync();
        var compName = $"AutoTest Comp {DateTime.Now.ToString("yyyyMMdd Hmmss")}";
        await page.Locator("[name='competitors[0].Name']").FillAsync(compName);
        var rnd = new Random();
        var sailNumber = $"AT{rnd.Next(9999)}";
        await page.Locator("[name='competitors[0].SailNumber']").FillAsync(sailNumber);
        await page.Locator("[name='competitors[0].BoatName']").FillAsync("Test Boat");
        await page.Locator("#BoatClassId").SelectOptionAsync(new SelectOptionValue { Label = "Test Boat Class" });
        await page.Locator("input[value='Create']").ClickAsync();
        var editButton = page.Locator($"//div[. = '{compName}']/../../..//a[@title = 'Edit']");
        await editButton.ClickAsync();
        await page.Locator("input[value='Save']").ClickAsync();
        var deleteButton = page.Locator($"//div[contains(@class, 'container')]//div[contains(@class, 'row')]//div[contains(@class, 'row') and contains(., '{compName}')]//a[@title='Delete']");
        await deleteButton.ClickAsync();
        await page.Locator("input[value='Delete']").ClickAsync();
        Assert.EndsWith("/TEST/Competitor", page.Url);
        await page.CloseAsync();
    }

    [Fact]
    public async Task AddEditDeleteFleet()
    {
        var page = await browser.NewPageAsync();
        await LoginAndGoToHiddenTestClubAsync(page);

        var sectionName = "fleets";
        await page.Locator("a:has-text('Admin page')").ClickAsync();
        await page.Locator($"#{sectionName}").ClickAsync();
        var createLink = page.Locator("a:has-text('New Fleet')");
        await createLink.ClickAsync();
        var fleetName = $"AutoTest Fleet {DateTime.Now.ToString("yyyyMMdd Hmmss")}";
        await page.Locator("#Name").FillAsync(fleetName);
        await page.Locator("#NickName").FillAsync(fleetName);
        await page.Locator("#fleetType").SelectOptionAsync(new SelectOptionValue { Label = "Selected Boats" });
        await page.SelectOptionsByLabelHiddenAsync("#competitorIds", 
            "11111 - Alice (Test Boat Class)", 
            "22222 - Bob (Test Boat Class)");
        var submitButton = page.Locator("input[value='Create']");
        await submitButton.ScrollIntoViewIfNeededAsync();
        await submitButton.ClickAsync();
        await page.Locator($"#{sectionName}").ClickAsync();
        var editButton = await GetEditButtonForRowAsync(page, sectionName, fleetName);
        await editButton.ClickAsync();
        await page.Locator("input[value='Save']").ClickAsync();
        await page.Locator($"#{sectionName}").ClickAsync();
        var deleteButton = await GetDeleteButtonForRowAsync(page, sectionName, fleetName);
        await deleteButton.ClickAsync();
        await page.Locator("input[value='Delete']").ClickAsync();
        Assert.Contains("/TEST/Admin", page.Url);
        await page.CloseAsync();
    }

    [Fact]
    public async Task AddEditDeleteSeason()
    {
        var page = await browser.NewPageAsync();
        await LoginAndGoToHiddenTestClubAsync(page);

        await page.Locator("a:has-text('Admin page')").ClickAsync();
        var sectionName = "seasons";
        await page.Locator($"#{sectionName}").ClickAsync();
        var createLink = page.Locator("a:has-text('New Season')");
        await createLink.ClickAsync();
        var startDate = DateTime.Today.AddYears(1);
        var finishDate = DateTime.Today.AddDays(1).AddYears(1);
        var seasonName = $"Test {startDate.Year}";
        await page.Locator("#Name").FillAsync(seasonName);
        await page.Locator("#Start").FillAsync(startDate.ToString("yyyy-MM-dd"));
        await page.Locator("#End").FillAsync(finishDate.ToString("yyyy-MM-dd"));
        await page.Locator("input[value='Create']").ClickAsync();
        // Redirects to Admin Index
        await page.Locator($"#{sectionName}").ClickAsync();
        var editButton = await GetEditButtonForRowAsync(page, sectionName, seasonName);
        await editButton.ClickAsync();
        await page.Locator("input[value='Save']").ClickAsync();
        await page.Locator($"#{sectionName}").ClickAsync();
        var deleteButton = await GetDeleteButtonForRowAsync(page, sectionName, seasonName);
        await deleteButton.ClickAsync();
        await page.Locator("input[value='Delete']").ClickAsync();
        Assert.Contains("/TEST/Admin", page.Url);
        await page.CloseAsync();
    }

    [Fact]
    public async Task AddEditDeleteSeries()
    {
        var page = await browser.NewPageAsync();
        await LoginAndGoToHiddenTestClubAsync(page);

        var sectionName = "series";
        var seriesName = $"Test Series {DateTime.Now.ToString("yyyyMMdd Hmmss")}";

        await page.Locator("a:has-text('Admin page')").ClickAsync();
        await page.Locator($"#{sectionName}").ClickAsync();
        var createLink = page.Locator("a:has-text('New Series')");
        await createLink.ClickAsync();
        await page.Locator("#Name").FillAsync(seriesName);
        await page.Locator("#SeasonId").SelectOptionAsync(new SelectOptionValue { Label = DateTime.Today.Year.ToString() });
        await page.Locator("input[value='Create']").ClickAsync();
        await page.Locator($"#{sectionName}").ClickAsync();
        var editButton = await GetEditButtonForRowAsync(page, sectionName, seriesName);
        await editButton.ClickAsync();
        await page.Locator("input[value='Save']").ClickAsync();
        await page.Locator($"#{sectionName}").ClickAsync();
        var deleteButton = await GetDeleteButtonForRowAsync(page, sectionName, seriesName);
        await deleteButton.ClickAsync();
        await page.Locator("input[value='Delete']").ClickAsync();
        Assert.Contains("/TEST/Admin", page.Url);
        await page.CloseAsync();
    }

    [Fact]
    public async Task AddEditDeleteRegatta()
    {
        var page = await browser.NewPageAsync();
        await LoginAndGoToHiddenTestClubAsync(page);

        var sectionName = "regattas";
        var regattaName = $"Test Regatta {DateTime.Now.ToString("yyyyMMdd Hmmss")}";
        await page.Locator("a:has-text('Admin page')").ClickAsync();
        await page.Locator($"#{sectionName}").ClickAsync();
        var createLink = page.Locator("a:has-text('New Regatta')");
        await createLink.ClickAsync();
        await page.Locator("#Name").FillAsync(regattaName);
        await page.Locator("[name='StartDate']").FillAsync(DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd"));
        await page.Locator("[name='EndDate']").FillAsync(DateTime.Today.AddDays(1).ToString("yyyy-MM-dd"));
        await page.Locator("input[value='Create']").ClickAsync();
        await page.Locator("a:has-text('Edit Regatta')").ClickAsync();
        await page.Locator("input[value='Save']").ClickAsync();
        await page.Locator("a:has-text('Club Admin')").ClickAsync();
        await page.Locator($"#{sectionName}").ClickAsync();
        var deleteButton = await GetDeleteButtonForRowAsync(page, sectionName, regattaName);
        await deleteButton.ClickAsync();
        await page.Locator("input[value='Delete']").ClickAsync();
        Assert.Contains("/TEST/Admin", page.Url);
        await page.CloseAsync();
    }

    [Fact]
    public async Task AddRaceAndSeeResults()
    {
        var page = await browser.NewPageAsync();
        await LoginAndGoToHiddenTestClubAsync(page);

        var year = DateTime.Today.Year;
        var serieslink = await page.Locator("a:has-text('Series')").AllAsync();
        if (serieslink.Count == 0)
        {
            await page.Locator("button").First.ClickAsync();
            serieslink = await page.Locator("a:has-text('Series')").AllAsync();
        }
        await serieslink[0].ClickAsync();
        var testSeriesLink = page.Locator($"a:has-text('{year} Test Series')").First;
        await testSeriesLink.ClickAsync();
        var headerlinks = await page.Locator("th a").AllAsync();
        int raceCount = headerlinks.Count;
        await page.Locator(".navbar-brand:has-text('TEST')").ClickAsync();
        await page.Locator("a:has-text('New Race')").ClickAsync();
        await page.Locator("#fleetId").SelectOptionAsync(new SelectOptionValue { Label = "Test Boat Class Fleet" });
        await page.SelectOptionsByLabelHiddenAsync("#SeriesIds", $"{year} Test Series");
        await page.Locator("a:has-text('Optional Fields')").ClickAsync();
        int order = ++raceCount;
        var orderField = page.Locator("#InitialOrder");
        await orderField.FillAsync(order.ToString());
        var addCompElement = page.Locator("#newCompetitor");
        await addCompElement.FillAsync("11111");
        await addCompElement.PressAsync("Enter");
        await addCompElement.FillAsync("222");
        await addCompElement.PressAsync("Enter");
        await page.Locator("input[value='Create']").ClickAsync();
        await page.Locator(".navbar-brand:has-text('TEST')").ClickAsync();
        await Task.Delay(1000);
        await page.Locator($"a:has-text('{year} Test Series')").ClickAsync();
        var linkText = $"{DateTime.Today.ToString("M/d")} R{order}";
        var raceLink = page.Locator($"a:has-text('{linkText}')");
        Assert.True(await raceLink.CountAsync() > 0);
        await page.CloseAsync();
    }

    private async Task<ILocator> GetDeleteButtonForRowAsync(
        IPage page,
        string sectionId,
        string itemName)
    {
        return page.Locator($"#{sectionId}div .row").Filter(new() { HasText = itemName }).First.Locator("a[title='Delete']");
    }

    private async Task<ILocator> GetEditButtonForRowAsync(
        IPage page,
        string sectionId,
        string itemName)
    {
        return page.Locator($"#{sectionId}div .row").Filter(new() { HasText = itemName }).First.Locator("a[title='Edit']");
    }
}
