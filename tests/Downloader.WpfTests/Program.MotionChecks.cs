using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using YouTubeDownloader;

internal static partial class Program
{
    private static async Task CaptureLogoPreview(MainWindow window, MainViewModel model, string captures, bool downloadButton = false)
    {
        var brand = (Button)WpfTree.Find(window, downloadButton ? "DownloadButton" : "BrandButton");
        var artwork = downloadButton ? brand : (FrameworkElement)WpfTree.Find(window, "BrandArtwork");
        double width = downloadButton ? 280 : 128, height = downloadButton ? 52 : 38;
        double renderScale = downloadButton ? 2 : 3;
        if (downloadButton)
        {
            model.Url = "https://youtu.be/jNQXAC9IVRw";
            for (int wait = 0; wait < 100 && !model.CanDownload; wait++) await Task.Delay(25);
            Check(model.CanDownload, "The native button preview uses an inspected, enabled action");
            brand.BringIntoView();
        }
        MotionPreferences.SetTestOverride(true);
        foreach (bool dark in new[] { true, false })
        {
            if (model.IsDark != dark) model.ThemeCommand.Execute(null);
            await Task.Delay(350);
            string folder = Path.Combine(captures, dark ? "dark" : "light");
            Directory.CreateDirectory(folder);
            var samples = new List<double>();
            var timer = System.Diagnostics.Stopwatch.StartNew();
            brand.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, Environment.TickCount) { RoutedEvent = Mouse.MouseEnterEvent });
            for (int frame = 0; frame < 90; frame++)
            {
                samples.Add(timer.Elapsed.TotalSeconds);
                var drawing = new DrawingVisual();
                using (var canvas = drawing.RenderOpen())
                {
                    canvas.DrawRectangle((Brush)Application.Current.Resources["Page"], null, new Rect(0, 0, width + 48, height + 36));
                    canvas.DrawRectangle(new VisualBrush(artwork), null, new Rect(24, 18, width, height));
                }
                var bitmap = new System.Windows.Media.Imaging.RenderTargetBitmap((int)((width + 48) * renderScale), (int)((height + 36) * renderScale), 96 * renderScale, 96 * renderScale, PixelFormats.Pbgra32);
                bitmap.Render(drawing);
                var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
                using (var output = File.Create(Path.Combine(folder, $"frame{frame:000}.png"))) encoder.Save(output);
                await Task.Delay(33);
            }
            using (var timeline = File.CreateText(Path.Combine(folder, "frames.ffconcat")))
            {
                timeline.WriteLine("ffconcat version 1.0");
                for (int frame = 0; frame < samples.Count; frame++)
                {
                    timeline.WriteLine($"file 'frame{frame:000}.png'");
                    double duration = frame + 1 < samples.Count ? samples[frame + 1] - samples[frame] : 1d / 30;
                    timeline.WriteLine("duration " + duration.ToString("F6", System.Globalization.CultureInfo.InvariantCulture));
                }
                timeline.WriteLine($"file 'frame{samples.Count - 1:000}.png'");
            }
            brand.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, Environment.TickCount) { RoutedEvent = Mouse.MouseLeaveEvent });
        }
        MotionPreferences.SetTestOverride(null);
        Console.WriteLine("Native logo animation frames: " + captures);
    }

    // Transfer feedback now belongs to the manager; the editor closes after Start.
    private static async Task CheckMotion(MainWindow window, MainViewModel model, FakeProvider provider, string captures)
    {
        var brand = window.HeaderContent.BrandButton;
        var light = (System.Windows.Shapes.Path)WpfTree.Find(brand, "LogoLight");
        var transition = window.ThemeTransition;
        bool previousDark = model.IsDark;
        async Task Until(Func<bool> ready, string label)
        {
            for (int attempt = 0; attempt < 100 && !ready(); attempt++) await Task.Delay(20);
            Check(ready(), label);
        }
        try
        {
            MotionPreferences.SetTestOverride(true);
            model.CloseAddDownloadCommand.Execute(null);
            foreach (string language in new[] { "en", "ar" }) foreach (bool dark in new[] { false, true })
            {
                model.SelectLanguageCommand.Execute(language); if (model.IsDark != dark) model.ThemeCommand.Execute(null);
                await Task.Delay(400);
                var original = brand.TransformToAncestor(window).TransformBounds(new Rect(brand.RenderSize));
                brand.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, Environment.TickCount) { RoutedEvent = Mouse.MouseEnterEvent });
                await Until(() => light.Opacity > 0, "The idle manager wordmark has a finite hover response: " + language + "/" + dark);
                Check(brand.TransformToAncestor(window).TransformBounds(new Rect(brand.RenderSize)) == original && provider.DownloadCalls == 0, "Brand motion never moves layout or starts downloads");
                brand.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, Environment.TickCount) { RoutedEvent = Mouse.MouseLeaveEvent });
                await Until(() => light.Opacity == 0, "Pointer exit settles the manager wordmark");
            }
            model.NewDownloadCommand.Execute(null);
            model.Url = "https://youtu.be/jNQXAC9IVRw";
            await Until(() => model.CanDownload, "Inspection prepares the real editor without simulating transfer progress");
            Check(model.Progress == 0 && model.DownloadActionState == DownloadActionState.Ready, "Ready metadata is distinct from transfer progress");
            model.Folder = Path.GetFullPath(Path.Combine(captures, "motion-fixtures")); Directory.CreateDirectory(model.Folder);
            provider.DownloadHandler = (_, token, progress) =>
            {
                progress.Report(new DownloadProgress { Stage = "Downloading", Percent = 20 });
                token.WaitHandle.WaitOne(); token.ThrowIfCancellationRequested(); return new DownloadResult();
            };
            var task = model.DownloadCommand.ExecuteAsync(null);
            await Until(() => model.IsBusy && model.Progress == 20 && model.SelectedLibraryItem?.Percent == 20, "The active manager receives actual transfer progress");
            Check(!model.AddDownloadVisible && model.SelectedLibraryItem.IsActive && model.LibraryCancelCommand.CanExecute(model.SelectedLibraryItem.Id), "Starting transitions from editor to the selected transfer with a reachable Cancel action");
            model.LibraryCancelCommand.Execute(model.SelectedLibraryItem.Id); await task;
            Check(model.SelectedLibraryItem.Entry.State == QueueState.Cancelled && model.DownloadActionState == DownloadActionState.Ready, "Cancellation never animates or records a successful transfer");
            model.CloseAddDownloadCommand.Execute(null);
            model.ThemeCommand.Execute(null);
            await Until(() => !transition.IsTransitioning, "A theme transition settles and releases its snapshot");
            ((FrameworkElement)window.Content).InvalidateMeasure();
            model.ThemeCommand.Execute(null);
            Check(!transition.IsTransitioning, "A pending layout applies the palette without forcing virtualized row generation into a snapshot");
            await Task.Delay(80);
            MotionPreferences.SetTestOverride(false);
            brand.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, Environment.TickCount) { RoutedEvent = Mouse.MouseEnterEvent });
            await Task.Delay(80);
            Check(light.Opacity == 0, "Reduced motion suppresses decorative brand animation");
            model.ThemeCommand.Execute(null);
            Check(!transition.IsTransitioning, "Reduced motion applies theme changes immediately");
        }
        finally
        {
            provider.DownloadHandler = null;
            model.CancelCommand.Execute(null);
            MotionPreferences.SetTestOverride(null);
            model.Url = "";
            model.SelectLanguageCommand.Execute("en");
            if (model.IsDark != previousDark) model.ThemeCommand.Execute(null);
        }
    }
}
