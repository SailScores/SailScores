
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
    public class Tests
    {

        private SailScoresTestConfig configuration;

        public Tests()
        {
            configuration = TestHelper.GetApplicationConfiguration(Environment.CurrentDirectory);
        }


        [Fact]
        public void LhycBasics()
        {
            using (var driver = new ChromeDriver(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)))
            {

                driver.Navigate().GoToUrl("https://sailscores.com");

                //if logged in, log out.
                var logout = driver.FindElementsByLinkText("Log out");



                var clubSelector = new SelectElement(driver.FindElement(By.Id("clubSelect")));
                //javascript should navigate automatically.
                clubSelector.SelectByText("Lake Harriet Yacht Club");
                var currentElement = driver.WaitUntilClickable(By.PartialLinkText("LHYC"));

                Assert.Equal("https://sailscores.com/LHYC", driver.Url);

                currentElement = driver.FindElement(By.LinkText("Series"));
                currentElement.Click();

                currentElement = driver.FindElement(By.CssSelector("a[href*='/2019/MC Season Champ']"));
                currentElement.Click();

                var rows = driver.FindElements(By.CssSelector("tr"));
                Assert.True(rows.Count > 25, "At least 25 rows expected in 2019 season champ results");
                var headers = driver.FindElements(By.CssSelector("thead th"));
                Assert.True(headers.Count > 25, "At least 25 headers expected");

            }
        }


        private void LoginAndGoToHiddenTestClub(IWebDriver driver)
        {
            //navigate to home page.
            driver.Navigate().GoToUrl("https://sailscores.com");

            //log in
            driver.FindElement(By.LinkText("Log in")).Click();
            driver.FindElement(By.Id("Email")).SendKeys(configuration.TestEmail);
            driver.FindElement(By.Id("Password")).SendKeys(configuration.TestPassword);
            driver.FindElement(By.CssSelector("form")).Submit();

            // go to (hidden) test club
            driver.Url = $"{driver.Url}/{configuration.TestClubInitials}";
        }

        [Fact]
        public void AddAndDeleteBoatClass()
        {
            using (var driver = new ChromeDriver(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)))
            {
                LoginAndGoToHiddenTestClub(driver);
                driver.FindElement(By.LinkText("Admin Page")).Click();
                driver.FindElement(By.Id("classes")).Click();

                var createLink = driver.WaitUntilClickable(By.LinkText("Create"));
                createLink.Click();
                var className = $"AutoTest Class {DateTime.Now.ToString("yyyyMMdd Hmmss")}";
                driver.FindElement(By.Id("Name")).SendKeys(className);

                var submitButton = driver.FindElement(By.XPath("//input[@value='Create']"));
                submitButton.Click();
                
                driver.FindElement(By.Id("classes")).Click();

                var deleteButton = driver.WaitUntilVisible(
                    By.XPath($"//tr/td[contains(text(), '{className}')]/..//a[contains(text(),'Delete')]"));

                deleteButton.Click();

                submitButton = driver.FindElement(By.XPath("//input[@value='Delete']"));
                submitButton.Click();

                Assert.True(driver.Url.Contains("/TEST/Admin"), "Failed to delete boat class");
            }
        }

        [Fact]
        public void AddEditDeleteCompetitor()
        {
            using (var driver = new ChromeDriver(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)))
            {
                LoginAndGoToHiddenTestClub(driver);
                driver.FindElement(By.LinkText("Admin Page")).Click();
                driver.FindElement(By.Id("competitors")).Click();

                var createLink = driver.WaitUntilClickable(By.LinkText("Create"));
                createLink.Click();
                var compName = $"AutoTest Comp {DateTime.Now.ToString("yyyyMMdd Hmmss")}";
                driver.FindElement(By.Id("Name")).SendKeys(compName);

                var rnd = new Random();
                var sailNumber = $"AT{rnd.Next(9999)}";
                driver.FindElement(By.Id("SailNumber")).SendKeys(sailNumber);
                driver.FindElement(By.Id("BoatName")).SendKeys("Test Boat");

                var classSelector = new SelectElement(driver.FindElement(By.Id("BoatClassId")));
                classSelector.SelectByText("Test Boat Class");

                var submitButton = driver.FindElement(By.XPath("//input[@value='Create']"));
                submitButton.Click();

                driver.FindElement(By.Id("competitors")).Click();


                var editButton = driver.WaitUntilVisible(
                    By.XPath($"//tr/td[contains(text(), '{compName}')]/..//a[contains(text(),'Edit')]"));
                editButton.Click();

                submitButton = driver.FindElement(By.XPath("//input[@value='Save']"));
                submitButton.Click();

                driver.FindElement(By.Id("competitors")).Click();

                var deleteButton = driver.WaitUntilVisible(
                    By.XPath($"//tr/td[contains(text(), '{compName}')]/..//a[contains(text(),'Delete')]"));
                deleteButton.Click();

                submitButton = driver.FindElement(By.XPath("//input[@value='Delete']"));
                submitButton.Click();

                Assert.True(driver.Url.Contains("/TEST/Admin"), "Failed to delete competitor");
            }
        }

        [Fact]
        public void AddEditDeleteFleet()
        {
            using (var driver = new ChromeDriver(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)))
            {
                LoginAndGoToHiddenTestClub(driver);
                driver.FindElement(By.LinkText("Admin Page")).Click();
                driver.FindElement(By.Id("fleets")).Click();

                var createLink = driver.WaitUntilClickable(By.LinkText("Create"));
                createLink.Click();
                var fleetName = $"AutoTest Fleet {DateTime.Now.ToString("yyyyMMdd Hmmss")}";
                driver.FindElement(By.Id("Name")).SendKeys(fleetName);
                driver.FindElement(By.Id("ShortName")).SendKeys(fleetName);
                driver.FindElement(By.Id("NickName")).SendKeys(fleetName);
                
                var typeSelector = new SelectElement(driver.FindElement(By.Id("fleetType")));
                typeSelector.SelectByText("Selected Boats");


                var boatSelector = new SelectElement(driver.FindElement(By.Id("CompetitorIds")));
                boatSelector.SelectByText("11111 - Alice (Test Boat Class)");
                boatSelector.SelectByText("22222 - Bob (Test Boat Class)");


                var submitButton = driver.FindElement(By.XPath("//input[@value='Create']"));
                submitButton.Click();

                driver.FindElement(By.Id("fleets")).Click();


                var editButton = driver.WaitUntilVisible(
                    By.XPath($"//tr/td[contains(text(), '{fleetName}')]/..//a[contains(text(),'Edit')]"));
                editButton.Click();

                submitButton = driver.FindElement(By.XPath("//input[@value='Save']"));
                submitButton.Click();

                driver.FindElement(By.Id("fleets")).Click();

                var deleteButton = driver.WaitUntilVisible(
                    By.XPath($"//tr/td[contains(text(), '{fleetName}')]/..//a[contains(text(),'Delete')]"));
                deleteButton.Click();

                submitButton = driver.FindElement(By.XPath("//input[@value='Delete']"));
                submitButton.Click();

                Assert.True(driver.Url.Contains("/TEST/Admin"), "Failed to delete fleet");
            }
        }

        [Fact]
        public void AddEditDeleteSeason()
        {
            using (var driver = new ChromeDriver(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)))
            {
                LoginAndGoToHiddenTestClub(driver);
                driver.FindElement(By.LinkText("Admin Page")).Click();
                var sectionName = "seasons";
                driver.FindElement(By.Id(sectionName)).Click();

                var createLink = driver.WaitUntilClickable(By.LinkText("Create"));
                createLink.Click();
                var startDate = DateTime.Today.AddYears(-5);
                var finishDate = DateTime.Today.AddDays(1).AddYears(-5);
                var seasonName = $"Test {startDate.Year}";
                driver.FindElement(By.Id("Name")).SendKeys(seasonName);
                driver.FindElement(By.Id("Start")).SendKeys(startDate.ToShortDateString());
                driver.FindElement(By.Id("End")).SendKeys(finishDate.ToShortDateString());


                var submitButton = driver.FindElement(By.XPath("//input[@value='Create']"));
                submitButton.Click();

                driver.FindElement(By.Id(sectionName)).Click();


                var editButton = driver.WaitUntilVisible(
                    By.XPath($"//tr/td[contains(text(), '{seasonName}')]/..//a[contains(text(),'Edit')]"));
                editButton.Click();

                submitButton = driver.FindElement(By.XPath("//input[@value='Save']"));
                submitButton.Click();

                driver.FindElement(By.Id(sectionName)).Click();

                var deleteButton = driver.WaitUntilVisible(
                    By.XPath($"//tr/td[contains(text(), '{seasonName}')]/..//a[contains(text(),'Delete')]"));
                deleteButton.Click();

                submitButton = driver.FindElement(By.XPath("//input[@value='Delete']"));
                submitButton.Click();

                Assert.True(driver.Url.Contains("/TEST/Admin"), "Failed to delete season");
            }
        }

        [Fact]
        public void AddEditDeleteSeries()
        {
            var sectionName = "series";
            var seriesName = $"Test Series {DateTime.Now.ToString("yyyyMMdd Hmmss")}";

            using (var driver = new ChromeDriver(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)))
            {
                LoginAndGoToHiddenTestClub(driver);
                driver.FindElement(By.LinkText("Admin Page")).Click();
                driver.FindElement(By.Id(sectionName)).Click();

                var createLink = driver.WaitUntilClickable(By.LinkText("Create"));
                createLink.Click();
                driver.FindElement(By.Id("Name")).SendKeys(seriesName);

                var seasonSelector = new SelectElement(driver.FindElement(By.Id("SeasonId")));
                seasonSelector.SelectByText("2019");

                var submitButton = driver.FindElement(By.XPath("//input[@value='Create']"));
                submitButton.Click();

                driver.FindElement(By.Id(sectionName)).Click();

                var editButton = driver.WaitUntilVisible(
                    By.XPath($"//tr/td[contains(text(), '{seriesName}')]/..//a[contains(text(),'Edit')]"));
                editButton.Click();

                submitButton = driver.FindElement(By.XPath("//input[@value='Save']"));
                submitButton.Click();

                driver.FindElement(By.Id(sectionName)).Click();

                var deleteButton = driver.WaitUntilVisible(
                    By.XPath($"//tr/td[contains(text(), '{seriesName}')]/..//a[contains(text(),'Delete')]"));
                deleteButton.Click();

                submitButton = driver.FindElement(By.XPath("//input[@value='Delete']"));
                submitButton.Click();

                Assert.True(driver.Url.Contains("/TEST/Admin"), "Failed to delete series");
            }
        }

        [Fact]
        public void AddEditDeleteRegatta()
        {
            var sectionName = "regatta";
            var seriesName = $"Test Regatta {DateTime.Now.ToString("yyyyMMdd Hmmss")}";

            using (var driver = new ChromeDriver(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)))
            {
                LoginAndGoToHiddenTestClub(driver);
                driver.FindElement(By.LinkText("Admin Page")).Click();
                driver.FindElement(By.Id(sectionName)).Click();

                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(2));
                var createLink = driver.WaitUntilClickable(By.LinkText("Create"));
                createLink.Click();
                driver.FindElement(By.Id("Name")).SendKeys(seriesName);
                driver.FindElement(By.Name("StartDate")).SendKeys(DateTime.Today.AddDays(-1).ToShortDateString());
                driver.FindElement(By.Name("EndDate")).SendKeys(DateTime.Today.AddDays(1).ToShortDateString());

                var submitButton = driver.FindElement(By.XPath("//input[@value='Create']"));
                submitButton.Click();

                driver.FindElement(By.Id(sectionName)).Click();

                var editButton = driver.WaitUntilVisible(
                    By.XPath($"//tr/td[contains(text(), '{seriesName}')]/..//a[contains(text(),'Edit')]"));
                editButton.Click();

                submitButton = driver.FindElement(By.XPath("//input[@value='Save']"));
                submitButton.Click();

                driver.FindElement(By.Id(sectionName)).Click();

                var deleteButton = driver.WaitUntilVisible(
                    By.XPath($"//tr/td[contains(text(), '{seriesName}')]/..//a[contains(text(),'Delete')]"));
                deleteButton.Click();

                submitButton = driver.FindElement(By.XPath("//input[@value='Delete']"));
                submitButton.Click();

                Assert.True(driver.Url.Contains("/TEST/Admin"), "Failed to delete regatta");
            }
        }


        [Fact]
        public void AddRaceAndSeeResults()
        {
            using (var driver = new ChromeDriver(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)))
            {
                LoginAndGoToHiddenTestClub(driver);

                var serieslink = driver.FindElements(By.LinkText("Series"));
                if (serieslink.Count == 0)
                {
                    var firstButton = driver.FindElement(By.CssSelector("button"));
                    firstButton.Click();
                    serieslink = driver.FindElements(By.LinkText("Series"));
                }

                driver.FindElement(By.LinkText("Test Series")).Click();

                var headerlinks = driver.FindElements(By.CssSelector("th a"));
                int raceCount = headerlinks.Count;

                // back to club page
                driver.FindElement(By.LinkText("TEST")).Click();

                // click add race button
                driver.FindElementByLinkText("New Race").Click();

                // set race fields, saving order for later
                var fleetSelector = new SelectElement(driver.FindElement(By.Id("fleetId")));
                fleetSelector.SelectByText("Test Boat Class Fleet");
                var seriesSelector = new SelectElement(driver.FindElement(By.Id("seriesIds")));
                seriesSelector.SelectByText("Test Series");

                driver.FindElementByLinkText("Additional Fields").Click();
                int order = ++raceCount;
                var orderField = driver.WaitUntilVisible(By.Id("InitialOrder"));
                orderField.SendKeys(order.ToString());

                var addCompElement = driver.FindElement(By.Id("newCompetitor"));
                addCompElement.SendKeys("11111");
                addCompElement.SendKeys(Keys.Return);

                addCompElement.SendKeys("222");
                addCompElement.SendKeys(Keys.Return);

                var submitButton = driver.FindElement(By.XPath("//input[@value='Create']"));
                submitButton.Click();

                // go to this series.
                driver.FindElement(By.LinkText("TEST")).Click();

                driver.FindElement(By.LinkText("Test Series")).Click();

                // verify series includes race heading
                var linkText = $"{DateTime.Today.ToString("M-d")} R{order}";
                var raceLink = driver.FindElement(By.LinkText(linkText));

                Assert.NotNull(raceLink);

            }
        }
    }
}