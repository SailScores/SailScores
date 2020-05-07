
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Xunit;

namespace SailScores.SeleniumTests
{
    [Trait("Club", "LHYC")]
    public class LhycAnonymousTests
    {
        private readonly SailScoresTestConfig configuration;

        public LhycAnonymousTests()
        {
            configuration = TestHelper.GetApplicationConfiguration(Environment.CurrentDirectory);
        }


        [Fact]
        public void LhycBasics()
        {
            using var driver = new ChromeDriver(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            driver.Navigate().GoToUrl(configuration.BaseUrl);

            //if logged in, log out.
            var logout = driver.FindElementsByLinkText("Log out");


            var clubSelector = new SelectElement(driver.FindElement(By.Id("clubSelect")));
            //javascript should navigate automatically.
            clubSelector.SelectByText("Lake Harriet Yacht Club");
            var currentElement = driver.WaitUntilClickable(By.PartialLinkText("LHYC"));

            Assert.Equal(configuration.BaseUrl + "/LHYC", driver.Url);

            currentElement = driver.WaitUntilClickable(By.CssSelector("a[href*='/LHYC/Series']"));
            currentElement.Click();

            currentElement = driver.WaitUntilClickable(By.CssSelector("a[href*='/LHYC/2019/MC Season Champ']"));
            currentElement.Click();

            driver.WaitUntilVisible(By.CssSelector("table.table"));
            var rows = driver.FindElements(By.CssSelector("tr"));
            Assert.True(rows.Count > 25, "At least 25 rows expected in 2019 season champ results");
            var headers = driver.FindElements(By.CssSelector("thead th"));
            Assert.True(headers.Count > 25, "At least 25 headers expected");
        }


    }
}