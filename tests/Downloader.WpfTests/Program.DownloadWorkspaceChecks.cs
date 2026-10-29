using System.Windows;
using System.Windows.Controls;
using YouTubeDownloader;

internal static partial class Program
{
    private static async Task CheckDownloadWorkspace(MainWindow window, MainViewModel model, string captures)
    {
        foreach (string language in new[] { "en", "ar" })
        foreach (bool dark in new[] { false, true })
        foreach (double width in new[] { 1280d, 760d })
        {
            model.Url = "";
            model.SelectLanguageCommand.Execute(language);
            if (model.IsDark != dark) model.ThemeCommand.Execute(null);
            window.Width = width; window.Height = 800;
            model.NewDownloadCommand.Execute(null);
            await Task.Delay(100); window.UpdateLayout();
            var form = window.DownloadForm;
            Check(((FrameworkElement)form.LinkInput.Parent).FlowDirection == FlowDirection.LeftToRight,
                "URL text and its link icon keep a predictable left-to-right field inside either app language");
            Check(window.Viewport.ScrollableWidth < 1 && form.LinkInput.IsVisible && !form.QualityInput.IsEnabled,
                "Empty download workspace fits and waits for actual available qualities");
            Check(Math.Abs(form.ActualWidth - window.Viewport.ViewportWidth) <= 1 &&
                Math.Abs(form.FormContentPanel.ActualWidth + 48 - window.Viewport.ViewportWidth) <= 1 &&
                Math.Abs(window.EditorSurface.ActualWidth - Math.Min(840, window.EditorOverlay.ActualWidth)) <= 1,
                $"The download form fills a capped readable column with consistent padding at every viewport width (surface={window.EditorSurface.ActualWidth}, workspace={window.EditorOverlay.ActualWidth}, form={form.ActualWidth}, viewport={window.Viewport.ViewportWidth}, content={form.FormContentPanel.ActualWidth})");
            var linkBounds = form.LinkWell.TransformToAncestor(window.EditorSurface).TransformBounds(new Rect(form.LinkWell.RenderSize));
            var headingBounds = window.EditorHeader.TransformToAncestor(window.EditorSurface).TransformBounds(new Rect(window.EditorHeader.RenderSize));
            Check(Math.Abs(linkBounds.Left - headingBounds.Left) <= 1 && Math.Abs(linkBounds.Right - headingBounds.Right) <= 1 &&
                !Descendants<Button>(window.EditorHeader).Any(),
                "The tab heading aligns with the full-width form and has no modal close button");
            Check(form.DestinationPicker.IsVisible && Descendants<Button>(form).Count(button => button.Command == model.BrowseCommand) == 1,
                "The redesigned workspace has one unambiguous folder picker");
            var primary = form.DownloadAction.DownloadButton;
            var secondary = form.AddToQueueButton;
            var primaryBounds = primary.TransformToAncestor(form).TransformBounds(new Rect(primary.RenderSize));
            var secondaryBounds = secondary.TransformToAncestor(form).TransformBounds(new Rect(secondary.RenderSize));
            Check(!primaryBounds.IntersectsWith(secondaryBounds), "Primary Download and secondary queue actions never overlap");
            var actionBounds = form.FormActions.TransformToAncestor(form).TransformBounds(new Rect(form.FormActions.RenderSize));
            Check(Math.Abs(primaryBounds.Left + primaryBounds.Width / 2 - actionBounds.Left - actionBounds.Width / 2) <= 1, "Download remains centered independently of the queue action");
            Check(language == "ar" ? secondaryBounds.Right > primaryBounds.Right : secondaryBounds.Left < primaryBounds.Left, "Add to queue stays at the leading edge in either language");
            Check(form.FormContentPanel.ActualWidth > 0 && primaryBounds.Bottom <= window.Viewport.ViewportHeight + 1,
                "The main download action is reachable without scrolling at normal height");
            CheckButtonHitAreas(window);
            Capture(window, System.IO.Path.Combine(captures, $"download-empty-{language}-{dark}-{width}.png"));
            model.Url = "https://youtu.be/jNQXAC9IVRw";
            await WaitFor(() => model.CanDownload, "Real fixture inspection enables downloading");
            await Task.Delay(80); window.UpdateLayout();
            Check(form.QualityInput.IsEnabled && window.MediaPreview.IsVisible, "Inspected media retains quality choices and its responsive preview");
            Check(Math.Abs(form.ActualWidth - window.Viewport.ViewportWidth) <= 1,
                "The form continues to fill its available width when the media inspector appears");
            CheckButtonHitAreas(window);
            Capture(window, System.IO.Path.Combine(captures, $"download-ready-{language}-{dark}-{width}.png"));
            model.Audio = true;
            Check(form.AudioChoice.IsChecked == true && model.SelectedEncoding != null && model.Qualities.Count > 0,
                "Audio mode retains its encoding and bitrate controls");
            model.Audio = false;
        }
    }
}
