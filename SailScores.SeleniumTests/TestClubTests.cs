﻿
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using Xunit;

namespace SailScores.SeleniumTests
{

    [Trait("Club", "TEST")]
    public class TestClubTests
    {
        private readonly SailScoresTestConfig configuration;

        public TestClubTests()
        {
            configuration = TestHelper.GetApplicationConfiguration(Environment.CurrentDirectory);
        }


        private void LoginAndGoToHiddenTestClub(IWebDriver driver)
        {
            //navigate to home page.
            driver.Navigate().GoToUrl(configuration.BaseUrl);

            //log in
            driver.FindElement(By.LinkText("Log in")).Click();
            driver.WaitUntilClickable(By.Id("Email")).SendKeys(configuration.TestEmail);
            driver.FindElement(By.Id("Password")).SendKeys(configuration.TestPassword);
            driver.FindElement(By.CssSelector("form")).Submit();

            // used go to (hidden) test club
            driver.Url = UrlCombine(configuration.BaseUrl, configuration.TestClubInitials);
        }

        //thank you stack overflow:
        //https://stackoverflow.com/a/2806717/400375
        private string UrlCombine(string url1, string url2)
        {
            if (url1.Length == 0)
            {
                return url2;
            }

            if (url2.Length == 0)
            {
                return url1;
            }

            url1 = url1.TrimEnd('/', '\\');
            url2 = url2.TrimStart('/', '\\');

            return string.Format("{0}/{1}", url1, url2);
        }

        [Fact]
        public void AddAndDeleteBoatClass()
        {
            var sectionId = "classes";
            using var driver = new ChromeDriver();
            LoginAndGoToHiddenTestClub(driver);
            driver.WaitUntilClickable(By.LinkText("Admin Page")).Click();
            driver.WaitUntilClickable(By.Id(sectionId)).Click();

            var createLink = driver.WaitUntilClickable(By.LinkText("New Class"));
            createLink.Click();
            var className = $"AutoTest Class {DateTime.Now.ToString("yyyyMMdd Hmmss")}";
            driver.FindElement(By.Id("Name")).SendKeys(className);

            var submitButton = driver.FindElement(By.XPath("//input[@value='Create']"));
            submitButton.Click();

            driver.FindElement(By.Id("classes")).Click();

            var deleteButton = GetDeleteButtonForRow(driver, sectionId, className);

            deleteButton.Click();

            submitButton = driver.FindElement(By.XPath("//input[@value='Delete']"));
            submitButton.Click();

            Assert.True(driver.Url.Contains("/TEST/Admin"), "Failed to delete boat class");
        }

        [Fact]
        public void AddEditDeleteCompetitor()
        {

            using var driver = new ChromeDriver();
            LoginAndGoToHiddenTestClub(driver);
            driver.FindElement(By.LinkText("Admin Page")).Click();
            
            //link to competitor page
            driver.FindElement(By.LinkText("Competitor page")).Click();

            var createLink = driver.WaitUntilClickable(By.LinkText("Create"));
            createLink.Click();
            var compName = $"AutoTest Comp {DateTime.Now.ToString("yyyyMMdd Hmmss")}";
            driver.FindElement(By.Name("competitors[0].Name")).SendKeys(compName);

            var rnd = new Random();
            var sailNumber = $"AT{rnd.Next(9999)}";
            driver.FindElement(By.Name("competitors[0].SailNumber")).SendKeys(sailNumber);
            driver.FindElement(By.Name("competitors[0].BoatName")).SendKeys("Test Boat");

            var classSelector = new SelectElement(driver.FindElement(By.Id("BoatClassId")));
            classSelector.SelectByText("Test Boat Class");

            var submitButton = driver.FindElement(By.XPath("//input[@value='Create']"));
            submitButton.Click();


            var editButton = driver.WaitUntilVisible(
                By.XPath($"//div[. = '{compName}']/../../..//a[@title = 'Edit']"));
            editButton.Click();

            submitButton = driver.FindElement(By.XPath("//input[@value='Save']"));
            submitButton.Click();


            var deleteButton = driver.WaitUntilVisible(
                By.XPath(
                $"//div[contains(@class, 'container')]//div[contains(@class, 'row')]" +
                $"//div[contains(@class, 'row') and contains(., '{compName}')]" +
                "//a[@title='Delete']"));
            deleteButton.Click();

            submitButton = driver.FindElement(By.XPath("//input[@value='Delete']"));
            submitButton.Click();

            Assert.True(driver.Url.EndsWith("/TEST/Competitor"), "Failed to delete competitor");
        }

