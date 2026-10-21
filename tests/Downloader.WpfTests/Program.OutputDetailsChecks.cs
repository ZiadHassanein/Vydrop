using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using YouTubeDownloader;

internal static partial class Program
{
    private static async Task CheckOutputDetails(MainWindow window, MainViewModel model, FakeProvider provider, string captures)
    {
        var old = (model.Folder, model.Audio, model.IsArabic, model.IsDark, window.Width, window.Height, provider.SourceAudio, provider.WebMSupported, provider.CaptionTracks, model.SelectedVideoFormat);
        string folder = Path.GetFullPath(Path.Combine(captures, "output-fixtures")); Directory.CreateDirectory(folder);
        string file = Path.Combine(folder, "Owned UI fixture.mkv"); File.WriteAllText(file, "Synthetic UI fixture; not playable media.");
        var video = (RadioButton)WpfTree.Find(window, "VideoChoice");
        var quality = (ComboBox)WpfTree.Find(window, "QualityInput");
        var inline = (FrameworkElement)WpfTree.Find(window, "InlineActivity");
        var context = (ScrollViewer)WpfTree.Find(window, "ContextViewport");
        var history = (FrameworkElement)WpfTree.Find(window, "HistoryButton");
        async Task Settle() { await Task.Delay(260); window.UpdateLayout(); }
        async Task Input() { model.Url = ""; model.Url = "https://youtu.be/jNQXAC9IVRw"; await Task.Delay(900); await Settle(); }
        async Task History(string variant)
        {
            model.HistoryCommand.Execute(null); await Settle();
            Check(model.HistoryVisible && window.ManagerContent.HistoryContent.IsVisible && window.OwnedWindows.Count == 0, "History opens in the main center workspace");
            Check(model.RecentDownloadCount == 0 && history.IsVisible && AutomationProperties.GetName(history) == model.HistoryLabel, "Opening history acknowledges its completion count on the visible navigation action");
            Check(model.History.Files.Any(row => row.FilePath == file && !row.IsAudio), "MKV history retains its actual media path");
            Capture(window, Path.Combine(captures, "output-history-" + variant + ".png"));
            model.NewDownloadCommand.Execute(null); await Settle();
        }
        try
        {
            model.SettingsVisible = false; model.Url = ""; model.Folder = folder; model.Audio = true;
            Check(model.Qualities.Count == 1 && model.Qualities[0].Bitrate == 0 && !quality.IsEnabled,
                "Before inspection audio offers only automatic conversion");
            provider.SourceAudio = new("aac", 128);
            provider.CaptionTracks = new() { new("en", "English"), new("ar", "العربية", true) };
            foreach (string locale in new[] { "en", "ar" })
                foreach (bool dark in new[] { true, false })
                {
                    model.SelectLanguageCommand.Execute(locale); if (model.IsDark != dark) model.ThemeCommand.Execute(null);
                    model.Url = ""; window.Width = 740; await Input();
                    model.Audio = false;
                    video.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent)); await Settle();
                    Check(video.ContextMenu.IsOpen && video.ContextMenu.Items.Count == 3,
                        "Video offers MP4, MKV and WebM through its native format picker");
                    var item = video.ContextMenu.Items.OfType<MenuItem>().Single(choice => ((VideoFormatChoice)choice.Tag).Container == VideoContainer.Mkv);
                    string variant = locale + (dark ? "-dark" : "-light");
                    CapturePopup((FrameworkElement)window.Content, (HwndSource)PresentationSource.FromVisual(video.ContextMenu), Path.Combine(captures, "output-video-menu-" + variant + ".png"));
                    ((IInvokeProvider)new MenuItemAutomationPeer(item).GetPattern(PatternInterface.Invoke)).Invoke(); await Settle();
                    Check(!video.ContextMenu.IsOpen && model.SelectedVideoFormat.Container == VideoContainer.Mkv && model.VideoLabel.Contains("MKV") && model.PreviewOutput.Contains("MKV"),
                        "Choosing MKV updates the real output request and summary and closes its picker");
                    model.Audio = true; model.SelectedEncoding = model.Encodings.First(choice => choice.Encoding == AudioEncoding.Mp3);
                    Check(model.Qualities.Any(choice => choice.Bitrate == 128) && model.Qualities.All(choice => choice.Bitrate <= 128) && model.QualityLabel != "Quality",
                        "A128kbps source offers honest supported conversion targets instead of320kbps quality");
                    model.SelectedQuality = model.Qualities.First(choice => choice.Bitrate == 128);
                    Check(model.AudioSourceHint.Contains("128") && ((TextBlock)WpfTree.Find(window, "SourceAudioSummary")).IsVisible,
                        "The details panel discloses the inspected source estimate and conversion limit");
                    await InspectOptionsModal(window, model, async dialog =>
                    {
                        Check(dialog.Title == model.CaptionsLabel && model.OptionsLabel == model.CaptionsLabel,
                            "The form action and owned modal are explicitly named Subtitles");
                        ((ComboBox)WpfTree.Find(dialog, "CaptionInput")).SelectedItem = model.CaptionChoices.First(choice => choice.Language == "en");
                        ((RadioButton)WpfTree.Find(dialog, "VttOption")).IsChecked = true; await Settle();
                        Capture(dialog, Path.Combine(captures, "output-subtitles-modal-" + variant + ".png"));
                    });
                    await Settle(); context.ScrollToEnd(); await Settle();
                    var cc = (FrameworkElement)WpfTree.Find(window, "SubtitleButtonIcon");
                    Check(cc.TranslatePoint(new Point(cc.ActualWidth, 0), window).X > cc.TranslatePoint(new Point(0, 0), window).X,
                        "The CC lettering keeps its physical left-to-right orientation in both languages");
                    var captions = (FrameworkElement)WpfTree.Find(window, "PreviewCaptionsSummary");
                    Check(model.HasPreviewCaptions && model.PreviewCaptions.Contains("English") && model.PreviewCaptions.Contains("VTT") && captions.IsVisible && model.SubtitlesEnabled,
                        "The selected subtitle language and file format appear in media details after the modal closes");
                    Capture(window, Path.Combine(captures, "output-subtitles-summary-" + variant + ".png"));
                    model.Audio = false;
                    int before = model.RecentDownloadCount;
                    provider.DownloadHandler = (_, _, _) => new() { FilePath = file, Files = new() { file }, Completed = 1, Total = 1 };
                    await model.DownloadCommand.ExecuteAsync(null); await Settle();
                    Check(provider.LastRequest.VideoContainer == VideoContainer.Mkv && provider.LastRequest.Captions is { Language: "en", Format: CaptionFormat.Vtt },
                        "The download receives the chosen container and selected subtitles");
                    Check(model.RecentDownloadCount == before + 1 && history.IsVisible && !inline.IsVisible && model.Url == "" && window.Width == 740,
                        "Success collapses media context and increments the recent count without a completion block");
                    Check(AutomationProperties.GetName(history) == model.HistoryAccessibleName,
                        "The completion count is included in the visible History action's accessible name");
                    window.Width = window.MinWidth; await Settle();
                    var brand = (FrameworkElement)WpfTree.Find(window, "BrandWordmark");
                    var actions = (FrameworkElement)WpfTree.Find(window, "HeaderActions");
                    Check(brand.TranslatePoint(new Point(brand.ActualWidth, 0), window).X < actions.TranslatePoint(new Point(), window).X,
                        "The notification badge fits beside the header utilities at minimum width");
                    Capture(window, Path.Combine(captures, "output-completed-" + variant + ".png"));
                    await History(variant);
                }
            provider.SourceAudio = new("aac", null); model.Audio = true; await Input();
            Check(model.Qualities.Count == 1 && !quality.IsEnabled, "Unknown source bitrate cannot offer fictional numeric quality choices");
            provider.SourceAudio = new("aac", 64); await Input();
            Check(model.Qualities.All(choice => choice.Bitrate <= 64), "A lower source estimate refreshes and bounds available targets");
            provider.WebMSupported = false; await Input();
            Check(!model.VideoFormats.Any(choice => choice.Container == VideoContainer.WebM), "Video hides WebM when inspection has no compatible streams");
            model.Audio = false; model.SelectedCaption = model.CaptionChoices.First(choice => choice.Language == "en");
            model.AddQueueCommand.Execute(null); model.DismissNoticeCommand.Execute(null);
            model.SelectedCaption = model.CaptionChoices.First(choice => choice.Language == "");
            using var finish = new ManualResetEventSlim();
            provider.DownloadHandler = (_, token, progress) => { progress.Report(new() { Stage = "Downloading", Percent = 30 }); finish.Wait(token); return new() { FilePath = file, Files = new() { file }, Completed = 1, Total = 1 }; };
            var queued = model.QueueStartCommand.ExecuteAsync(null);
            try
            {
                await Settle();
                Check(model.QueueRunning && model.HasPreviewCaptions && model.PreviewCaptions.Contains("English"),
                    "An active queue's subtitle summary comes from its snapshot, even if the form selection changed");
            }
            finally { finish.Set(); await queued; }
            Check((model.RecentDownloadCount > 0) && !inline.IsVisible, "A successful queue updates the recent count without a redundant footer block");
        }
        finally
        {
            provider.DownloadHandler = null; provider.SourceAudio = old.SourceAudio; provider.WebMSupported = old.WebMSupported; provider.CaptionTracks = old.CaptionTracks;
            foreach (var entry in model.Queue.Entries) model.RemoveQueueCommand.Execute(entry.Id);
            model.Url = ""; model.Folder = old.Folder; model.Audio = old.Audio; model.SelectedVideoFormat = old.SelectedVideoFormat;
            model.SelectLanguageCommand.Execute(old.IsArabic ? "ar" : "en"); if (model.IsDark != old.IsDark) model.ThemeCommand.Execute(null);
            window.Width = old.Width; window.Height = old.Height;
        }
    }
}
