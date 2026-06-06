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

        await page.Locator("a:has-text('Admin page')").ClickAsync();
        await ExpandSectionAsync(page, "classes");
        var createLink = page.Locator("a:has-text('New Class')");
        await createLink.ClickAsync();
        var className = $"AutoTest Class {DateTime.Now.ToString("yyyyMMdd Hmmss")}";
        await page.Locator("#Name").FillAsync(className);
        await page.Locator("input[value='Create']").ClickAsync();
        var deleteButton = await GetAdminSectionDeleteLinkAsync(page, "classes", className);
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
        await ExpandSectionAsync(page, "competitors");
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

        await page.Locator("a:has-text('Admin page')").ClickAsync();
        await ExpandSectionAsync(page, "fleets");
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
        var editButton = await GetAdminSectionEditLinkAsync(page, "fleets", fleetName);
        await editButton.ClickAsync();
        await page.Locator("input[value='Save']").ClickAsync();
        var deleteButton = await GetAdminSectionDeleteLinkAsync(page, "fleets", fleetName);
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
        await ExpandSectionAsync(page, "seasons");
        var createLink = page.Locator("a:has-text('New Season')");
        await createLink.ClickAsync();
        var startDate = DateTime.Today.AddYears(1);
        var finishDate = DateTime.Today.AddDays(1).AddYears(1);
        var seasonName = $"Test {startDate.Year}";
        await page.Locator("#Name").FillAsync(seasonName);
        await page.Locator("#Start").FillAsync(startDate.ToString("yyyy-MM-dd"));
        await page.Locator("#End").FillAsync(finishDate.ToString("yyyy-MM-dd"));
        await page.Locator("input[value='Create']").ClickAsync();
        var editButton = await GetAdminSectionEditLinkAsync(page, "seasons", seasonName);
        await editButton.ClickAsync();
        await page.Locator("input[value='Save']").ClickAsync();
        var deleteButton = await GetAdminSectionDeleteLinkAsync(page, "seasons", seasonName);
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

        var seriesName = $"Test Series {DateTime.Now.ToString("yyyyMMdd Hmmss")}";

        await page.Locator("a:has-text('Admin page')").ClickAsync();
        await ExpandSectionAsync(page, "series");
        var createLink = page.Locator("a:has-text('New Series')");
        await createLink.ClickAsync();
        await page.Locator("#Name").FillAsync(seriesName);
        await page.Locator("#SeasonId").SelectOptionAsync(new SelectOptionValue { Label = DateTime.Today.Year.ToString() });
        await page.Locator("input[value='Create']").ClickAsync();
        var editButton = await GetAdminSectionEditLinkAsync(page, "series", seriesName);
        await editButton.ClickAsync();
        await page.Locator("input[value='Save']").ClickAsync();
        var deleteButton = await GetAdminSectionDeleteLinkAsync(page, "series", seriesName);
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

        var regattaName = $"Test Regatta {DateTime.Now.ToString("yyyyMMdd Hmmss")}";
        await page.Locator("a:has-text('Admin page')").ClickAsync();
        await ExpandSectionAsync(page, "regattas");
        var createLink = page.Locator("a:has-text('New Regatta')");
        await createLink.ClickAsync();
        await page.Locator("#Name").FillAsync(regattaName);
        await page.Locator("[name='StartDate']").FillAsync(DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd"));
        await page.Locator("[name='EndDate']").FillAsync(DateTime.Today.AddDays(1).ToString("yyyy-MM-dd"));
        await page.Locator("input[value='Create']").ClickAsync();
        await page.Locator("a:has-text('Edit Regatta')").ClickAsync();
        await page.Locator("input[value='Save']").ClickAsync();
        await page.Locator("a:has-text('Club Admin')").ClickAsync();
        var deleteButton = await GetAdminSectionDeleteLinkAsync(page, "regattas", regattaName);
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
        await page.GotoAsync(UrlCombine(configuration.BaseUrl, configuration.TestClubInitials + "/Series"));

        var currentYearSeriesLinks = page.Locator($"a[href^='/{configuration.TestClubInitials}/{year}/']");
        var preferredSeriesLink = page.Locator($"a[href='/{configuration.TestClubInitials}/{year}/series-1']");
        var selectedSeriesLink = await preferredSeriesLink.CountAsync() > 0
            ? preferredSeriesLink.First
            : currentYearSeriesLinks.First;

        var selectedSeriesName = (await selectedSeriesLink.InnerTextAsync()).Trim();
        var selectedSeriesHref = await selectedSeriesLink.GetAttributeAsync("href");
        Assert.False(string.IsNullOrWhiteSpace(selectedSeriesHref));
        await selectedSeriesLink.ClickAsync();
        var raceCount = await page.Locator("a[href*='/Race/Details/']").CountAsync();

        await page.Locator(".navbar-brand:has-text('TEST')").ClickAsync();
        await page.Locator("a:has-text('New Race')").ClickAsync();
        await page.Locator("#fleetId").SelectOptionAsync(new SelectOptionValue { Label = "Test Boat Class Fleet" });
        await page.EvalOnSelectorAsync("#SeriesIds", @"(el, seriesName) => {
            const target = (seriesName || '').trim().toLowerCase();
            for (let i = 0; i < el.options.length; i++) {
                const opt = el.options[i];
                const optText = (opt.text || opt.label || '').trim().toLowerCase();
                opt.selected = optText === target || optText.endsWith(` ${target}`) || optText.endsWith(`- ${target}`);
            }
            el.dispatchEvent(new Event('change', { bubbles: true }));
        }", selectedSeriesName);
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

        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        Assert.DoesNotContain("/Race/Create", page.Url);
        var createdRacePath = new Uri(page.Url).AbsolutePath;

        var selectedSeriesUrl = UrlCombine(configuration.BaseUrl, selectedSeriesHref!);
        await page.GotoAsync(selectedSeriesUrl);
        await Assertions.Expect(page.Locator($"a[href='{createdRacePath}']")).ToHaveCountAsync(1);
        await page.CloseAsync();
    }

    private async Task ExpandSectionAsync(IPage page, string sectionName)
    {
        await GetAdminSectionToggleAsync(page, sectionName).ClickAsync();
        await page.WaitForTimeoutAsync(300);
    }

    private ILocator GetAdminSectionToggleAsync(IPage page, string sectionName)
    {
        var buttonText = char.ToUpper(sectionName[0]) + sectionName.Substring(1);
        return page.Locator($"button:has-text('{buttonText}')");
    }

    private async Task<ILocator> GetAdminSectionItemContainerAsync(
        IPage page,
        string itemName)
    {
        return page.GetByText(itemName, new() { Exact = false })
            .Locator("xpath=ancestor::div[.//a[starts-with(@href, '/TEST/')]][1]")
            .First;
    }

    private async Task<ILocator> GetAdminSectionEditLinkAsync(
        IPage page,
        string sectionName,
        string itemName)
    {
        await ExpandSectionAsync(page, sectionName);
        var row = await GetAdminSectionItemContainerAsync(page, itemName);
        return row.Locator("a[title='Edit'], a[href*='/Edit/']").First;
    }

    private async Task<ILocator> GetAdminSectionDeleteLinkAsync(
        IPage page,
        string sectionName,
        string itemName)
    {
        await ExpandSectionAsync(page, sectionName);
        var row = await GetAdminSectionItemContainerAsync(page, itemName);
        return row.Locator("a[title='Delete'], a[href*='/Delete/']").First;
    }
}