        [Fact]
        public void AddEditDeleteFleet()
        {
            var sectionName = "fleets";
            using var driver = new ChromeDriver();
            LoginAndGoToHiddenTestClub(driver);
            driver.FindElement(By.LinkText("Admin Page")).Click();
            driver.FindElement(By.Id(sectionName)).Click();

            var createLink = driver.WaitUntilClickable(By.LinkText("New Fleet"));
            createLink.Click();
            var fleetName = $"AutoTest Fleet {DateTime.Now.ToString("yyyyMMdd Hmmss")}";
            driver.FindElement(By.Id("Name")).SendKeys(fleetName);
            driver.FindElement(By.Id("NickName")).SendKeys(fleetName);

            var typeSelector = new SelectElement(driver.FindElement(By.Id("fleetType")));
            typeSelector.SelectByText("Selected Boats");


            var boatSelector = new SelectElement(driver.FindElement(By.Id("CompetitorIds")));
            boatSelector.SelectByText("11111 - Alice (Test Boat Class)");
            boatSelector.SelectByText("22222 - Bob (Test Boat Class)");


            var submitButton = driver.FindElement(By.XPath("//input[@value='Create']"));

            Actions actions = new Actions(driver);
            actions.MoveToElement(submitButton);
            actions.Perform();
            submitButton.Click();

            driver.FindElement(By.Id(sectionName)).Click();

            var editButton = GetEditButtonForRow(driver, sectionName, fleetName);
            editButton.Click();

            submitButton = driver.FindElement(By.XPath("//input[@value='Save']"));
            submitButton.Click();

            driver.FindElement(By.Id(sectionName)).Click();


            var deleteButton = GetDeleteButtonForRow(driver, sectionName, fleetName);
            deleteButton.Click();

            submitButton = driver.FindElement(By.XPath("//input[@value='Delete']"));
            submitButton.Click();

            Assert.True(driver.Url.Contains("/TEST/Admin"), "Failed to delete fleet");
        }

        [Fact]
        public void AddEditDeleteSeason()
        {
            using var driver = new ChromeDriver();
            LoginAndGoToHiddenTestClub(driver);
            driver.FindElement(By.LinkText("Admin Page")).Click();
            var sectionName = "seasons";
            driver.FindElement(By.Id(sectionName)).Click();

            var createLink = driver.WaitUntilClickable(By.LinkText("New Season"));
            createLink.Click();
            var startDate = DateTime.Today.AddYears(-5);
            var finishDate = DateTime.Today.AddDays(1).AddYears(-5);
            var seasonName = $"Test {startDate.Year}";
            driver.FindElement(By.Id("Name")).SendKeys(seasonName);
            driver.FindElement(By.Id("Start")).SendKeys(startDate.ToString("MMDDYYYY"));
            driver.FindElement(By.Id("End")).SendKeys(finishDate.ToString("MMDDYYYY"));


            var submitButton = driver.WaitUntilVisible(By.XPath("//input[@value='Create']"));
            submitButton.Click();
            // if submit button fails, check to see if already a season with this name exists.

            driver.FindElement(By.Id(sectionName)).Click();


            var editButton = GetEditButtonForRow(driver, sectionName, seasonName);
            editButton.Click();

            submitButton = driver.FindElement(By.XPath("//input[@value='Save']"));
            submitButton.Click();

            driver.FindElement(By.Id(sectionName)).Click();


            var deleteButton = GetDeleteButtonForRow(driver, sectionName, seasonName);
            deleteButton.Click();

            submitButton = driver.FindElement(By.XPath("//input[@value='Delete']"));
            submitButton.Click();

            Assert.True(driver.Url.Contains("/TEST/Admin"), "Failed to delete season");
        }

        [Fact]
        public void AddEditDeleteSeries()
        {
            var sectionName = "series";
            var seriesName = $"Test Series {DateTime.Now.ToString("yyyyMMdd Hmmss")}";

            using var driver = new ChromeDriver();
            LoginAndGoToHiddenTestClub(driver);
            driver.FindElement(By.LinkText("Admin Page")).Click();
            driver.FindElement(By.Id(sectionName)).Click();

            var createLink = driver.WaitUntilClickable(By.LinkText("New Series"));
            createLink.Click();
            driver.FindElement(By.Id("Name")).SendKeys(seriesName);

            var seasonSelector = new SelectElement(driver.FindElement(By.Id("SeasonId")));
            seasonSelector.SelectByText(DateTime.Today.Year.ToString());

            var submitButton = driver.FindElement(By.XPath("//input[@value='Create']"));
            submitButton.Click();

            driver.FindElement(By.Id(sectionName)).Click();

            var editButton = GetEditButtonForRow(driver, sectionName, seriesName);
            editButton.Click();

            submitButton = driver.FindElement(By.XPath("//input[@value='Save']"));
            submitButton.Click();

            driver.FindElement(By.Id(sectionName)).Click();

            var deleteButton = GetDeleteButtonForRow(driver, sectionName, seriesName);
            deleteButton.Click();

            submitButton = driver.FindElement(By.XPath("//input[@value='Delete']"));
            submitButton.Click();

            Assert.True(driver.Url.Contains("/TEST/Admin"), "Failed to delete series");
        }

