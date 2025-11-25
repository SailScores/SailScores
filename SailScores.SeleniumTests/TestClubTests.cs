using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;
using System;
using System.Threading.Tasks;
using Xunit;

namespace SailScores.SeleniumTests
{
    [Trait("Club", "TEST")]
    public class TestClubTests : PageTest
    {
        private readonly SailScoresTestConfig configuration;

        public TestClubTests()
        {
            configuration = TestHelper.GetApplicationConfiguration(Environment.CurrentDirectory);
        }

        private async Task LoginAndGoToHiddenTestClubAsync()
        {
            await Page.GotoAsync(configuration.BaseUrl);
            await Page.Locator("a:has-text('Log in')").ClickAsync();
            await Page.Locator("#Email").FillAsync(configuration.TestEmail);
            await Page.Locator("#Password").FillAsync(configuration.TestPassword);
            await Page.Locator("form input[type='submit'], form button[type='submit']").ClickAsync();
            await Page.GotoAsync(UrlCombine(configuration.BaseUrl, configuration.TestClubInitials));
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
            var sectionId = "classes";
            await LoginAndGoToHiddenTestClubAsync();
            await Page.Locator("a:has-text('Admin Page')").ClickAsync();
            await Page.Locator($"#{sectionId}").ClickAsync();
            var createLink = Page.Locator("a:has-text('New Class')");
            await createLink.ClickAsync();
            var className = $"AutoTest Class {DateTime.Now.ToString("yyyyMMdd Hmmss")}";
            await Page.Locator("#Name").FillAsync(className);
            await Page.Locator("input[value='Create']").ClickAsync();
            await Page.Locator("#classes").ClickAsync();
            var deleteButton = await GetDeleteButtonForRowAsync(sectionId, className);
            await deleteButton.ClickAsync();
            await Page.Locator("input[value='Delete']").ClickAsync();
            Assert.Contains("/TEST/Admin", Page.Url);
        }

        [Fact]
        public async Task AddEditDeleteCompetitor()
        {
            await LoginAndGoToHiddenTestClubAsync();
            await Page.Locator("a:has-text('Admin Page')").ClickAsync();
            await Page.Locator("a:has-text('Competitor page')").ClickAsync();
            var createLink = Page.Locator("a:has-text('Create')");
            await createLink.ClickAsync();
            var compName = $"AutoTest Comp {DateTime.Now.ToString("yyyyMMdd Hmmss")}";
            await Page.Locator("[name='competitors[0].Name']").FillAsync(compName);
            var rnd = new Random();
            var sailNumber = $"AT{rnd.Next(9999)}";
            await Page.Locator("[name='competitors[0].SailNumber']").FillAsync(sailNumber);
            await Page.Locator("[name='competitors[0].BoatName']").FillAsync("Test Boat");
            await Page.Locator("#BoatClassId").SelectOptionAsync(new SelectOptionValue { Label = "Test Boat Class" });
            await Page.Locator("input[value='Create']").ClickAsync();
            var editButton = Page.Locator($"//div[. = '{compName}']/../../..//a[@title = 'Edit']");
            await editButton.ClickAsync();
            await Page.Locator("input[value='Save']").ClickAsync();
            var deleteButton = Page.Locator($"//div[contains(@class, 'container')]//div[contains(@class, 'row')]//div[contains(@class, 'row') and contains(., '{compName}')]//a[@title='Delete']");
            await deleteButton.ClickAsync();
            await Page.Locator("input[value='Delete']").ClickAsync();
            Assert.EndsWith("/TEST/Competitor", Page.Url);
        }

