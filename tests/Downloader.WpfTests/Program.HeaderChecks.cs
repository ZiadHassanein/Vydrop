using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using YouTubeDownloader;

internal static partial class Program
{
    private static async Task CheckHeader(Window owner, MainViewModel model, string captures)
    {
        var window = (MainWindow)owner;
        var header = window.HeaderContent;
        var language = header.LanguageButton;
        MotionPreferences.SetTestOverride(false);
        try
        {
            model.NewDownloadCommand.Execute(null); model.Url = "https://youtu.be/jNQXAC9IVRw";
            await WaitFor(() => model.CanDownload, "The header fixture uses an inspected video");
            model.Audio = true;
            var quality = model.SelectedQuality;
            var folder = model.Folder;
            foreach (bool arabic in new[] { false, true }) foreach (bool dark in new[] { false, true }) foreach (double width in new[] { window.MinWidth, 1280d })
            {
                model.SelectLanguageCommand.Execute(arabic ? "ar" : "en"); if (model.IsDark != dark) model.ThemeCommand.Execute(null);
                window.Width = width; await Task.Delay(80); window.UpdateLayout();
                Rect Bounds(FrameworkElement element) => element.TransformToAncestor(window).TransformBounds(new Rect(element.RenderSize));
                var before = Bounds(language);
                var brand = (FrameworkElement)WpfTree.Find(window, "BrandWordmark");
                var navigation = Bounds(window.ManagerContent.NavigationSurface);
                Check(language.ContextMenu == null && ReferenceEquals(language.Command, model.ToggleLanguageCommand) && language.IsEnabled,
                    "Language is a direct button with no dropdown or detached popup");
                Check(Descendants<TextBlock>(language).Single().Text == (arabic ? "English" : "العربية") && AutomationProperties.GetName(language) == model.LanguageAction,
                    "The button names the destination language and exposes a localized accessible action");
                Check(before.Left >= 0 && before.Right <= window.ActualWidth && before.Width == 106 && Bounds(brand).Left >= navigation.Left && Bounds(brand).Right <= navigation.Right,
                    "Header controls and branding fit the compact and wide layouts in both directions");
                language.Focus();
                ((IInvokeProvider)new ButtonAutomationPeer(language).GetPattern(PatternInterface.Invoke)).Invoke();
                await WaitFor(() => model.IsArabic != arabic, "One invocation switches the app language");
                await Task.Delay(50); window.UpdateLayout();
                Check(model.SelectedQuality.Key == quality.Key && model.SelectedQuality.Bitrate == quality.Bitrate && model.Folder == folder && model.HasMediaPreview && language.IsKeyboardFocusWithin,
                    "Switching language preserves the chosen output, folder, preview and keyboard focus");
                Check(language.ActualWidth == before.Width && language.ContextMenu == null, "Switching label and script does not resize the button or open a popup");
                Capture(window, System.IO.Path.Combine(captures, $"language-button-{arabic}-{dark}-{width}.png"));
                foreach (var routed in new[] { Keyboard.KeyDownEvent, Keyboard.KeyUpEvent })
                    language.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, PresentationSource.FromVisual(language), Environment.TickCount, Key.Space) { RoutedEvent = routed });
                await WaitFor(() => model.IsArabic == arabic, "Space operates the same language button from the keyboard");
            }
            var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var job = model.Queue.Add(new DownloadOptions { Url = "https://example.com/busy" });
            var active = model.Queue.RunJobAsync(job.Id, async (_, _, _) => { await release.Task; return new DownloadResult { Cancelled = true }; });
            try
            {
                bool previous = model.IsArabic;
                Check(!model.ToggleLanguageCommand.CanExecute(null) && !language.IsEnabled, "Language switching retains the app's existing busy-operation guard");
                model.ToggleLanguageCommand.Execute(null);
                Check(model.IsArabic == previous, "Busy downloads cannot change language through a direct command call");
            }
            finally { release.SetResult(); await active; model.Queue.Remove(job.Id); }
            Check(model.ToggleLanguageCommand.CanExecute(null) && language.IsEnabled, "The language action becomes available when the operation ends");
        using var iconStream = Application.GetResourceStream(new Uri("pack://application:,,,/Vydrop;component/Assets/app.ico")).Stream;
        var frames = new IconBitmapDecoder(iconStream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad).Frames;
        Check(frames.Select(frame => frame.PixelWidth).SequenceEqual(new[] { 16, 24, 32, 48, 64, 128, 256 }),
            "The app icon contains seven native sizes instead of a stretched small bitmap");
        using var logoStream = typeof(Program).Assembly.GetManifestResourceStream("Vydrop.Tests.HeaderLogo");
        var logo = new FormatConvertedBitmap(BitmapFrame.Create(logoStream), PixelFormats.Bgra32, null, 0);
        byte[] pixels = new byte[logo.PixelWidth * logo.PixelHeight * 4];
        logo.CopyPixels(pixels, logo.PixelWidth * 4, 0);
        int armPixel = (128 * 512 + 128) * 4;
        int arrowPixel = (256 * 512 + 256) * 4;
        int pointPixel = (384 * 512 + 256) * 4;
        Check(logo.PixelWidth == 512 && pixels[3] == 0 &&
            pixels[arrowPixel] == 255 && pixels[arrowPixel + 1] == 255 && pixels[arrowPixel + 2] == 255 && pixels[arrowPixel + 3] == 255 &&
            pixels[pointPixel + 3] == 255 && pixels[pointPixel] == 0x4D && pixels[pointPixel + 1] == 0x3D && pixels[pointPixel + 2] == 0xE6 &&
            pixels[armPixel + 3] == 255 && pixels[armPixel] == 0x4D && pixels[armPixel + 1] == 0x3D && pixels[armPixel + 2] == 0xE6,
            "The V-only icon retains transparent edges, a solid white download arrow and its red lower point");
            foreach (string key in new[] { "LanguagesIcon", "LibraryIcon", "VideoIcon" })
            {
                var bounds = ((Geometry)Application.Current.Resources[key]).Bounds;
                Check(bounds.Left >= 0 && bounds.Top >= 0 && bounds.Right <= 24 && bounds.Bottom <= 24, "Icon geometry retains the pinned 24-unit coordinate system: " + key);
            }
        }
        finally { model.Url = ""; model.SelectLanguageCommand.Execute("en"); MotionPreferences.SetTestOverride(null); }
    }
}
