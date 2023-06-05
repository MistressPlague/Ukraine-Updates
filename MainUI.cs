using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using DiscordWebhook;
using HtmlAgilityPack;
using ImgurNet;
using ImgurNet.ApiEndpoints;
using ImgurNet.Authentication;
using ImgurNet.Models;
using Libraries;
using Microsoft.Win32;
using OpenGraphNet;
using Pathoschild.NaturalTimeParser.Parser;
using Svg;
//using TwitterTimeLine;
using HtmlDocument = System.Windows.Forms.HtmlDocument;
using Image = ImgurNet.Models.Image;

namespace Image_Loading_Test
{
    public partial class MainUI : Form
    {
        public ConfigLib<MainConfiguration> MainConfig = new(Environment.CurrentDirectory + "\\Webhooks.json");
        //public ConfigLib<TwitterConfiguration> TwitterConfig = new(Environment.CurrentDirectory + "\\Twitter.json");

        public ConfigLib<TestClass> TestConfig = new(Environment.CurrentDirectory + "\\Test.json");

        public class TestClass
        {
            public bool thing { get; set; } = false;
        }

        private List<Webhook> hooks = new();

        private ImageEndpoint imgur = new(new Imgur(new ClientAuthentication("redacted", false)));

        public MainUI()
        {
            if (!WBEmulator.IsBrowserEmulationSet())
            {
                WBEmulator.SetBrowserEmulationVersion(BrowserEmulationVersion.Version11Edge);
            }

            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e) // Context: I originally wrote this to teach someone some web-scrape coding
        {
            Task.Run(() =>
            {
                // We'll code a twitter post og:video test here; hoping the messagebox will show a video url
                // Here's your foundation

                var ogdata = OpenGraph.ParseUrl(textBox1.Text);

                //ogdata.Metadata // - this is a dictionary, key is the og tag, such as og:image, og:video, etc; the list of values is the array of values attributed; the goal is to get the first valid value and show it in a messagebox of video
                // no python lol


                // We already have all the "packages" we need; we have the opengraph data above in a variable ready to use, and we know metadata holds what we are after, i made a string to hold what you throw in it for the messagebox to show, read above for what that is
                //type below this
                // Why no talk in call? (music) Dis confuse me - coding is how you learn, not thinking -> experiment
                // Need a start? - Im reading it and I dont understand anything

                var Values = string.Join(", ", ogdata.Metadata.SelectMany(q => q.Value).Select(o => o.Name + ": " + o.Value)); // example -> this would give a list of StructuredMetadata -> a class from OpenGraph holding said data - this holds a thing called Value -> the goal is to get ALL of the values in the list, combind them into one and use them for the message box

                MessageBox.Show(Values); // This is what you use to make a message box popup
            });
        }

        //private TwitterTimeLine.TwitterTimeLine TwitterAPI;

        private void Test_Load(object sender, EventArgs e)
        {
            MainConfig.OnConfigUpdated += Config_OnConfigUpdated;

            hooks.Clear();

            foreach (var url in MainConfig.InternalConfig.Webhooks)
            {
                hooks.Add(new Webhook(url));
            }
        }

        private void Config_OnConfigUpdated()
        {
            foreach (var hook in hooks)
            {
                hook.Dispose();
            }

            hooks.Clear();

            foreach (var url in MainConfig.InternalConfig.Webhooks)
            {
                hooks.Add(new Webhook(url));
            }
        }

        public static string WildCardToRegular(string value)
        {
            return "^" + Regex.Escape(value).Replace("\\*", ".*") + "$";
        }

        private static IEnumerable<HtmlElement> ElementsByClass(HtmlDocument doc, string className)
        {
            foreach (HtmlElement e in doc.All)
            {
                if (Regex.IsMatch(e.GetAttribute("className"), WildCardToRegular(className)))
                {
                    yield return e;
                }
            }
        }

        private static IEnumerable<HtmlElement> ElementsByClass(HtmlElement doc, string className)
        {
            foreach (HtmlElement e in doc.All)
            {
                if (Regex.IsMatch(e.GetAttribute("className"), WildCardToRegular(className)))
                {
                    yield return e;
                }
            }
        }

