
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
    [Trait("Club", "None")]
    public class HomeAnonymousTests
    {
        private readonly SailScoresTestConfig configuration;

        public HomeAnonymousTests()
        {
            configuration = TestHelper.GetApplicationConfiguration(Environment.CurrentDirectory);
        }


        [Trait("Read Only", "True")]
        [Fact]
        public void HomePage_HasDropdown()
        {
            using var driver = new ChromeDriver();
            driver.Navigate().GoToUrl(configuration.BaseUrl);

            //if logged in, log out.
            var logout = driver.FindElements(By.LinkText("Log out"));
            if(logout.Count > 0)
            {
                logout[0].Click();
            }

            var clubSelector = new SelectElement(driver.FindElement(By.Id("clubSelect")));
            Assert.NotNull(clubSelector);
        }

        [Trait("Read Only", "True")]
        [Fact]
        public void AboutAndNews_Load()
        {
            using var driver = new ChromeDriver();
            driver.Navigate().GoToUrl(configuration.BaseUrl);


            //if logged in, log out.
            var logout = driver.FindElements(By.LinkText("Log out"));
            if (logout.Count > 0)
            {
                logout[0].Click();
            }

            var currentElement = driver.WaitUntilClickable(By.LinkText("About"));
            currentElement.Click();

            // Relying on this to throw exception if not found.
            driver.FindElement(By.XPath("//h1[contains(.,'kept simple')]"));

            currentElement = driver.WaitUntilClickable(By.LinkText("News"));
            currentElement.Click();

            // Relying on this to throw exception if not found.
            driver.FindElement(By.XPath("//h5[contains(.,'To the cloud')]"));
        }

        [Fact]
        public void FillOutClubRequest_Load()
        {
            using var driver = new ChromeDriver();
            driver.Navigate().GoToUrl(configuration.BaseUrl);


            //if logged in, log out.
            var logout = driver.FindElements(By.LinkText("Log out"));
            if (logout.Count > 0)
            {
                logout[0].Click();
            }
            int num = new Random().Next(1000);
            driver.FindElement(By.LinkText("Try it out")).Click();
            driver.FindElement(By.Id("ClubName")).Click();
            driver.FindElement(By.Id("ClubName")).SendKeys($"asdf{num}");
            driver.FindElement(By.Id("ClubInitials")).Click();
            driver.FindElement(By.Id("ClubInitials")).SendKeys($"ASDF{num}");
            driver.FindElement(By.Id("ClubLocation")).SendKeys("somewhere");
            driver.FindElement(By.Id("ClubWebsite")).SendKeys("http://www.google.com");
            driver.FindElement(By.Id("ContactFirstName")).SendKeys("Jamie");
            driver.FindElement(By.Id("ContactLastName")).SendKeys("Fraser Test");
            driver.FindElement(By.Id("ContactEmail")).SendKeys($"test{num}@jamie.com");

            driver.FindElement(By.Id("Password")).SendKeys("P@ssw0rd");
            driver.FindElement(By.Id("ConfirmPassword")).SendKeys("P@ssw0rd");
            driver.FindElement(By.Id("Classes")).SendKeys("MC ");
            driver.FindElement(By.Id("Comments")).SendKeys("Nothing");
            driver.FindElement(By.CssSelector(".btn-success")).Click();
            driver.FindElement(By.XPath("//h1[contains(.,'Club Created')]"));
        }

    }
}