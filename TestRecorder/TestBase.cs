using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using ScreenRecorderLib;
using System.Diagnostics;
using System.Drawing;
using Font = System.Drawing.Font;
using ImageFormat = System.Drawing.Imaging.ImageFormat;

namespace AppTests
{
    public class TestBase
    {
        protected static string? TestUrl {  get; set; }
        protected static TestContext? TestContext { get; set; }
        protected static IWebDriver? Driver { get; private set; }
        protected static Recorder? ScreenRecorder { get; set; }
        protected static List<RecordingSourceBase>? WindowSources { get; set; }

        private static string _resultsFolder;
        private static string _fileTimeStamp;

        protected static void ClassSetup(TestContext context, bool isDebug = false, bool showBrowser = false)
        {
            
            TestContext = context;
            TestUrl = context.Properties["testUrl"].ToString();
            _resultsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Videos");
            var options = new ChromeOptions();
            options.AddArgument($"--window-name={context.FullyQualifiedTestClassName}");
            options.AddArguments("--disable-gpu", "--ignore-certificate-errors", "--disable-extensions", "--no-sandbox", "--disable-dev-shm-usage", $"--window-name={context.FullyQualifiedTestClassName}");
            if (!showBrowser) { options.AddArgument("--headless"); }
            options.AddUserProfilePreference("credentials_enable_service", false);
            options.AddUserProfilePreference("profile.password_manager_enabled", false);
            options.AddUserProfilePreference("autofill.profile_enabled", false);
            Driver = new ChromeDriver(AppDomain.CurrentDomain.BaseDirectory, options, TimeSpan.FromSeconds(300));
            Driver.Manage().Window.Size = new Size(1300, 975);
            Driver.Manage().Window.Position = new Point(0, 0);
            Driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(300);
            var driverWindow = Recorder.GetWindows().FirstOrDefault(w => w.Title == $"{context.FullyQualifiedTestClassName}");
            if (driverWindow != null)
            {
                WindowSources = [driverWindow];
                ScreenRecorder = Recorder.CreateRecorder();
            }
        }

        protected static void ClassCleanup()
        {
            Driver?.Quit();
            foreach (var process in Process.GetProcessesByName("chromedriver")) 
            {
                process.Kill();
            }
        }

        protected static void TestSetup()
        {
        }
        protected static void TestCleanup()
        {
            if (ScreenRecorder != null && ScreenRecorder.Status == RecorderStatus.Recording)
            {
                Thread.Sleep(200);
                ScreenRecorder.Stop();
            }
        }
        internal static void StartRecording(string testName)
        {

            ScreenRecorder.SetOptions(new RecorderOptions
            {
                SourceOptions = new SourceOptions { RecordingSources = WindowSources },
                AudioOptions = new AudioOptions { IsAudioEnabled = false },
                OverlayOptions = new OverLayOptions
                {
                    Overlays =
                    [
                        new ImageOverlay()
                        {
                            AnchorPoint = Anchor.TopRight,
                            Offset = new ScreenSize(10, 110),
                            Stretch = StretchMode.None,
                            SourceStream = new MemoryStream(GetTimestampImage())
                        }
                    ]
                }
            });
            _fileTimeStamp = $"{DateTime.Now:MMddhhmmss}";

            string videoPath = Path.Combine(_resultsFolder, $"{testName}_{_fileTimeStamp}.mp4");
            ScreenRecorder.Record(videoPath);
        }

        private static byte[] GetTimestampImage()
        {
            byte[] imageBytes = [];
            using (var bitmap = new Bitmap(610, 56))
            {
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    graphics.Clear(Color.White);
                    var font = new Font("Consolas", 28, FontStyle.Bold);
                    var brush = new SolidBrush(Color.Red);
                    var dateTime = DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt").ToLower();
                    graphics.DrawString(dateTime, font, brush, new PointF(5, 2));
                }
                using (var memoryStream = new MemoryStream())
                {
                    bitmap.Save(memoryStream, ImageFormat.Png);
                    imageBytes = memoryStream.ToArray();
                }
            }
            return imageBytes;
        }

    }
}
