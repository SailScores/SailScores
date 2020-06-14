
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using System.IO;
using System.Reflection;
using System.Threading;
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

        [Trait("Read Only", "True")]
        [Fact]
        public void LhycSeries()
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
            Thread.Sleep(300);

            currentElement = driver.WaitUntilClickable(By.LinkText("Series"));
            currentElement.Click();
            Thread.Sleep(300);

            currentElement = driver.WaitUntilClickable(By.CssSelector("a[href*='LHYC/2019/MC Season Champ']"));
            currentElement.Click();

            driver.WaitUntilVisible(By.CssSelector("table.table"));
            var rows = driver.FindElements(By.CssSelector("tr"));
            Assert.True(rows.Count > 25, "At least 25 rows expected in 2019 season champ results");
            var headers = driver.FindElements(By.CssSelector("thead th"));
            Assert.True(headers.Count > 25, "At least 25 headers expected");
        }

        [Trait("Read Only", "True")]
        [Fact]
        public void LhycRace()
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
            Thread.Sleep(300);

            currentElement = driver.WaitUntilClickable(By.LinkText("Races"));
            currentElement.Click();

            currentElement = driver.FindElementByXPath("//*[@id='racelink_faceec7e-d3db-4a5c-964d-9846cf64a009']");
            currentElement.Click();

            // Relying on these to throw exception if not found.
            driver.FindElement(By.XPath("//*[contains(.,'Colburn, Bill')]"));
            driver.FindElement(By.XPath("//*[contains(.,'Mango')]"));
        }

        [Trait("Read Only", "True")]
        [Fact]
        public void LhycRegatta()
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
            Thread.Sleep(300);

            currentElement = driver.WaitUntilClickable(By.LinkText("Regattas"));
            currentElement.Click();

            currentElement = driver.WaitUntilClickable(By.XPath("//*[@id='regattalink_6f2e4bfe-8d0d-41b5-485e-08d732752bb6']"));
            currentElement.Click();

            currentElement = driver.WaitUntilVisible(By.XPath("//*[@id='Results2019DieHardMC']/div/table/tbody/tr[1]/th[4]"));
            Assert.Contains("Grosch", currentElement.Text);
        }

    }
}