        private bool IsRunning = false;
        private void Checker_Tick(object sender, EventArgs e)
        {
            webBrowser1.Navigate(textBox1.Text);

            while (webBrowser1.ReadyState != WebBrowserReadyState.Complete)
            {
                Application.DoEvents();
            }

            if (IsRunning)
            {
                return;
            }

            var data = webBrowser1.Document;

            var Posts = ElementsByClass(data, "event cat* sourcees").ToList().GetFromTo(0, 10); // Get last 10 posts

            Task.Run(() =>
            {
                IsRunning = true;

                try
                {
                    foreach (var Latest in Posts)
                    {
                        var source = ElementsByClass(Latest, "source-link")?.FirstOrDefault()?.GetAttribute("href");

                        var TimeUntil = Latest.ElementsByClass("date_add")?.FirstOrDefault()?.InnerText;
                        var TimeSincePost = TimeUntil != null ? DateTime.Now.Offset(TimeUntil.Replace("an", "1").Replace("a ", "1 ")) : DateTime.Now;

                        if (source != null)
                        {
                            if (!MainConfig.InternalConfig.PostedSources.Contains(source))
                            {
                                MainConfig.InternalConfig.PostedSources.Add(source);

                                var Region = "";

                                try
                                {
                                    Region = "SVG";

                                    #region SVG

                                    Bitmap svg = null;

                                    var str = ElementsByClass(Latest, "time top-info")?.FirstOrDefault()?.ElementsWhere(o => o != null && !string.IsNullOrEmpty(o.GetAttribute("src"))).FirstOrDefault()?.GetAttribute("src");

                                    if (!string.IsNullOrEmpty(str))
                                    {
                                        if (str.Contains("base64,"))
                                        {
                                            var str2 = str.Substring(str.LastIndexOf("base64,") + "base64,".Length); // Assume there is always a lil' svg image at the top left

                                            var bytes = Convert.FromBase64String(str2);

                                            var str3 = Encoding.Default.GetString(bytes);

                                            var mySvg = SvgDocument.FromSvg<SvgDocument>(str3);

                                            svg = mySvg.Draw(240, 240);

                                            pictureBox2.Image = svg;
                                        }
                                    }

                                    #endregion

                                    Region = "Time";

                                    var Title = ElementsByClass(Latest, "title").FirstOrDefault()?.InnerText;

                                    label2.Text = $"[{TimeUntil}] " + Title;

                                    //var img = post.ElementsByClass("img ")?.FirstOrDefault()?.ElementsWhere(o => o?.InnerText != null &&  o.InnerText.Contains("http"))?.FirstOrDefault()?.InnerText;

                                    //if (!string.IsNullOrEmpty(img))
                                    //{
                                    //    MessageBox.Show(img);

                                    //    pictureBox1.ImageLocation = img;
                                    //}
                                    //else
                                    {
                                        Region = "Link";

                                        var link = Latest.GetAttribute("data-link");

                                        if (!string.IsNullOrEmpty(source))
                                        {
                                            Region = "OpenGraph";

                                            var odata = OpenGraph.ParseUrl(source);

                                            Region = "Image URL";

                                            var ImageURL = odata?.Image?.AbsoluteUri;

                                            if (ImageURL != null && ImageURL.Length > 3)
                                            {
                                                if (ImageURL.Where(o => o == ':').ToList().Count > 1)
                                                {
                                                    ImageURL = ImageURL.Substring(0, ImageURL.LastIndexOf(':'));
                                                }

                                                pictureBox1.Load(ImageURL);
                                            }

                                            //Region = "Video URL";

                                            //var VideoURL = odata?.Metadata["og:video"]?.FirstOrDefault(o => o.Value != null)?.Value;

                                            //MessageBox.Show(VideoURL + "\r\n" + JsonConvert.SerializeObject(odata.Metadata.Where(o => o.Key.ToLower().Contains("video"))));

                                            Region = "Save Image";

                                            ImgurResponse<Image> Upload = null;

                                            using (var stream = new MemoryStream())
                                            {
                                                svg.Save(stream, ImageFormat.Png);

                                                Region = "Upload Image";

                                                try
                                                {
                                                    Upload = imgur.UploadImageFromBinaryAsync(stream.ToArray())?.Result;
                                                }
                                                catch (Exception ex)
                                                {
                                                    MessageBox.Show($"Error Uploading To Imgur: {ex}");
                                                }
                                            }

                                            Region = "Colour Calc";

                                            // Send Webhook

                                            var Col = svg.GetPixel(25, 115);

                                            var ConvCol = Color.FromArgb(Col.R, Col.G, Col.B).ToRgb();

                                            Region = "VidPost";
                                            var VidPost = Latest.GetAttribute("data-twitpic");

                                            Region = "Send Webhook";

                                            Embed[] Embed;

                                            if (ImageURL != null && ImageURL.Length > 3)
                                            {
                                                Embed = new[] { new Embed { Author = new EmbedAuthor { Name = "Ukraine News", Url = link }, Title = "New Update", Color = ConvCol, Description = Title + (!string.IsNullOrEmpty(VidPost) && VidPost.Contains("video") ? $"\r\n\r\n[Video Link]({VidPost})" : ""), Image = new EmbedImage { Url = ImageURL, Width = 720, Height = 720 }, Url = link, TimeStamp = TimeSincePost, Footer = new EmbedFooter { Text = "Developed By Kanna Kamui#0001" }, Thumbnail = new EmbedThumbnail { Width = 248, Height = 248, Url = Upload.Data.Link } } };
                                            }
                                            else
                                            {
                                                Embed = new[] { new Embed { Author = new EmbedAuthor { Name = "Ukraine News", Url = link }, Title = "New Update", Color = ConvCol, Description = Title + (!string.IsNullOrEmpty(VidPost) && VidPost.Contains("video") ? $"\r\n\r\n[Video Link]({VidPost})" : ""), Url = link, TimeStamp = TimeSincePost, Footer = new EmbedFooter { Text = "Developed By Kanna Kamui#0001" }, Thumbnail = new EmbedThumbnail { Width = 248, Height = 248, Url = Upload.Data.Link } } };
                                            }

                                            for (var index = 0; index < hooks.Count; index++)
                                            {
                                                var hook = hooks[index];

                                                try
                                                {
                                                    hook.Send("", embeds: Embed);
                                                }
                                                catch (Exception ex)
                                                {
                                                    textBox2.AppendText("Caught error On Hook.Send!\r\n\r\n" + ex + "\r\n");
                                                }
                                            }
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    textBox2.AppendText(Region + "\r\n\r\n" + ex + "\r\n");
                                }
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                catch
                {

                }

                IsRunning = false;
            });
        }

        private void Test_FormClosed(object sender, FormClosedEventArgs e)
        {
            Environment.Exit(0);
        }

        public class MainConfiguration
        {
            public List<string> Webhooks { get; set; } = new();
            public List<string> PostedSources { get; set; } = new();
        }

        public class TwitterConfiguration
        {
            public List<string> TwitterUsersToMonitor { get; set;} = new();
            public List<string> PostedTweets { get; set;} = new();
        }

        private async void TweetTimer_Tick(object sender, EventArgs e)
        {
            //MessageBox.Show("TweetTimer_Tick Start");

            //try
            //{
            //    foreach (var user in TwitterConfig.InternalConfig.TwitterUsersToMonitor)
            //    {
            //        var TPosts = (TwitterAPI.GetTweets(user))
            //            .Where(a => 
                            
            //            (a.Text.ToLower().Contains("russia") || a.Text.ToLower().Contains("ukrain") || a.Text.ToLower().Contains("kremlin") || a.Text.ToLower().Contains("putin") || a.Text.ToLower().Contains("kyiv"))
            //            &&
            //            (a.CreatedAt.ToUniversalTime().Subtract(DateTime.UtcNow).Minutes < (((TweetTimer.Interval / 1000) / 60) + 1)))
                        
            //            .OrderBy(o => o.CreatedAt)
                        
            //            .Where(o => !TwitterConfig.InternalConfig.PostedTweets.Contains("")); // Get Posts In Last 3 Mins

            //        foreach (var Post in TPosts)
            //        {
            //            //TwitterHook.Send($"[<t:{Post.CreatedAt.Subtract(new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds}>] https://twitter.com/{UserGrabbed.Username}/status/{Post.Id}");

            //            TwitterConfig.InternalConfig.PostedTweets.Add("");
            //        }
            //    }
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show(ex.ToString());
            //}

            //MessageBox.Show("TweetTimer_Tick End");
        }

        public static DateTime ConvertFromUnixTimestamp(double timestamp)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            return origin.AddSeconds(timestamp);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //TwitterAPI.GetTweets("POTUS");
        }
    }

    public static class Ext
    {
        public static List<HtmlNode> AllDescendants(this HtmlNode parent)
        {
            var list = new List<HtmlNode>();

            foreach (var a in parent.Descendants())
            {
                list.Add(a);

                list.AddRange(a.AllDescendants());
            }

            return list;
        }

        public static IEnumerable<HtmlElement> ElementsByClass(this HtmlDocument doc, string className)
        {
            foreach (HtmlElement e in doc.All)
            {
                if (Regex.IsMatch(e.GetAttribute("className"), MainUI.WildCardToRegular(className)))
                {
                    yield return e;
                }
            }
        }

        public static IEnumerable<HtmlElement> ElementsByClass(this HtmlElement doc, string className)
        {
            foreach (HtmlElement e in doc.All)
            {
                if (Regex.IsMatch(e.GetAttribute("className"), MainUI.WildCardToRegular(className)))
                {
                    yield return e;
                }
            }
        }

        public static IEnumerable<HtmlElement> ElementsWhere(this HtmlElement doc, Func<HtmlElement, bool> Condition)
        {
            foreach (HtmlElement e in doc.All)
            {
                if (Condition(e))
                {
                    yield return e;
                }
            }
        }
    }

    public enum BrowserEmulationVersion
    {
        Default = 0,
        Version7 = 7000,
        Version8 = 8000,
        Version8Standards = 8888,
        Version9 = 9000,
        Version9Standards = 9999,
        Version10 = 10000,
        Version10Standards = 10001,
        Version11 = 11000,
        Version11Edge = 11001
    }

    public static class WBEmulator
    {
        private const string InternetExplorerRootKey = @"Software\Microsoft\Internet Explorer";
        private const string BrowserEmulationKey = InternetExplorerRootKey + @"\Main\FeatureControl\FEATURE_BROWSER_EMULATION";

        public static int GetInternetExplorerMajorVersion()
        {
            int result;

            result = 0;

            try
            {
                RegistryKey key;

                key = Registry.LocalMachine.OpenSubKey(InternetExplorerRootKey);

                if (key != null)
                {
                    object value;

                    value = key.GetValue("svcVersion", null) ?? key.GetValue("Version", null);

                    if (value != null)
                    {
                        string version;
                        int separator;

                        version = value.ToString();
                        separator = version.IndexOf('.');
                        if (separator != -1)
                        {
                            int.TryParse(version.Substring(0, separator), out result);
                        }
                    }
                }
            }
            catch (SecurityException)
            {
                // The user does not have the permissions required to read from the registry key.
            }
            catch (UnauthorizedAccessException)
            {
                // The user does not have the necessary registry rights.
            }

            return result;
        }

        public static BrowserEmulationVersion GetBrowserEmulationVersion()
        {
            BrowserEmulationVersion result;

            result = BrowserEmulationVersion.Default;

            try
            {
                RegistryKey key;

                key = Registry.CurrentUser.OpenSubKey(BrowserEmulationKey, true);
                if (key != null)
                {
                    string programName;
                    object value;

                    programName = Path.GetFileName(Environment.GetCommandLineArgs()[0]);
                    value = key.GetValue(programName, null);

                    if (value != null)
                    {
                        result = (BrowserEmulationVersion)Convert.ToInt32(value);
                    }
                }
            }
            catch (SecurityException)
            {
                // The user does not have the permissions required to read from the registry key.
            }
            catch (UnauthorizedAccessException)
            {
                // The user does not have the necessary registry rights.
            }

            return result;
        }

        public static bool SetBrowserEmulationVersion(BrowserEmulationVersion browserEmulationVersion)
        {
            bool result;

            result = false;

            try
            {
                RegistryKey key;

                key = Registry.CurrentUser.OpenSubKey(BrowserEmulationKey, true);

                if (key != null)
                {
                    string programName;

                    programName = Path.GetFileName(Environment.GetCommandLineArgs()[0]);

                    if (browserEmulationVersion != BrowserEmulationVersion.Default)
                    {
                        // if it's a valid value, update or create the value
                        key.SetValue(programName, (int)browserEmulationVersion, RegistryValueKind.DWord);
                    }
                    else
                    {
                        // otherwise, remove the existing value
                        key.DeleteValue(programName, false);
                    }

                    result = true;
                }
            }
            catch (SecurityException)
            {
                // The user does not have the permissions required to read from the registry key.
            }
            catch (UnauthorizedAccessException)
            {
                // The user does not have the necessary registry rights.
            }

            return result;
        }

        public static bool SetBrowserEmulationVersion()
        {
            int ieVersion;
            BrowserEmulationVersion emulationCode;

            ieVersion = GetInternetExplorerMajorVersion();

            if (ieVersion >= 11)
            {
                emulationCode = BrowserEmulationVersion.Version11;
            }
            else
            {
                switch (ieVersion)
                {
                    case 10:
                        emulationCode = BrowserEmulationVersion.Version10;
                        break;
                    case 9:
                        emulationCode = BrowserEmulationVersion.Version9;
                        break;
                    case 8:
                        emulationCode = BrowserEmulationVersion.Version8;
                        break;
                    default:
                        emulationCode = BrowserEmulationVersion.Version7;
                        break;
                }
            }

            return SetBrowserEmulationVersion(emulationCode);
        }

        public static bool IsBrowserEmulationSet()
        {
            return GetBrowserEmulationVersion() != BrowserEmulationVersion.Default;
        }
    }
}