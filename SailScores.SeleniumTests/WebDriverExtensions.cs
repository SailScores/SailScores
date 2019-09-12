using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.Text;

namespace SailScores.SeleniumTests
{
    public static class WebDriverExtensions
    {

        // use: element = driver.WaitUntilVisible(By.XPath("//input[@value='Save']"));
        public static IWebElement WaitUntilVisible(
            this IWebDriver driver,
            By itemSpecifier,
            int secondsTimeout = 10)
        {
            var wait = new WebDriverWait(driver, new TimeSpan(0, 0, secondsTimeout));
            var element = wait.Until<IWebElement>(driver =>
            {
                try
                {
                    var elementToBeDisplayed = driver.FindElement(itemSpecifier);
                    if(elementToBeDisplayed.Displayed)
                    {
                        return elementToBeDisplayed;
                    }
                    return null;
                }
                catch (StaleElementReferenceException)
                {
                    return null;
                }
                catch (NoSuchElementException)
                {
                    return null;
                }

            });
            return element;
        }

        public static IWebElement WaitUntilClickable(
            this IWebDriver driver,
            By itemSpecifier,
            int secondsTimeout = 10)
        {
            var wait = new WebDriverWait(driver, new TimeSpan(0, 0, secondsTimeout));
            var element = wait.Until<IWebElement>(driver =>
            {
                try
                {
                    var elementToBeFound = driver.FindElement(itemSpecifier);
                    if (elementToBeFound.Displayed && elementToBeFound.Enabled)
                    {
                        return elementToBeFound;
                    }
                    return null;
                }
                catch (StaleElementReferenceException)
                {
                    return null;
                }
                catch (NoSuchElementException)
                {
                    return null;
                }

            });
            return element;
        }
    }
}
