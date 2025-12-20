using Microsoft.Playwright;
using SailScores.DocScreenshots.Models;
using SailScores.DocScreenshots.Utils;
using Microsoft.Extensions.Configuration;

namespace SailScores.DocScreenshots.Services;

internal class ScreenshotService : IAsyncDisposable
{
    private readonly IPlaywright _playwright;
    private readonly IBrowser _browser;
    private readonly string? _loginEmail;
    private readonly string? _loginPassword;

    private ScreenshotService(IPlaywright playwright, IBrowser browser, string? loginEmail, string? loginPassword)
    {
        _playwright = playwright;
        _browser = browser;
        _loginEmail = loginEmail;
        _loginPassword = loginPassword;
    }

    public static async Task<ScreenshotService> CreateAsync()
    {
        var playwright = await Playwright.CreateAsync();
        var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });

        // Build configuration to read from user-secrets and environment variables
        var configBuilder = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .AddUserSecrets(typeof(ScreenshotService).Assembly, optional: true);

        var config = configBuilder.Build();

        // Prefer user-secrets/project secrets if present, otherwise fall back to environment variables
        var email = config["DocScreenshots:Email"] ?? config["DOCSCREENSHOT_EMAIL"];
        var password = config["DocScreenshots:Password"] ?? config["DOCSCREENSHOT_PASSWORD"];

        Console.WriteLine($"Using email: {email}");

        return new ScreenshotService(playwright, browser, email, password);
    }

    public async Task CaptureAsync(PageInfo pageInfo, string outputDir)
    {
        await using var context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1024, Height = 900 }
        });

        var page = await context.NewPageAsync();

        // If credentials provided, ensure we're logged in on this origin before capturing
        if (!string.IsNullOrWhiteSpace(_loginEmail) && !string.IsNullOrWhiteSpace(_loginPassword))
        {
            await EnsureLoggedInAsync(page, pageInfo);
        }

        await page.GotoAsync(pageInfo.Url, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 60000 });

        // Ensure fonts and dynamic content finish loading
        await page.WaitForTimeoutAsync(1000);

        // Light mode
        await SetColorScheme(page, "light");
        var lightFilename = FileNameHelper.BuildFileName(pageInfo, "light");
        var lightPath = Path.Combine(outputDir, lightFilename);
        var lightOptions = BuildScreenshotOptions(pageInfo);
        lightOptions.Path = lightPath;
        await page.ScreenshotAsync(lightOptions);

        // Dark mode
        await SetColorScheme(page, "dark");
        await page.WaitForTimeoutAsync(500);
        var darkFilename = FileNameHelper.BuildFileName(pageInfo, "dark");
        var darkPath = Path.Combine(outputDir, darkFilename);
        Console.WriteLine($"output path = {outputDir}");
        var darkOptions = BuildScreenshotOptions(pageInfo);
        darkOptions.Path = darkPath;
        await page.ScreenshotAsync(darkOptions);

        await page.CloseAsync();
    }

    private static PageScreenshotOptions BuildScreenshotOptions(PageInfo pageInfo)
    {
        if (pageInfo.Region is null)
            return new PageScreenshotOptions { FullPage = true };

        return new PageScreenshotOptions
        {
            FullPage = false,
            Clip = new Clip
            {
                X = pageInfo.Region.X,
                Y = pageInfo.Region.Y,
                Width = pageInfo.Region.Width,
                Height = pageInfo.Region.Height
            }
        };
    }

    private static async Task SetColorScheme(IPage page, string scheme)
    {
        await page.EmulateMediaAsync(new PageEmulateMediaOptions { ColorScheme = scheme == "dark" ? ColorScheme.Dark : ColorScheme.Light });
    }

    private async Task EnsureLoggedInAsync(IPage page, PageInfo pageInfo)
    {
        try
        {
            // Derive base URI from the page URL to ensure we target the same origin for login
            var uri = new Uri(pageInfo.Url);
            var baseUri = uri.GetLeftPart(UriPartial.Authority);
            var homeUrl = baseUri + "/";

            await page.GotoAsync(homeUrl, new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 60000 });

            // If a "Log in" link is present, perform login
            var loginLink = page.Locator("text=Log in");
            if (await loginLink.CountAsync() > 0 && await loginLink.IsVisibleAsync())
            {
                await loginLink.ClickAsync();
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                // Fill credentials. Login form uses inputs with ids Email and Password
                await page.FillAsync("#Email", _loginEmail!);
                await page.FillAsync("#Password", _loginPassword!);

                // Submit the form
                var submit = page.Locator("form input[type=submit], form button[type=submit]");
                if (await submit.CountAsync() > 0)
                {
                    await submit.First.ClickAsync();
                }
                else
                {
                    // fallback: press Enter in password field
                    await page.Keyboard.PressAsync("Enter");
                }

                // Wait for navigation to complete after login
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

                // small delay to allow UI to update
                await page.WaitForTimeoutAsync(500);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Login attempt failed: {ex.Message}");
            // Don't fail the whole screenshot process for login errors; continue anonymously
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _browser.CloseAsync();
        _playwright.Dispose();
    }
}
