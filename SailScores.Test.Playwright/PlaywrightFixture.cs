using Microsoft.Playwright;
using SailScores.Test.Playwright.Utilities;
using System;
using System.Threading.Tasks;
using Xunit;


namespace SailScores.Test.Playwright;

public class PlaywrightFixture : IAsyncLifetime
{
    public IBrowser Browser { get; private set; }
    public IPlaywright Playwright { get; private set; }
    public SailScoresTestConfig Configuration { get; private set; }
    public IBrowserContext Context { get; private set; }

    public async Task InitializeAsync()
    {
        Configuration = TestHelper.GetApplicationConfiguration(Environment.CurrentDirectory);
        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = Configuration.Headless
        });
        Context = await Browser.NewContextAsync();
        Context.SetDefaultTimeout(15000);
        Context.SetDefaultNavigationTimeout(15000);
        Assertions.SetDefaultExpectTimeout(15000);
    }

    public async Task DisposeAsync()
    {
        if (Context != null)
            await Context.CloseAsync();
        if (Browser != null)
            await Browser.CloseAsync();
        Playwright?.Dispose();
    }
}

[CollectionDefinition("Playwright collection")]
public class PlaywrightCollection : ICollectionFixture<PlaywrightFixture>
{
    // No code needed here
}
