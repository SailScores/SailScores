using Microsoft.Playwright;
using System;
using System.Threading.Tasks;

namespace SailScores.Test.Playwright.Utilities;

public static class PageExtensions
{
    public static async Task EnsureLoggedOutAsync(this IPage page)
    {
        // Check for "Log out" link or button in the nav bar or footer
        var logoutLink = page.Locator("#navbarNav >> text=Log out, #logoutForm >> text=Log out");
        if (await logoutLink.CountAsync() > 0)
        {
            await logoutLink.First.ClickAsync();
        }
    }

    public static async Task LoginAsync(this IPage page, string loginUrl, string email, string password)
    {
        await page.GotoAsync(loginUrl);
        
        // Use a more specific locator for the Log in link in the nav bar to avoid strict mode violation
        await page.Locator("#navbarNav").GetByRole(AriaRole.Link, new() { Name = "Log in" }).ClickAsync();
        
        await page.Locator("#Email").FillAsync(email);
        await page.Locator("#Password").FillAsync(password);
        // Target the primary form's submit button specifically. The login page has two separate forms:
        // 1. Primary form (form[action*="/Account/Login"]) - email/password with this button
        // 2. External auth form (form[action*="/Account/ExternalLogin"]) - Google/Microsoft/Facebook buttons
        // Using the specific selector prevents clicking the wrong button on pages with multiple submit buttons.
        await page.Locator("form[action*=\"/Account/Login\"] button[type=submit]").ClickAsync();
    }

    /// <summary>
    /// Captures a screenshot with timestamp. Ensures screenshot directory exists.
    /// </summary>
    public static async Task CaptureScreenshotAsync(this IPage page, string screenshotPath, string testName)
    {
        if (string.IsNullOrWhiteSpace(screenshotPath))
            return;

        var dir = System.IO.Path.GetDirectoryName(screenshotPath);
        if (!System.IO.Directory.Exists(dir))
            System.IO.Directory.CreateDirectory(dir);

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff");
        var filename = $"{timestamp}_{testName}.png";
        var fullPath = System.IO.Path.Combine(screenshotPath, filename);

        await page.ScreenshotAsync(new PageScreenshotOptions { Path = fullPath });
    }

    /// <summary>
    /// Captures a screenshot on test failure for troubleshooting.
    /// </summary>
    public static async Task CaptureScreenshotOnFailureAsync(this IPage page, string screenshotPath, string testName, Exception exception)
    {
        if (string.IsNullOrWhiteSpace(screenshotPath))
            return;

        try
        {
            var dir = System.IO.Path.GetDirectoryName(screenshotPath);
            if (!System.IO.Directory.Exists(dir))
                System.IO.Directory.CreateDirectory(dir);

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff");
            var filename = $"FAILURE_{timestamp}_{testName}.png";
            var fullPath = System.IO.Path.Combine(screenshotPath, filename);

            await page.ScreenshotAsync(new PageScreenshotOptions { Path = fullPath });
            System.Console.WriteLine($"Screenshot captured: {fullPath}");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Failed to capture screenshot: {ex.Message}");
        }
    }

    public static async Task SelectOptionByLabelAsync(this IPage page, string selector, string label)
    {
        var locator = page.Locator(selector);
        await locator.WaitForAsync();
        await locator.SelectOptionAsync(new SelectOptionValue { Label = label });
    }

    public static async Task ClickAndNavigateAsync(this IPage page, string clickSelector, string expectedUrlPattern)
    {
        await Task.WhenAll(
            page.WaitForURLAsync(expectedUrlPattern),
            page.Locator(clickSelector).ClickAsync()
        );
    }

    public static async Task SelectOptionsByLabelHiddenAsync(this IPage page, string selector, params string[] labels)
    {
        await page.Locator(selector).WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Attached });
        await page.EvalOnSelectorAsync(selector, @"(el, labels) => {
            const labelsArray = (Array.isArray(labels) ? labels : [labels]).map(l => l.trim().toLowerCase());
            for (let i = 0; i < el.options.length; i++) {
                const opt = el.options[i];
                const optText = (opt.text || opt.label || '').trim().toLowerCase();
                if (labelsArray.includes(optText)) {
                    opt.selected = true;
                }
            }
            el.dispatchEvent(new Event('change', { bubbles: true }));
        }", labels);
    }
}
