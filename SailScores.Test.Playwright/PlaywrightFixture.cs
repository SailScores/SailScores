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

    public async Task InitializeAsync()
    {
        Configuration = TestHelper.GetApplicationConfiguration(Environment.CurrentDirectory);
        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = Configuration.Headless
        });
    }

    public async Task DisposeAsync()
    {
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
