using Microsoft.Playwright;
using System;
using System.Threading.Tasks;

namespace SailScores.SeleniumTests
{
    public static class PageExtensions
    {
        private static readonly int _basicTimeout = 20000; // milliseconds

        // Wait until element is visible
        public static async Task<ILocator> WaitUntilVisibleAsync(
            this IPage page,
            string selector,
            int? millisecondsTimeout = null)
        {
            var locator = page.Locator(selector);
            await locator.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = millisecondsTimeout ?? _basicTimeout
            });
            return locator;
        }

        // Wait until element is clickable (visible and enabled)
        public static async Task<ILocator> WaitUntilClickableAsync(
            this IPage page,
            string selector,
            int? millisecondsTimeout = null)
        {
            var locator = page.Locator(selector);
            await locator.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = millisecondsTimeout ?? _basicTimeout
            });
            // Ensure it's also enabled
            await page.WaitForFunctionAsync(
                $"document.querySelector('{selector.Replace("'", "\\'")}')?.disabled === false",
                new PageWaitForFunctionOptions
                {
                    Timeout = millisecondsTimeout ?? _basicTimeout
                });
            return locator;
        }

        // Convert Selenium By locators to Playwright selectors
        public static string ToPlaywrightSelector(this string selectorType, string value)
        {
            return selectorType switch
            {
                "Id" => $"#{value}",
                "Name" => $"[name='{value}']",
                "ClassName" => $".{value}",
                "CssSelector" => value,
                "XPath" => value,
                "LinkText" => $"a:has-text('{value}')",
                "PartialLinkText" => $"a:text-matches('{value}', 'i')",
                _ => value
            };
        }
    }
}
