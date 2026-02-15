using Microsoft.Playwright;
using Microsoft.Playwright.Xunit;
using SailScores.Test.Playwright.Utilities;
using System;
using System.Threading.Tasks;
using Xunit;

namespace SailScores.Test.Playwright;

[Trait("Club", "None")]
public class HomeAnonymousTests : PageTest
{
    private readonly SailScoresTestConfig configuration;

    public HomeAnonymousTests()
    {
        configuration = TestHelper.GetApplicationConfiguration(Environment.CurrentDirectory);
    }

    [Trait("Read Only", "True")]
    [Fact]
    public async Task HomePage_HasClubCards()
    {
        await Page.GotoAsync(configuration.BaseUrl);

        await Page.EnsureLoggedOutAsync();

        var lhycCard = Page.Locator(".club-card[data-club-name='lake harriet yacht club']");
        await Expect(lhycCard).ToBeVisibleAsync();
    }

    [Trait("Read Only", "True")]
    [Fact]
    public async Task AboutAndNews_Load()
    {
        await Page.GotoAsync(configuration.BaseUrl);

        await Page.EnsureLoggedOutAsync();

        await Page.Locator("a:has-text('About')").ClickAsync();
        var aboutHeading = Page.Locator("//h1[contains(.,'kept simple')]");
        await Expect(aboutHeading).ToBeVisibleAsync(); // auto-waits for visibility

        await Page.Locator("a:has-text('News')").ClickAsync();
        var newsHeading = Page.Locator("//h5[contains(.,'Something feels different')]");
        await Expect(newsHeading).ToBeVisibleAsync(); // auto-waits for visibility
    }

    [Fact]
    public async Task FillOutClubRequest_Load()
    {
        await Page.GotoAsync(configuration.BaseUrl);

        await Page.EnsureLoggedOutAsync();

        int num = new Random().Next(1000);
        await Page.Locator("a:has-text('Try it out')").ClickAsync();
        await Page.Locator("#ClubName").ClickAsync();
        await Page.Locator("#ClubName").FillAsync($"asdf{num}");
        await Page.Locator("#ClubInitials").ClickAsync();
        await Page.Locator("#ClubInitials").FillAsync($"ASDF{num}");
        await Page.Locator("#ClubLocation").FillAsync("somewhere");
        await Page.Locator("#ClubWebsite").FillAsync("http://www.google.com");
        await Page.Locator("#ContactFirstName").FillAsync("Jamie");
        await Page.Locator("#ContactLastName").FillAsync("Fraser Test");
        await Page.Locator("#ContactEmail").FillAsync($"test{num}@jamie.com");

        await Page.Locator("#Password").FillAsync("P@ssw0rd");
        await Page.Locator("#ConfirmPassword").FillAsync("P@ssw0rd");
        await Page.Locator("#Classes").FillAsync("MC ");
        await Page.Locator("#Comments").FillAsync("Nothing");
        await Page.Locator(".btn-success").ClickAsync();

        var successHeading = Page.Locator("//h1[contains(.,'Club Created')]");
        Assert.True(await successHeading.CountAsync() > 0);
    }
}
