using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;
using System;
using System.Threading.Tasks;
using Xunit;

namespace SailScores.SeleniumTests
{
    [Trait("Club", "LHYC")]
    public class LhycAnonymousTests : PageTest
    {
        private readonly SailScoresTestConfig configuration;

        public LhycAnonymousTests()
        {
            configuration = TestHelper.GetApplicationConfiguration(Environment.CurrentDirectory);
        }


        [Trait("Read Only", "True")]
        [Fact]
        public async Task LhycSeries()
        {
            await Page.GotoAsync(configuration.BaseUrl);

            // JavaScript should navigate automatically
            await Page.Locator("#clubSelect").SelectOptionAsync(new SelectOptionValue { Label = "Lake Harriet Yacht Club" });
            await Page.Locator("a:text-matches('LHYC', 'i')").First.WaitForAsync();

            Assert.Equal(configuration.BaseUrl + "/LHYC", Page.Url);

            await Page.Locator("a:has-text('Series')").ClickAsync();
            await Page.Locator("a[href*='LHYC/2019/MC Season Champ']").ClickAsync();

            await Page.Locator("table.table").WaitForAsync();
            var rows = await Page.Locator("tr").AllAsync();
            Assert.True(rows.Count > 25, "At least 25 rows expected in 2019 season champ results");
            var headers = await Page.Locator("thead th").AllAsync();
            Assert.True(headers.Count > 25, "At least 25 headers expected");
        }

        [Trait("Read Only", "True")]
        [Fact]
        public async Task LhycRace()
        {
            await Page.GotoAsync(configuration.BaseUrl);

            await Page.Locator("#clubSelect").SelectOptionAsync(new SelectOptionValue { Label = "Lake Harriet Yacht Club" });
            await Page.Locator("a:text-matches('LHYC', 'i')").First.WaitForAsync();

            Assert.Equal(configuration.BaseUrl + "/LHYC", Page.Url);
            await Task.Delay(300);

            await Page.Locator("a:has-text('Races')").ClickAsync();
            await Page.Locator("a:has-text('2020')").ClickAsync();
            await Page.Locator("#racelink_5e191bc2-04aa-4c5a-8a19-76b1484a95bb").ClickAsync();

            var groschElement = Page.Locator("//*[contains(.,'Grosch, Ryan')]");
            Assert.True(await groschElement.CountAsync() > 0);
            var blackCatElement = Page.Locator("//*[contains(.,'Black Cat')]");
            Assert.True(await blackCatElement.CountAsync() > 0);
        }

        [Trait("Read Only", "True")]
        [Fact]
        public async Task LhycRegatta()
        {
            await Page.GotoAsync(configuration.BaseUrl);

            await Page.Locator("#clubSelect").SelectOptionAsync(new SelectOptionValue { Label = "Lake Harriet Yacht Club" });
            await Page.Locator("a:text-matches('LHYC', 'i')").First.WaitForAsync();

            Assert.Equal(configuration.BaseUrl + "/LHYC", Page.Url);
            await Task.Delay(300);

            await Page.Locator("a:has-text('Regattas')").ClickAsync();
            await Page.Locator("a[href*= '/2020/DieHard']").ClickAsync();

            var currentElement = Page.Locator("//*[contains(.,'Grosch, Ryan')]");
            await currentElement.WaitForAsync();

            var elementText = await currentElement.TextContentAsync();
            Assert.Contains("Grosch", elementText);
        }
    }
}