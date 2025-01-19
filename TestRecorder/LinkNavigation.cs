using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using ScreenRecorderLib;

namespace AppTests
{
    [TestClass]
    public class LinkNavigation : TestBase
    {
        [ClassInitialize]
        public static void ClassSetup(TestContext context)
        {
            TestBase.ClassSetup(context, isDebug: true, showBrowser: true);
        }
        [ClassCleanup]
        public static new void ClassCleanup()
        {
            TestBase.ClassCleanup();
            Directory.Delete(TestContext.TestRunDirectory, true);
        }

        [TestInitialize]
        public void Setup()
        {
            TestSetup();
        }
        [TestCleanup]
        public void Cleanup()
        {
            TestCleanup();
        }

        [TestMethod]
        public void AccessPages()
        {
            var wait = new WebDriverWait(Driver, TimeSpan.FromSeconds(30));
            var failures = new List<string>();
            Driver.Url = TestUrl;
            var newsLinks = Driver.FindElements(By.XPath("//div[@role='menubar']/div[not(@aria-selected='true')]/a[@class='brSCsc']"));
            var linkTitles = newsLinks.Select(l => l.GetAttribute("aria-label")).ToList();
            linkTitles.Reverse();
            StartRecording(TestContext.TestName);
            do
            {
                Thread.Sleep(200);
            } while (ScreenRecorder.Status != RecorderStatus.Recording);

            Thread.Sleep(1000);
            var originalUrl = Driver.Url;
            foreach (var title in linkTitles)
            {
                var link = Driver.FindElement(By.XPath($"//div[@role='menubar']//a[text()='{title}']"));
                if (link.Displayed)
                {
                    link.Click();
                    wait.Until(Driver => ((IJavaScriptExecutor)Driver).ExecuteScript("return document.readyState").ToString() == "complete");
                    Thread.Sleep(1000);
                }
                Driver.Url = originalUrl;
                wait.Until(Driver => ((IJavaScriptExecutor)Driver).ExecuteScript("return document.readyState").ToString() == "complete");
                Thread.Sleep(1000);
            }
            
        }
    }
}
