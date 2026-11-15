using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using YouTubeDownloader;

internal static partial class Program
{
    private static async Task CheckContactLinks(MainWindow window, MainViewModel model, string captures)
    {
        model.Url = "";
        model.LibraryFilterCommand.Execute("all");
        Check(WpfTree.Find(window.ManagerContent.NavigationSurface, "ContactButton") == null &&
            !Descendants<TextBlock>(window.ManagerContent.NavigationSurface).Any(text => text.Text == model.ContactLabel),
            "Contact stays outside the main navigation");
        foreach (string locale in new[] { "en", "ar" })
            foreach (bool dark in new[] { false, true })
                foreach (double width in new[] { 1280d, 760d })
                {
                    model.SelectLanguageCommand.Execute(locale);
                    if (model.IsDark != dark) model.ThemeCommand.Execute(null);
                    model.LibraryFilterCommand.Execute("all");
                    window.Width = width;
                    await Task.Delay(180);
                    window.UpdateLayout();
                    var section = (FrameworkElement)WpfTree.Find(window.ContactFooter, "ContactSection");
                    var heading = (TextBlock)WpfTree.Find(section, "ContactHeading");
                    var actions = (WrapPanel)WpfTree.Find(section, "ContactActions");
                    var links = actions.Children.OfType<Button>().ToArray();
                    Check(section.IsVisible && heading.Text == (locale == "ar" ? "تواصل معي" : "Contact me") &&
                        section.FlowDirection == model.ContentDirection && !Descendants<TextBlock>(section).Any(text =>
                            text.Text.Contains("Ziad", StringComparison.OrdinalIgnoreCase) || text.Text.Contains("زياد") || text.Text == "Developed by"),
                        "The permanent contact footer is localized and does not add an author name or byline: " + locale);
                    Check(links.Length == 3 && links.All(link => ReferenceEquals(link.Command, model.SocialCommand) &&
                        link.IsVisible && link.IsEnabled && link.ContextMenu == null && link.ActualHeight >= 36) &&
                        links.Select(link => (string)link.CommandParameter).SequenceEqual(new[] { "github", "linkedin", "email" }) &&
                        AutomationProperties.GetName(links[2]) == model.EmailLabel,
                        "GitHub, LinkedIn and Email remain directly available through the existing contact commands");
                    Rect Bounds(FrameworkElement element) => element.TransformToAncestor(window.Root).TransformBounds(new Rect(element.RenderSize));
                    var visible = new Rect(0, 0, window.Root.ActualWidth, window.Root.ActualHeight);
                    var footer = Bounds(window.ContactFooterSurface);
                    Check(visible.Contains(Bounds(heading)) && links.All(link => visible.Contains(Bounds(link))) &&
                        links.All(link => !Bounds(link).IntersectsWith(Bounds(heading))) && footer.Height <= 50 &&
                        Math.Abs(footer.Bottom - visible.Bottom) <= 1 && Bounds(window.ManagerContent).Bottom <= footer.Top + 1,
                        "The compact contact footer fits at the bottom without overlapping content or contact labels");
                    Check(links.Select(link => (string)link.ToolTip).SequenceEqual(new[] {
                        "https://github.com/ZiadHassanein", "https://www.linkedin.com/in/ziad-hassanein/", "ziadmuhammed501@gmail.com" }) &&
                        DesktopActions.ContactUrl("email") == "mailto:ziadmuhammed501@gmail.com" &&
                        !Descendants<ContactLinksView>(window.SettingsPane).Any(),
                        "Footer links expose the real existing destinations and Settings does not repeat them");
                    foreach (var (page, open) in new (string, Action)[] {
                        ("queue", () => model.LibraryFilterCommand.Execute("queue")),
                        ("history", () => model.HistoryCommand.Execute(null)),
                        ("download", () => model.NewDownloadCommand.Execute(null)),
                        ("settings", () => model.SettingsVisible = true) })
                    {
                        open(); await Task.Delay(40); window.UpdateLayout();
                        Check(section.IsVisible && links.All(link => link.IsVisible && link.IsEnabled) &&
                            Math.Abs(Bounds(window.ContactFooterSurface).Bottom - visible.Bottom) <= 1,
                            "Contacts stay visible on " + page);
                    }
                    window.Activate();
                    links[0].Focus();
                    Check(links[0].IsKeyboardFocused, "The first contact link can receive keyboard focus");
                    links[0].MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                    Check(links[1].IsKeyboardFocused, "Tab advances directly to LinkedIn");
                    links[1].MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                    Check(links[2].IsKeyboardFocused, "Tab advances directly to Email without opening a menu");
                    string capture = Path.GetFullPath(Path.Combine(captures, $"contact-{locale}-{(dark ? "dark" : "light")}-{width}.png"));
                    Capture(window, capture);
                    var frame = new PngBitmapDecoder(new Uri(capture), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad).Frames[0];
                    int footerTop = (int)Math.Round(footer.Top * VisualTreeHelper.GetDpi(window.Root).DpiScaleY);
                    var crop = new CroppedBitmap(frame, new Int32Rect(0, footerTop, frame.PixelWidth, frame.PixelHeight - footerTop));
                    var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(crop));
                    using (var stream = File.Create(Path.Combine(captures, $"footer-{locale}-{(dark ? "dark" : "light")}-{width}.png"))) encoder.Save(stream);
                    model.SettingsVisible = false;
                    Check(section.IsVisible, "Leaving Settings keeps the contact footer visible");
                }
        model.SelectLanguageCommand.Execute("en");
        window.Width = 1280;
    }
}
