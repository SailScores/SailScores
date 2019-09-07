
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
                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(30));
                var currentElement = wait.Until(ExpectedConditions.ElementToBeClickable(By.PartialLinkText("LHYC")));

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

                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(2));
                var createLink = wait.Until(ExpectedConditions.ElementToBeClickable(By.LinkText("Create")));
                createLink.Click();
                var className = $"AutoTest Class {DateTime.Now.ToString("yyyyMMdd Hmmss")}";
                driver.FindElement(By.Id("Name")).SendKeys(className);

                var submitButton = driver.FindElement(By.XPath("//input[@value='Create']"));
                submitButton.Click();
                
                driver.FindElement(By.Id("classes")).Click();

                var deleteButton = wait.Until(ExpectedConditions.ElementIsVisible(
                    By.XPath($"//tr/td[contains(text(), '{className}')]/../../tr//a[contains(text(),'Delete')]")));

                deleteButton.Click();

                submitButton = driver.FindElement(By.XPath("//input[@value='Delete']"));
                submitButton.Click();

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

                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(2));
                var createLink = wait.Until(ExpectedConditions.ElementToBeClickable(By.LinkText("Create")));
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


                var editButton = wait.Until(ExpectedConditions.ElementIsVisible(
                    By.XPath($"//tr/td[contains(text(), '{compName}')]/../../tr//a[contains(text(),'Edit')]")));
                editButton.Click();

                submitButton = driver.FindElement(By.XPath("//input[@value='Save']"));
                submitButton.Click();

                driver.FindElement(By.Id("competitors")).Click();

                var deleteButton = wait.Until(ExpectedConditions.ElementIsVisible(
                    By.XPath($"//tr/td[contains(text(), '{compName}')]/../../tr//a[contains(text(),'Delete')]")));
                deleteButton.Click();

                submitButton = driver.FindElement(By.XPath("//input[@value='Delete']"));
                submitButton.Click();

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

                var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(2));
                var createLink = wait.Until(ExpectedConditions.ElementToBeClickable(By.LinkText("Create")));
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


                var editButton = wait.Until(ExpectedConditions.ElementIsVisible(
                    By.XPath($"//tr/td[contains(text(), '{fleetName}')]/../../tr//a[contains(text(),'Edit')]")));
                editButton.Click();

                submitButton = driver.FindElement(By.XPath("//input[@value='Save']"));
                submitButton.Click();

                driver.FindElement(By.Id("fleets")).Click();

                var deleteButton = wait.Until(ExpectedConditions.ElementIsVisible(
                    By.XPath($"//tr/td[contains(text(), '{fleetName}')]/../../tr//a[contains(text(),'Delete')]")));
                deleteButton.Click();

                submitButton = driver.FindElement(By.XPath("//input[@value='Delete']"));
                submitButton.Click();

                Assert.True(driver.Url.Contains("/TEST/Admin"));
            }
        }

        [Fact]
        public void AddRaceAndSeeResults()
        {
            using (var driver = new ChromeDriver(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)))
            {
                LoginAndGoToHiddenTestClub(driver);

                // click add race button
                driver.FindElementByLinkText("New Race").Click();

                // set race fields, saving date and order for later

                // add competitors
                // click save
                // go to this series.
                // verify series includes race heading

            }
        }

        // create fleet
        // create series
        // create regatta
        // delete regatta
    }
}