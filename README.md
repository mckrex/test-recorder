# test-recorder

**Video Recording Selenium Tests in C#**

I have a suite of automated tests written using Selenium, .NET Core, and
MSTest, and I needed a way to make video recordings of the test
executions. My organization doesn’t use Selenium Grid and our projects
don’t justify setting that up, so I needed something I could integrate
into test executions that I run directly from Visual Studio. In the
past, the common recommendation was to use
[<u>Microsoft.Express.Encoder</u>](https://en.wikipedia.org/wiki/Microsoft_Expression_Encoder),
but this product has been [<u>thoroughly
discontinued</u>](https://answers.microsoft.com/en-us/windows/forum/all/microsoft-expression-encoder-replacement-for/32d4e460-8239-49b9-b013-d2668770276b).
[<u>Some search results
suggested</u>](https://stackoverflow.com/a/57434905/2052082) calling a
batch file to start a third party program, but I felt like this was more
of a hack than a full integration into my tests. I wanted to control,
inside the test execution, what got recorded and when.

So I needed to find something new and found this library:
[<u>ScreenRecorderLib</u>](https://github.com/sskodje/ScreenRecorderLib).
Using that library I created a way to integrate screen recording into my
test suite. This project is a simplified example of how this can be done.

**Loading the
[<u>ScreenRecorderLib</u>](https://github.com/sskodje/ScreenRecorderLib)
Package**

The nuget package is easy enough to find, but it has a requirement I
don’t think I’ve ever seen before: it will not compile using “Any CPU”
as the platform. I don’t think I’ve ever faced this before, but the fix
is easy enough: just pick x64 or x86 as your solution platform.

**Defining the Recorder**

I use a base class for my tests suites where I define objects every test
will use, so this is where I decided to set up the screen recorder. I
“construct” this static base class by calling it from a method in my
test decorated with the \[ClassInitialize\] attribute.
```
[TestClass]  
public class LinkNavigation : TestBase  
{  
    [ClassInitialize]  
    public static void ClassSetup(TestContext context)  
    {  
        TestBase.ClassSetup(context, isDebug: true, showBrowser: true);  
    }  
}
```
The recorder is initialized by a static method in ScreenRecordrLib. I
assign the result to a property in the base class.
```
protected static Recorder ScreenRecorder { get; set; }  
protected static void ClassSetup(TestContext context, bool isDebug = false, bool showBrowser = false)  
{  
    ScreenRecorder = Recorder.CreateRecorder();
}
```
The next challenge is trying to define the area of the screen to
record. ScreenRecorderLib has another static method that will identify
all available recording sources, Recorder.GetWindows(), that returns a
List\<ScreenRecorderLib.RecordableWindow\>. A browser window opens when
a new WebDriver object is created, and after its creation the browser
window will be one of the items in that list.

But the tricky part is identifying which item. Typically, GetWindows()
returns all the windows in reverse creation order, so the most recently
created window is the first on the list. Typically, I’ll create the
recorder right after I initialize the driver window, so my test browser
is probably the first item in the list. But I don’t like relying on
default sorts like this because frameworks can change and, in any case,
it’s not guaranteed that I’ll create the recorder immediately after the
web driver.

Digging deeper, I looked for some kind of shared identifier. A WebDriver
window has a property called SessionID, and theRecordableWindow object
includes Handle and ID properties, but unfortunately these properties
don’t align in any way. So I chose to set the title property of the web
driver window in such a way that I could find it in the RecordableWindow
list.

The window title can be defined in the arguments of a DriverOptions
object. I used the the name of the testing class, but any unique value
would be sufficient. Once the window is identified, the RecordableWindow
object can be added to the SourceOptions.RecordingSources property of
the screen recorder.

```
protected static void ClassSetup(TestContext context, bool isDebug = false, bool showBrowser = false)  
{  
    ScreenRecorder = Recorder.CreateRecorder();  
    var options = new ChromeOptions();  
    options.AddArgument($"--window-name={context.FullyQualifiedTestClassName}");  
    var driver = new ChromeDriver(AppDomain.CurrentDomain.BaseDirectory,
    options, TimeSpan.FromSeconds(300));  
    var sources = new List<RecordingSourceBase>()  
    {  
        Recorder.GetWindows().FirstOrDefault(w => w.Title == context.FullyQualifiedTestClassName)  
    };  
    ScreenRecorder.SetOptions(new RecorderOptions  
    {  
        SourceOptions = new SourceOptions { RecordingSources = sources }  
    });  
}
```
Do note that the driver must be open on the screen. If the driver is run
in headless mode it will not be added as RecordableSource.

**Using the Recorder**

Since the recorder is a property of the base class it’s available to the
derived test classes and can be started and stopped at any stage of an
individual test.

It does need a path to save the recording, so I use the test name and a
timestamp to create a unique file name. I also use a method in the base
class to start and name the recording, since this is a common action for
every test.
```
internal static void StartRecording(string testName)  
{  
    string videoPath = Path.Combine(Path.GetTempPath(),
    $"{testName}_{DateTime.Now:MMddhhmmss}.mp4");  
    ScreenRecorder.Record(videoPath);  
    do  
    {  
        Thread.Sleep(100);  
    } while (ScreenRecorder.Status != RecorderStatus.Recording);  
}
```
While the recorder starts quickly, Selenium executes quickly, so I add a
loop to verify the recorder’s status before returning to the test.

My use case for these tests is to provide a complete application demo
for product owners without obligating them to perform every action
themselves. I also want to give them a chance to move back and forth
between screens as slowly as they want so they can see how the
application behaves. Because Selenium executes so quickly, I found there
were times when no frames were actually recorded for certain test
actions. For this reason, I include pauses in the test process to make
sure every action is visible.

**Sample Implementation**

One place where I used these recordings was after a major refactor of a
web application’s underlying code. To show that all of the ~80 screens
in the application loaded correctly, I wrote a test that navigated to
every available link to show that each page loaded without error. This project has a simplified version of this test, using the
[<u>Google news home page</u>](https://news.google.com) as the test
subject.

The test identifies every link in the Google news menu bar, clicks on
each link, then returns to the Google news home page after the linked
page is loaded. At each stage, I pause for one second to make sure the
viewer can see correct page has fully loaded.
```
var originalUrl = driver.Url;  
foreach (var title in linkTitles)  
{  
    var link = driver.FindElement(By.XPath($"//div[@role='menubar']//a[text()='{title}']"));  
    if (link.Displayed)  
    {  
        link.Click();  
        wait.Until(driver => ((IJavaScriptExecutor)driver).ExecuteScript("return document.readyState").ToString() == "complete");  
        Thread.Sleep(1000);  
    }  
    driver.Url = originalUrl;  
    wait.Until(driver => ((IJavaScriptExecutor)driver).ExecuteScript("return document.readyState").ToString() == "complete");  
    Thread.Sleep(1000);  
}
```
You can download the code and experiment with shorter or even no pauses
and you’ll see that operations can happen so quickly it’s not clear from
the recording that the test succeeded.

**Stopping the Recording**

The recording needs to stop gracefully to create a valid video file. To
ensure the recording will stop I use a test cleanup method. The
recording really needs to be started and stopped from each test because
each test starts and stops it’s own browser window. Again, I call a
method in the base class from a properly decorated method in the test
class.
```
// text class method  
[TestCleanup]  
public void Cleanup()  
{  
    base.TestCleanup();  
}  
  
// base class method  
protected void TestCleanup()  
{  
    if (ScreenRecorder != null && ScreenRecorder.Status == RecorderStatus.Recording)  
    {  
        ScreenRecorder.Stop();  
    }  
}
```
If the recording isn’t stopped gracefully, you end up with an .mp4 file
that cannot be opened.

**Overlays**

One test I recorded had a test user perform an action, then had a second
user log in and review their action in a timestamped log. Obviously, to
show success, I wanted to be clear when the first user’s action was
performed so the reviewer could match the action to the time stamp in
the log. For this I used ScreenRecorderLib’s overlay feature.

An overlay, like a recording source, is an option on the screen
recorder. All options need to be defined as objects and assigned in the
SetOptions() method; they aren’t simple properties that can be defined
directly. My overlay is a dynamically created image with a timestamp,
but an overlay can also be a path to a file.
```
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
```
It’s added as one of the OverlayOptions of the screen recorder. Here all
the options I include in the SetOptions() method, including the
SourceOptions mentioned earlier.
```
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
```
Note there are also audio options available. I disable audio for these
tests, but I could see the value of recording a voice over for a test
execution like this.

While option properties cannot be set directly, they can be updated
during a recording using the recorder’s GetDynamicOptionsBuilder()
method.