        [Fact]
        public async Task AddEditDeleteFleet()
        {
            var sectionName = "fleets";
            await LoginAndGoToHiddenTestClubAsync();
            await Page.Locator("a:has-text('Admin Page')").ClickAsync();
            await Page.Locator($"#{sectionName}").ClickAsync();
            var createLink = Page.Locator("a:has-text('New Fleet')");
            await createLink.ClickAsync();
            var fleetName = $"AutoTest Fleet {DateTime.Now.ToString("yyyyMMdd Hmmss")}";
            await Page.Locator("#Name").FillAsync(fleetName);
            await Page.Locator("#NickName").FillAsync(fleetName);
            await Page.Locator("#fleetType").SelectOptionAsync(new SelectOptionValue { Label = "Selected Boats" });
            await Page.Locator("#CompetitorIds").SelectOptionAsync(new[] {
                new SelectOptionValue { Label = "11111 - Alice (Test Boat Class)" },
                new SelectOptionValue { Label = "22222 - Bob (Test Boat Class)" }
            });
            var submitButton = Page.Locator("input[value='Create']");
            await submitButton.ScrollIntoViewIfNeededAsync();
            await submitButton.ClickAsync();
            await Page.Locator($"#{sectionName}").ClickAsync();
            var editButton = await GetEditButtonForRowAsync(sectionName, fleetName);
            await editButton.ClickAsync();
            await Page.Locator("input[value='Save']").ClickAsync();
            await Page.Locator($"#{sectionName}").ClickAsync();
            var deleteButton = await GetDeleteButtonForRowAsync(sectionName, fleetName);
            await deleteButton.ClickAsync();
            await Page.Locator("input[value='Delete']").ClickAsync();
            Assert.Contains("/TEST/Admin", Page.Url);
        }

        [Fact]
        public async Task AddEditDeleteSeason()
        {
            await LoginAndGoToHiddenTestClubAsync();
            await Page.Locator("a:has-text('Admin Page')").ClickAsync();
            var sectionName = "seasons";
            await Page.Locator($"#{sectionName}").ClickAsync();
            var createLink = Page.Locator("a:has-text('New Season')");
            await createLink.ClickAsync();
            var startDate = DateTime.Today.AddYears(-5);
            var finishDate = DateTime.Today.AddDays(1).AddYears(-5);
            var seasonName = $"Test {startDate.Year}";
            await Page.Locator("#Name").FillAsync(seasonName);
            await Page.Locator("#Start").FillAsync(startDate.ToString("MM/dd/yyyy"));
            await Page.Locator("#End").FillAsync(finishDate.ToString("MM/dd/yyyy"));
            await Page.Locator("input[value='Create']").ClickAsync();
            await Page.Locator($"#{sectionName}").ClickAsync();
            var editButton = await GetEditButtonForRowAsync(sectionName, seasonName);
            await editButton.ClickAsync();
            await Page.Locator("input[value='Save']").ClickAsync();
            await Page.Locator($"#{sectionName}").ClickAsync();
            var deleteButton = await GetDeleteButtonForRowAsync(sectionName, seasonName);
            await deleteButton.ClickAsync();
            await Page.Locator("input[value='Delete']").ClickAsync();
            Assert.Contains("/TEST/Admin", Page.Url);
        }

        [Fact]
        public async Task AddEditDeleteSeries()
        {
            var sectionName = "series";
            var seriesName = $"Test Series {DateTime.Now.ToString("yyyyMMdd Hmmss")}";
            await LoginAndGoToHiddenTestClubAsync();
            await Page.Locator("a:has-text('Admin Page')").ClickAsync();
            await Page.Locator($"#{sectionName}").ClickAsync();
            var createLink = Page.Locator("a:has-text('New Series')");
            await createLink.ClickAsync();
            await Page.Locator("#Name").FillAsync(seriesName);
            await Page.Locator("#SeasonId").SelectOptionAsync(new SelectOptionValue { Label = DateTime.Today.Year.ToString() });
            await Page.Locator("input[value='Create']").ClickAsync();
            await Page.Locator($"#{sectionName}").ClickAsync();
            var editButton = await GetEditButtonForRowAsync(sectionName, seriesName);
            await editButton.ClickAsync();
            await Page.Locator("input[value='Save']").ClickAsync();
            await Page.Locator($"#{sectionName}").ClickAsync();
            var deleteButton = await GetDeleteButtonForRowAsync(sectionName, seriesName);
            await deleteButton.ClickAsync();
            await Page.Locator("input[value='Delete']").ClickAsync();
            Assert.Contains("/TEST/Admin", Page.Url);
        }

