using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
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

    private static async Task CheckMotion(MainWindow window, MainViewModel model, FakeProvider provider, string captures)
    {
        string oldFolder = model.Folder;
        bool oldAudio = model.Audio, oldDark = model.IsDark, oldArabic = model.IsArabic;
        var oldEncoding = model.SelectedEncoding;
        var oldQuality = model.SelectedQuality;
        double oldWidth = window.Width, oldHeight = window.Height;
        string fixtureFolder = System.IO.Path.GetFullPath(System.IO.Path.Combine(captures, "motion-fixtures"));
        System.IO.Directory.CreateDirectory(fixtureFolder);
        string fixtureFile = System.IO.Path.Combine(fixtureFolder, "Local UI fixture.mp4");
        System.IO.File.WriteAllText(fixtureFile, "Owned UI fixture; not playable media.");
        var action = (FrameworkElement)WpfTree.Find(window, "DownloadAction");
        var button = (Button)WpfTree.Find(window, "DownloadButton");
        var arrow = (System.Windows.Shapes.Path)WpfTree.Find(action, "DownloadArrow");
        var check = (System.Windows.Shapes.Path)WpfTree.Find(action, "CompleteCheck");
        var arrowOffset = (TranslateTransform)arrow.RenderTransform;
        var scale = (ScaleTransform)button.Template.FindName("ProgressScale", button);
        var buttonLight = (FrameworkElement)button.Template.FindName("DownloadShine", button);
        var buttonSurface = (Border)button.Template.FindName("DownloadShineSurface", button);
        var header = (FrameworkElement)WpfTree.Find(window, "HeaderContent");
        var brand = (Button)WpfTree.Find(header, "BrandButton");
        var light = (System.Windows.Shapes.Path)WpfTree.Find(header, "LogoLight");
        var artwork = (FrameworkElement)WpfTree.Find(header, "BrandArtwork");
        var logoScale = (ScaleTransform)((FrameworkElement)WpfTree.Find(header, "BrandInitial")).RenderTransform;
        var viewport = (ScrollViewer)WpfTree.Find(window, "Viewport");
        var transition = (ThemeTransitionView)WpfTree.Find(window, "ThemeTransition");
        async Task Until(Func<bool> predicate, string label)
        {
            for (int attempt = 0; attempt < 150 && !predicate(); attempt++) await Task.Delay(20);
            Check(predicate(), label + $" (busy={model.IsBusy}, progress={model.Progress}, phase={model.DownloadActionState}, status={model.StatusHeading}: {model.StatusDetail})");
        }
        Rect Bounds(FrameworkElement view) => view.TransformToAncestor(window).TransformBounds(new Rect(view.RenderSize));
        void Shot(string name) { window.UpdateLayout(); Capture(window, System.IO.Path.Combine(captures, "motion-" + name + ".png"), transitionFrame: name == "theme-midpoint"); }
        async Task Input()
        {
            model.Url = "";
            model.Url = "https://youtu.be/jNQXAC9IVRw";
            await Until(() => model.CanDownload, "Fixture inspection finishes without starting download motion");
            Check(model.DownloadActionState == DownloadActionState.Ready && scale.ScaleX == 0 && check.Visibility == Visibility.Hidden,
                "Metadata inspection cannot imitate a download or a completed file");
        }
        try
        {
            MotionPreferences.SetTestOverride(true);
            // Keep unrelated desktop input out of deterministic fixture phases.
            // The explicit native-pointer phase below restores hit testing/focus.
            window.IsHitTestVisible = false;
            window.Width = 740;
            window.Height = Math.Min(800, window.CurrentWorkArea().Height - 16);
            model.SettingsVisible = false;
            model.Folder = fixtureFolder;
            model.SelectedEncoding = model.Encodings.First(choice => choice.Encoding == AudioEncoding.Mp3);
            model.Audio = true;
            await Input();
            model.SelectedQuality = model.Qualities.First(q => q.Bitrate == 320);
            model.Url = "";
            foreach (string locale in new[] { "en", "ar" })
                foreach (bool dark in new[] { true, false })
                {
                    model.SelectLanguageCommand.Execute(locale);
                    if (model.IsDark != dark) model.ThemeCommand.Execute(null);
                    await Task.Delay(320);
                    viewport.ScrollToTop();
                    await Task.Delay(30);
                    string variant = locale + (dark ? "-dark" : "-light");
                    var frame = new Rect(window.Left, window.Top, window.ActualWidth, window.ActualHeight);
                    double headerHeight = header.ActualHeight;
                    double headingTop = Bounds((FrameworkElement)WpfTree.Find(window, "NewDownloadHeading")).Top;
                    var logoBounds = Bounds(artwork);
                    brand.Focus();
                    brand.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    // Wait for a rendered animation tick; elapsed wall time alone is not a WPF frame.
                    await Until(() => light.Opacity > 0, "The logo begins its finite light pass in " + variant);
                    Check(Math.Abs(header.ActualHeight - headerHeight) < .1 &&
                        Math.Abs(Bounds((FrameworkElement)WpfTree.Find(window, "NewDownloadHeading")).Top - headingTop) < .1,
                        "Clicking the logo animates inside fixed bounds without moving the form in " + variant);
                    Check(Bounds(artwork).Width <= logoBounds.Width + .5,
                        "Logo light pass stays inside its fixed silhouette in " + variant + $" (before={logoBounds}, after={Bounds(artwork)}, opacity={light.Opacity})");
                    await Until(() => light.Opacity > .9 && light.Fill is LinearGradientBrush { RelativeTransform: TranslateTransform { X: > -.1 and < .1 } },
                        "The visible shine crosses the center of the full logo in " + variant);
                    Shot("logo-" + variant);
                    await Until(() => light.Opacity == 0, "The finite logo shine ends in " + variant);
                    Check(light.Opacity == 0 && logoScale.ScaleX == 1 && logoScale.ScaleY == 1,
                        "The logo settles without a decorative loop in " + variant);
                    brand.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, Environment.TickCount) { RoutedEvent = Mouse.MouseEnterEvent });
                    await Until(() => light.Opacity > 0, "Hover starts the whole-wordmark shine in " + variant);
                    var brandFace = (Border)brand.Template.FindName("Face", brand);
                    var lettering = (System.Windows.Shapes.Path)WpfTree.Find(header, "BrandLetters");
                    Check(light.Data.Bounds.Right >= lettering.Data.Bounds.Right &&
                        brandFace.Background is SolidColorBrush { Color.A: 0 } && logoScale.ScaleX == 1 && logoScale.ScaleY == 1,
                        "Hover shines through the lettering without a background plate or logo movement in " + variant);
                    brand.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, Environment.TickCount) { RoutedEvent = Mouse.MouseLeaveEvent });
                    await Until(() => light.Opacity == 0, "Pointer exit gently fades the logo shine in " + variant);
                    Check(light.Opacity == 0, "Pointer exit clears the logo shine in " + variant);
                    await Input();
                    action.BringIntoView();
                    await Task.Delay(50);
                    var before = Bounds(button);
                    int beforeHover = provider.DownloadCalls;
                    button.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, Environment.TickCount) { RoutedEvent = Mouse.MouseEnterEvent });
                    await Until(() => buttonLight.Opacity > .9 && buttonSurface.OpacityMask is LinearGradientBrush { RelativeTransform: TranslateTransform { X: > -.1 and < .1 } },
                        "Hover plays the slow button shine in " + variant);
                    Check(model.DownloadActionState == DownloadActionState.Ready && scale.ScaleX == 0 && provider.DownloadCalls == beforeHover && Bounds(button) == before,
                        "Button shine never starts a download, imitates progress or changes layout in " + variant + $" (mode={button.ClickMode}, state={model.DownloadActionState}, calls={beforeHover}->{provider.DownloadCalls}, bounds={before}->{Bounds(button)})");
                    Shot("download-hover-" + variant);
                    button.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, Environment.TickCount) { RoutedEvent = Mouse.MouseLeaveEvent });
                    await Until(() => buttonLight.Opacity == 0, "Pointer exit settles the button shine in " + variant);
                    using var finish = new ManualResetEventSlim();
                    provider.DownloadHandler = (_, token, progress) =>
                    {
                        progress.Report(new DownloadProgress { Stage = "Downloading", Detail = "Local animation fixture", Percent = 20 });
                        finish.Wait(token);
                        return new DownloadResult { FilePath = fixtureFile, Files = new() { fixtureFile }, Completed = 1, Total = 1 };
                    };
                    int calls = provider.DownloadCalls;
                    var invoke = (IInvokeProvider)new ButtonAutomationPeer(button).GetPattern(PatternInterface.Invoke);
                    invoke.Invoke();
                    await Until(() => model.Progress == 20 && model.IsBusy, "Download motion receives actual reported progress in " + variant);
                    var download = model.DownloadCommand.ExecutionTask;
                    await Until(() => buttonLight.Opacity > 0, "An accepted click shines even after the busy button is disabled in " + variant);
                    action.BringIntoView();
                    await Task.Delay(70);
                    Check(model.DownloadActionState == DownloadActionState.Downloading && !button.IsEnabled && arrow.Visibility == Visibility.Visible && check.Visibility == Visibility.Hidden,
                        "Only an active download moves the arrow and prevents duplicate activation in " + variant);
                    Check(scale.ScaleX is > 0 and <= .2 && model.Progress == 20,
                        "The animated fill approaches the real target without changing semantic progress in " + variant);
                    Shot("downloading-" + variant);
                    await Task.Delay(240);
                    Check(Math.Abs(scale.ScaleX - .2) < .001 && arrowOffset.Y == 0 && provider.DownloadCalls == calls + 1,
                        "Progress settles at twenty percent and the arrow plays once in " + variant);
                    Check(Math.Abs(Bounds(button).Width - before.Width) < 1 && Math.Abs(Bounds(button).Height - before.Height) < 1,
                        "Download content keeps the fixed action dimensions in " + variant);
                    var cancel = (Button)WpfTree.Find(action, "ActionCancel");
                    var cancelBounds = cancel.TransformToAncestor(viewport).TransformBounds(new Rect(cancel.RenderSize));
                    Check(cancel.IsEnabled && cancel.IsVisible && cancelBounds.Top >= 0 && cancelBounds.Bottom <= viewport.ActualHeight + 1,
                        "Cancel stays visible beside the active action in the compact viewport in " + variant);
                    bool sawCompletionAnimation = false;
                    System.ComponentModel.PropertyChangedEventHandler observeCompletion = (_, args) =>
                    {
                        if (args.PropertyName == nameof(MainViewModel.DownloadActionState) &&
                            model.DownloadActionState == DownloadActionState.Complete)
                            sawCompletionAnimation |= check.Visibility == Visibility.Visible && check.HasAnimatedProperties;
                    };
                    model.PropertyChanged += observeCompletion;
                    try
                    {
                        finish.Set();
                        if (download != null) await download;
                    }
                    finally { model.PropertyChanged -= observeCompletion; }
                    await Task.Delay(70);
                    // Observe attachment at the transition; runner delays can exceed a short animation's duration.
                    Check(model.DownloadActionState == DownloadActionState.Complete && model.DownloadActionLabel == model.RevealLabel &&
                        check.Visibility == Visibility.Visible && sawCompletionAnimation && button.IsEnabled && model.Progress == 100,
                        "Only full success draws the check and exposes the localized folder action in " + variant);
                    Shot("complete-draw-" + variant);
                    await Until(() => check.StrokeDashOffset == 0 && check.Opacity == 1 && check.RenderTransform is ScaleTransform { ScaleX: 1, ScaleY: 1 }, "The success check settles after its stroke draw in " + variant);
                    Check(check.StrokeDashOffset == 0 && scale.ScaleX == 1 &&
                        AutomationProperties.GetName(button) == model.RevealLabel && button.Command == model.DownloadActionCommand,
                        "Completion settles into a named native folder action in " + variant);
                    Shot("complete-" + variant);
                    Check(new Rect(window.Left, window.Top, window.ActualWidth, window.ActualHeight) == frame,
                        "Motion, metadata and completion preserve the outer frame in " + variant);
                }

            await Input();
            provider.DownloadHandler = (_, token, progress) =>
            {
                progress.Report(new DownloadProgress { Stage = "Downloading", Percent = 35 });
                token.WaitHandle.WaitOne();
                token.ThrowIfCancellationRequested();
                throw new InvalidOperationException("Unreachable fixture");
            };
            for (int attempt = 0; attempt < 2; attempt++)
            {
                var task = model.DownloadCommand.ExecuteAsync(null);
                await Until(() => model.IsBusy && model.Progress == 35, "Repeat/cancel fixture reaches its actual progress");
                model.CancelCommand.Execute(null);
                await task;
                await Task.Delay(50);
                Check(model.DownloadActionState == DownloadActionState.Ready && scale.ScaleX == 0 && arrowOffset.Y == 0 &&
                    check.Visibility == Visibility.Hidden && button.IsEnabled, "Cancellation resets motion and permits immediate retry without stale success");
            }
            provider.DownloadHandler = (_, _, _) => throw new IOException("Fixture download failed");
            await model.DownloadCommand.ExecuteAsync(null);
            Check(model.DownloadActionState == DownloadActionState.Ready && scale.ScaleX == 0 && check.Visibility == Visibility.Hidden,
                "A failed download cannot display the success check or fill");

            var feedback = (QueueFeedbackView)WpfTree.Find(window, "QueueFeedback");
            var added = (System.Windows.Shapes.Path)WpfTree.Find(feedback, "AddedMark");
            int revision = model.QueueAdditionVersion;
            model.AddQueueCommand.Execute(null);
            await Task.Delay(140);
            Check(model.QueueAdditionVersion == revision + 1 && added.Opacity > .9, "A successful queue addition receives one brief check confirmation");
            Shot("queue-confirmation");
            model.DismissNoticeCommand.Execute(null);
            await Task.Delay(800);
            Check(added.Opacity == 0, "The queue confirmation returns to the normal add icon");

            model.Url = "";
            await Task.Delay(100);
            viewport.ScrollToTop();
            window.UpdateLayout();
            var paste = (Button)WpfTree.Find(window, "PasteButton");
            paste.ApplyTemplate();
            var hover = (Border)paste.Template.FindName("HoverLayer", paste);
            // Exercise the real WPF hover handlers without touching the system cursor.
            paste.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, Environment.TickCount) { RoutedEvent = Mouse.MouseLeaveEvent });
            await Task.Delay(160);
            var pasteBounds = Bounds(paste);
            paste.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, Environment.TickCount) { RoutedEvent = Mouse.MouseEnterEvent });
            await Task.Delay(45);
            Check(hover.Opacity is > 0 and < 1, "Simulated WPF hover interpolates the button wash without moving the cursor");
            Shot("hover-enter");
            await Task.Delay(160);
            Check(hover.Opacity == 1 && Bounds(paste) == pasteBounds, "Hover settles without moving or resizing the button");
            paste.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, Environment.TickCount) { RoutedEvent = Mouse.MouseLeaveEvent });
            await Task.Delay(160);
            Check(hover.Opacity == 0, "Pointer exit clears the hover wash");

            model.ThemeCommand.Execute(null);
            await Task.Delay(55);
            Check(transition.IsTransitioning && !transition.IsHitTestVisible, "Theme changes reveal coherent palette regions without blocking input");
            Shot("theme-midpoint");
            model.ThemeCommand.Execute(null);
            await Task.Delay(320);
            Check(!transition.IsTransitioning, "Rapid theme changes replace and release the previous snapshot");
            model.ThemeCommand.Execute(null);
            window.Width += 10;
            await Task.Delay(60);
            Check(!transition.IsTransitioning, "Resizing interrupts the theme snapshot cleanly");
            window.Width -= 10;

            MotionPreferences.SetTestOverride(false);
            brand.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            model.ThemeCommand.Execute(null);
            Check(light.Opacity == 0 && !transition.IsTransitioning, "Reduced motion applies theme and branding immediately");
            await Input();
            provider.DownloadHandler = (_, token, progress) =>
            {
                progress.Report(new DownloadProgress { Stage = "Downloading", Percent = 20 });
                token.WaitHandle.WaitOne();
                token.ThrowIfCancellationRequested();
                throw new InvalidOperationException("Unreachable fixture");
            };
            var reduced = model.DownloadCommand.ExecuteAsync(null);
            await Until(() => model.IsBusy && model.Progress == 20, "Reduced-motion download receives semantic progress");
            Check(scale.ScaleX == .2 && arrowOffset.Y == 0, "Reduced motion presents the real progress immediately without spatial movement");
            model.CancelCommand.Execute(null);
            await reduced;
            Shot("reduced-motion");

            provider.DownloadHandler = (_, _, _) => new DownloadResult { FilePath = fixtureFile, Files = new() { fixtureFile }, Completed = 1, Total = 1 };
            await model.DownloadCommand.ExecuteAsync(null);
            Check(model.DownloadActionState == DownloadActionState.Complete && check.StrokeDashOffset == 0 && check.IsVisible && scale.ScaleX == 1,
                "Reduced motion preserves immediate success and folder action feedback");
            model.Url = "";

            MotionPreferences.SetTestOverride(true);
            brand.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await Task.Delay(80);
            MotionPreferences.SetTestOverride(false);
            Check(light.Opacity == 0 && logoScale.ScaleX == 1 && logoScale.ScaleY == 1,
                "A preference change interrupts an in-flight logo animation");
            MotionPreferences.SetTestOverride(true);
            model.ThemeCommand.Execute(null);
            MotionPreferences.SetTestOverride(false);
            Check(!transition.IsTransitioning, "A preference change immediately releases an in-flight theme snapshot");
        }
        finally
        {
            provider.DownloadHandler = null;
            model.CancelCommand.Execute(null);
            MotionPreferences.SetTestOverride(null);
            model.Folder = oldFolder;
            model.Url = "";
            model.SelectedEncoding = oldEncoding;
            model.Audio = oldAudio;
            model.SelectedQuality = model.Qualities.FirstOrDefault(choice => choice.Key == oldQuality?.Key && choice.Bitrate == oldQuality?.Bitrate && choice.Height == oldQuality?.Height) ?? model.Qualities.FirstOrDefault();
            model.SelectLanguageCommand.Execute(oldArabic ? "ar" : "en");
            if (model.IsDark != oldDark) model.ThemeCommand.Execute(null);
            window.Width = oldWidth;
            window.Height = oldHeight;
        }
    }
}
