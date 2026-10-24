using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using YouTubeDownloader;

internal static partial class Program
{
    private static async Task CheckCheckingFeedback(MainWindow window, MainViewModel model, FakeProvider provider, string captures)
    {
        var action = (DownloadActionView)WpfTree.Find(window, "DownloadAction");
        var button = (Button)WpfTree.Find(action, "DownloadButton");
        var video = (FrameworkElement)WpfTree.Find(action, "CheckingVideo");
        var scan = (TranslateTransform)action.FindName("CheckScanOffset");
        var cancel = (Button)WpfTree.Find(window.DownloadForm, "FormCancel");
        var progress = (ScaleTransform)button.Template.FindName("ProgressScale", button);
        var inline = (FrameworkElement)WpfTree.Find(window, "InlineActivity");
        var completed = (FrameworkElement)WpfTree.Find(window, "CompleteCheck");
        Rect Bounds(FrameworkElement element) => element.TransformToAncestor(window).TransformBounds(new Rect(element.RenderSize));
        async Task Until(Func<bool> predicate, string message)
        {
            for (int attempt = 0; attempt < 150 && !predicate(); attempt++) await Task.Delay(20);
            Check(predicate(), message);
        }
        try
        {
            MotionPreferences.SetTestOverride(true);
            foreach (string language in new[] { "en", "ar" })
                foreach (bool dark in new[] { true, false })
                {
                    model.Url = "";
                    model.SelectLanguageCommand.Execute(language);
                    if (model.IsDark != dark) model.ThemeCommand.Execute(null);
                    using var release = new ManualResetEventSlim();
                    provider.InspectionHook = token => release.Wait(token);
                    try
                    {
                        model.Url = "https://youtu.be/jNQXAC9IVRw";
                        await Until(() => model.IsCheckingMedia && video.IsVisible && scan.HasAnimatedProperties, "Inspection animates the video scanner inside the action");
                        button.BringIntoView();
                        window.UpdateLayout();
                        Rect before = Bounds(button);
                        Check(!inline.IsVisible && !completed.IsVisible && !button.IsEnabled && button.Opacity == 1,
                            "The action owns checking feedback without duplicate activity or premature success");
                        Check(scan.X is >= 3 and <= 17 && progress.ScaleX == 0 && model.Progress == 0, "Checking never invents download progress");
                        Check(video.FlowDirection == FlowDirection.LeftToRight && model.DownloadActionLabel == (language == "ar" ? "جارٍ فحص الفيديو" : "Checking video"),
                            "The checking action localizes its label without reversing the video scanner");
                        Check(cancel.IsVisible && cancel.IsEnabled, "Checking retains an available Cancel action");
                        Rect cancelBounds = Bounds(cancel);
                        Check(Math.Abs(cancelBounds.Top + cancelBounds.Height / 2 - before.Top - before.Height / 2) <= 1 && !cancelBounds.IntersectsWith(before), "Cancel sits beside the centered Download button without overlap");
                        await Task.Delay(300);
                        Check(Bounds(button) == before && Math.Abs(before.Width - action.Width) <= 1 && Math.Abs(before.Height - button.Height) <= 1,
                            "Checking motion preserves the configured inline action bounds");
                        Capture(window, Path.Combine(captures, "checking-" + language + (dark ? "-dark" : "-light") + ".png"));
                        model.SettingsCommand.Execute(null);
                        await Task.Delay(40);
                        Check(model.IsCheckingMedia && !video.IsVisible && !scan.HasAnimatedProperties, "Settings hides checking feedback and releases its clock without ending inspection");
                        model.SettingsCommand.Execute(null);
                        await Until(() => video.IsVisible && scan.HasAnimatedProperties, "Returning to the form resumes a still-active inspection");
                        MotionPreferences.SetTestOverride(false);
                        Check(video.IsVisible && !scan.HasAnimatedProperties && button.Opacity == 1, "Reduced motion keeps the checking action while stopping movement");
                        MotionPreferences.SetTestOverride(true);
                    }
                    finally { release.Set(); }
                    await Until(() => model.CanDownload, "The held media inspection completes");
                    Check(!video.IsVisible && !scan.HasAnimatedProperties && !completed.IsVisible && !cancel.IsVisible, "Ready metadata clears checking motion without implying a completed download");
                }
            using var cancellation = new ManualResetEventSlim();
            provider.InspectionHook = token => cancellation.Wait(token);
            model.Url = "";
            model.Url = "https://youtu.be/jNQXAC9IVRw";
            try
            {
                await Until(() => model.IsCheckingMedia, "Cancellation fixture reaches video inspection");
                cancel.Command.Execute(null);
                await Until(() => model.IsIdle, "Cancelling inspection releases the operation");
                await Until(() => !model.IsCheckingMedia, "Cancelling clears the checking action");
                Check(!video.IsVisible && !scan.HasAnimatedProperties && !completed.IsVisible, "Cancelled checking stops cleanly and never draws a success check");
            }
            finally { cancellation.Set(); }
        }
        finally
        {
            provider.InspectionHook = null;
            model.CancelCommand.Execute(null);
            model.SettingsVisible = false;
            model.Url = "";
            MotionPreferences.SetTestOverride(null);
        }
    }
}