        [Fact]
        public async Task AddEditDeleteRegatta()
        {
            var sectionName = "regattas";
            var regattaName = $"Test Regatta {DateTime.Now.ToString("yyyyMMdd Hmmss")}";
            await LoginAndGoToHiddenTestClubAsync();
            await Page.Locator("a:has-text('Admin Page')").ClickAsync();
            await Page.Locator($"#{sectionName}").ClickAsync();
            var createLink = Page.Locator("a:has-text('New Regatta')");
            await createLink.ClickAsync();
            await Page.Locator("#Name").FillAsync(regattaName);
            await Page.Locator("[name='StartDate']").FillAsync(DateTime.Today.AddDays(-1).ToString("MM/dd/yyyy"));
            await Page.Locator("[name='EndDate']").FillAsync(DateTime.Today.AddDays(1).ToString("MM/dd/yyyy"));
            await Page.Locator("input[value='Create']").ClickAsync();
            await Page.Locator("a:has-text('Edit Regatta')").ClickAsync();
            await Page.Locator("input[value='Save']").ClickAsync();
            await Page.Locator("a:has-text('Club Admin')").ClickAsync();
            await Page.Locator($"#{sectionName}").ClickAsync();
            var deleteButton = await GetDeleteButtonForRowAsync(sectionName, regattaName);
            await deleteButton.ClickAsync();
            await Page.Locator("input[value='Delete']").ClickAsync();
            Assert.Contains("/TEST/Admin", Page.Url);
        }

        [Fact]
        public async Task AddRaceAndSeeResults()
        {
            var year = DateTime.Today.Year;
            await LoginAndGoToHiddenTestClubAsync();
            var serieslink = await Page.Locator("a:has-text('Series')").AllAsync();
            if (serieslink.Count == 0)
            {
                await Page.Locator("button").First.ClickAsync();
                serieslink = await Page.Locator("a:has-text('Series')").AllAsync();
            }
            await serieslink[0].ClickAsync();
            var testSeriesLink = Page.Locator($"a:has-text('{year} Test Series')");
            await testSeriesLink.ClickAsync();
            var headerlinks = await Page.Locator("th a").AllAsync();
            int raceCount = headerlinks.Count;
            await Page.Locator("a:has-text('TEST')").ClickAsync();
            await Page.Locator("a:has-text('New Race')").ClickAsync();
            await Page.Locator("#fleetId").SelectOptionAsync(new SelectOptionValue { Label = "Test Boat Class Fleet" });
            await Page.Locator("#seriesIds").SelectOptionAsync(new SelectOptionValue { Label = $"{year} Test Series" });
            await Page.Locator("a:has-text('Optional Fields')").ClickAsync();
            int order = ++raceCount;
            var orderField = Page.Locator("#InitialOrder");
            await orderField.FillAsync(order.ToString());
            var addCompElement = Page.Locator("#newCompetitor");
            await addCompElement.FillAsync("11111");
            await addCompElement.PressAsync("Enter");
            await addCompElement.FillAsync("222");
            await addCompElement.PressAsync("Enter");
            await Page.Locator("input[value='Create']").ClickAsync();
            await Page.Locator("a:has-text('TEST')").ClickAsync();
            await Task.Delay(1000);
            await Page.Locator($"a:has-text('{year} Test Series')").ClickAsync();
            var linkText = $"{DateTime.Today.ToString("M/d")} R{order}";
            var raceLink = Page.Locator($"a:has-text('{linkText}')");
            Assert.True(await raceLink.CountAsync() > 0);
        }

        private async Task<ILocator> GetDeleteButtonForRowAsync(string sectionId, string itemName)
        {
            return Page.Locator(GetButtonSelector("Delete", sectionId, itemName));
        }
        private async Task<ILocator> GetEditButtonForRowAsync(string sectionId, string itemName)
        {
            return Page.Locator(GetButtonSelector("Edit", sectionId, itemName));
        }
        private string GetButtonSelector(string buttonTitle, string sectionId, string itemInRowText)
        {
            return $"//div[@id=\"{sectionId}div\"]//div[contains(string(), \"{itemInRowText}\")]//a[@title=\"{buttonTitle}\"]";
        }
    }
}