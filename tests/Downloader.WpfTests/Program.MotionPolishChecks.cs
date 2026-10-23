using System.IO;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using YouTubeDownloader;

internal static partial class Program
{
    private static async Task CheckMotionPolish(MainWindow window, MainViewModel model, string captures)
    {
        MotionPreferences.SetTestOverride(true);
        try
        {
            window.Width = 1280; model.LibraryFilterCommand.Execute("all"); await Task.Delay(250);
            var manager = window.ManagerContent;
            var button = manager.AddDownloadButton;
            var visual = Descendants<FrameworkElement>(button).First(InteractionMotion.GetEnabled);
            var scale = ((TransformGroup)visual.RenderTransform).Children.OfType<ScaleTransform>().Single();
            Rect Bounds() => button.TransformToAncestor(window).TransformBounds(new Rect(button.RenderSize));
            var bounds = Bounds();
            button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            await Task.Delay(35);
            Check(scale.HasAnimatedProperties && scale.ScaleX < 1 && Bounds() == bounds, "Click feedback compresses the visual while its full hit area and layout stay fixed");
            CheckButtonHitAreas(window);
            await Task.Delay(180);
            Check(!scale.HasAnimatedProperties && scale.ScaleX == 1 && scale.ScaleY == 1, "The press response releases its clocks and returns to the exact resting size");
            var hover = Descendants<FrameworkElement>(button).First(HoverMotion.GetEnabled);
            button.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, Environment.TickCount) { RoutedEvent = Mouse.MouseEnterEvent });
            await Task.Delay(40);
            Check(hover.Opacity > 0 && hover.Opacity < HoverMotion.GetStrength(hover), "Hover highlights ease in rather than appearing in one frame");
            button.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, Environment.TickCount) { RoutedEvent = Mouse.MouseLeaveEvent });
            await Task.Delay(150);
            Check(hover.Opacity == 0, "Pointer exit clears the highlight");

            model.SettingsCommand.Execute(null);
            await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            Check(manager.EditorHost.RenderTransform is TransformGroup && manager.EditorHost.HasAnimatedProperties && manager.NavigationSurface.IsEnabled,
                "Tab navigation animates only the new workspace and keeps navigation enabled");
            Check(manager.WorkspaceViewport.ClipToBounds, "Workspace motion is clipped before it reaches the sidebar or inspector");
            Capture(window, Path.Combine(captures, "workspace-handoff-frame.png"), transitionFrame: true);
            model.HistoryCommand.Execute(null); model.NewDownloadCommand.Execute(null); model.LibraryFilterCommand.Execute("queue");
            await Task.Delay(250);
            Check(manager.LibraryContent.IsVisible && !manager.EditorHost.IsVisible && !manager.HistoryContent.IsVisible && !manager.WorkspaceHost.IsVisible && window.OwnedWindows.Count == 0,
                "Rapid navigation settles on the final tab without leaving a blocking or stale surface");
            Check(!manager.LibraryContent.HasAnimatedProperties && manager.LibraryContent.Opacity == 1 && manager.LibraryContent.RenderTransform.Value.IsIdentity,
                "The completed tab transition releases its animations and transforms");
            model.SettingsCommand.Execute(null);
            await window.Dispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            MotionPreferences.SetTestOverride(false);
            Check(!manager.EditorHost.HasAnimatedProperties && manager.EditorHost.Opacity == 1 && manager.EditorHost.RenderTransform.Value.IsIdentity,
                "Changing Reduce Motion during a transition immediately settles the workspace");
            model.LibraryFilterCommand.Execute("all"); await Task.Delay(50);
            button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            Check(!scale.HasAnimatedProperties && scale.ScaleX == 1 && manager.LibraryContent.RenderTransform.Value.IsIdentity,
                "Reduced motion keeps click feedback and navigation immediate without spatial animation");

            foreach (string locale in new[] { "en", "ar" }) foreach (bool dark in new[] { false, true }) foreach (double width in new[] { 1280d, 760d })
            {
                model.SelectLanguageCommand.Execute(locale); if (model.IsDark != dark) model.ThemeCommand.Execute(null);
                window.Width = width; model.NewDownloadCommand.Execute(null); await Task.Delay(260);
                foreach (string control in new[] { "DownloadButton", "VideoChoice", "AudioChoice" })
                    Check(Descendants<FrameworkElement>((DependencyObject)WpfTree.Find(window, control)).Any(visual => InteractionMotion.GetEnabled(visual) && visual.RenderTransform is TransformGroup), "Custom form controls participate in the shared press behavior: " + control);
                Check(window.HeaderContent.LanguageButton.ContextMenu == null && window.DownloadForm.LinkInput.IsVisible, "The language action and download form remain accessible in " + locale + "/" + dark + "/" + width);
                Capture(window, Path.Combine(captures, $"polish-add-{locale}-{dark}-{width}.png"));
                model.SettingsCommand.Execute(null); await Task.Delay(80);
                Capture(window, Path.Combine(captures, $"polish-settings-{locale}-{dark}-{width}.png"));
            }
        }
        finally { MotionPreferences.SetTestOverride(null); model.SelectLanguageCommand.Execute("en"); model.LibraryFilterCommand.Execute("all"); }
    }
}