        [Fact]
        public void AddEditDeleteRegatta()
        {
            var sectionName = "regattas";
            var regattaName = $"Test Regatta {DateTime.Now.ToString("yyyyMMdd Hmmss")}";

            using var driver = new ChromeDriver();
            LoginAndGoToHiddenTestClub(driver);
            driver.FindElement(By.LinkText("Admin Page")).Click();
            driver.FindElement(By.Id(sectionName)).Click();

            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(2));
            var createLink = driver.WaitUntilClickable(By.LinkText("New Regatta"));
            createLink.Click();
            driver.FindElement(By.Id("Name")).SendKeys(regattaName);
            driver.FindElement(By.Name("StartDate")).SendKeys(DateTime.Today.AddDays(-1).ToString("MM/dd/yyyy"));
            driver.FindElement(By.Name("EndDate")).SendKeys(DateTime.Today.AddDays(1).ToString("MM/dd/yyyy"));

            var submitButton = driver.FindElement(By.XPath("//input[@value='Create']"));
            submitButton.Click();

            driver.WaitUntilClickable(By.LinkText("Edit Regatta")).Click();

            submitButton = driver.FindElement(By.XPath("//input[@value='Save']"));
            submitButton.Click();

            driver.FindElement(By.LinkText("Club Admin")).Click();

            driver.WaitUntilVisible(By.Id(sectionName)).Click();

            var deleteButton = GetDeleteButtonForRow(driver, sectionName, regattaName);
            deleteButton.Click();

            submitButton = driver.FindElement(By.XPath("//input[@value='Delete']"));
            submitButton.Click();

            Assert.True(driver.Url.Contains("/TEST/Admin"), "Failed to delete regatta");
        }


        [Fact]
        public void AddRaceAndSeeResults()
        {
            var year = DateTime.Today.Year;
            using var driver = new ChromeDriver();
            LoginAndGoToHiddenTestClub(driver);

            var serieslink = driver.FindElements(By.LinkText("Series"));
            if (serieslink.Count == 0)
            {
                var firstButton = driver.FindElement(By.CssSelector("button"));
                firstButton.Click();
                serieslink = driver.FindElements(By.LinkText("Series"));
            }
            serieslink[0].Click();

            var testSeriesLink = driver.WaitUntilClickable(By.LinkText($"{year} Test Series"));
            testSeriesLink.Click();

            var headerlinks = driver.FindElements(By.CssSelector("th a"));
            int raceCount = headerlinks.Count;

            // back to club page
            driver.FindElement(By.LinkText("TEST")).Click();

            // click add race button
            driver.FindElement(By.LinkText("New Race")).Click();

            // set race fields, saving order for later
            var fleetSelector = new SelectElement(driver.WaitUntilVisible(By.Id("fleetId")));
            fleetSelector.SelectByText("Test Boat Class Fleet");
            var seriesSelector = new SelectElement(driver.FindElement(By.Id("seriesIds")));
            seriesSelector.SelectByText($"{year} Test Series");

            driver.FindElement(By.LinkText("Optional Fields")).Click();
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
            driver.WaitUntilVisible(By.LinkText("TEST")).Click();

            // since results are calculated as a background thread, need to pause here:
            Thread.Sleep(1000);
            driver.WaitUntilVisible(By.LinkText($"{year} Test Series")).Click();

            // verify series includes race heading
            var linkText = $"{DateTime.Today.ToString("M/d")} R{order}";
            var raceLink = driver.FindElement(By.LinkText(linkText));

            Assert.NotNull(raceLink);
        }

        private IWebElement GetDeleteButtonForRow(
            IWebDriver driver,
            string sectionId,
            string itemName
            )
        {
            return driver.WaitUntilClickable(
                GetButtonSelector("Delete", sectionId, itemName));
        }
        private IWebElement GetEditButtonForRow(
           IWebDriver driver,
           string sectionId,
           string itemName
           )
        {
            return driver.WaitUntilClickable(
                GetButtonSelector("Edit", sectionId, itemName));
        }

        private By GetButtonSelector(
            string buttonTitle,
            string sectionId,
            string itemInRowText)
        {
            return By.XPath(
                $"//div[@id=\"{sectionId}div\"]//div[contains(string(), \"{itemInRowText}\")]//a[@title=\"{buttonTitle}\"]");

        }
    }
}