using Microsoft.Playwright;
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
        await page.Locator("form input[type='submit'], form button[type='submit']").ClickAsync();
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
