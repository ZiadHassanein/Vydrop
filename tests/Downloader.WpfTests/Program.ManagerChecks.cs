using System.IO;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using YouTubeDownloader;

internal static partial class Program
{
    private static async Task CheckManager(MainWindow window, MainViewModel model, FakeProvider provider, string root, string captures)
    {
        Check(!window.EditorOverlay.IsVisible && window.ManagerContent.IsEnabled && model.LibraryEmpty, "The manager opens as the primary screen with an honest empty library");
        Check(Descendants<System.Windows.Controls.Border>(window.ManagerContent).Where(b => b.Tag is int n && n == 0).All(b => b.Visibility == System.Windows.Visibility.Collapsed), "Zero counts are hidden across sidebar and status filters");
        Capture(window, Path.Combine(captures, "manager-empty-no-counts.png"));
        model.NewDownloadCommand.Execute(null); await Task.Delay(100);
        Check(window.EditorOverlay.IsVisible && window.DownloadForm.LinkInput.IsVisible && window.ManagerContent.IsEnabled && !window.ManagerContent.LibraryContent.IsVisible, "Add download occupies the center panel and keeps navigation enabled");
        Capture(window, Path.Combine(captures, "manager-add-empty.png"));
        var escape = new System.Windows.Input.KeyEventArgs(System.Windows.Input.Keyboard.PrimaryDevice, System.Windows.PresentationSource.FromVisual(window), 0, System.Windows.Input.Key.Escape) { RoutedEvent = System.Windows.Input.Keyboard.PreviewKeyDownEvent };
        window.DownloadForm.LinkInput.RaiseEvent(escape); await Task.Delay(80);
        Check(!model.AddDownloadVisible && escape.Handled, "Escape leaves the inline editor and restores the library");
        Check(!window.EditorOverlay.IsVisible && window.ManagerContent.IsEnabled, "Closing the editor restores the library");
        model.SettingsCommand.Execute(null); await Task.Delay(80);
        Check(window.SettingsPane.IsVisible && window.EditorOverlay.IsVisible, "Settings remain accessible from the manager");
        model.SettingsCommand.Execute(null);
        model.Folder = root;
        string firstFile = Path.Combine(root, "coast.mp4"), deletedFile = Path.Combine(root, "deleted.mp4");
        File.WriteAllBytes(firstFile, new byte[1048576]); File.WriteAllBytes(deletedFile, new byte[128]);
        DownloadOptions Options(string id, bool audio = false) => new() { Url = "https://www.youtube.com/watch?v=" + id, Folder = root, Audio = audio, Height = audio ? 0 : 720, AudioBitrate = 0, ThumbnailUrl = "https://example.test/thumbnail.png", Duration = 504 };
        var complete = model.Queue.Add(Options("jNQXAC9IVRw"), "Coastal paths · Local test media");
        await model.Queue.RunJobAsync(complete.Id, (_, _, _) => Task.FromResult(new DownloadResult { FilePath = firstFile }));
        var deleted = model.Queue.Add(Options("jNQXAC9IVRx"), "رحلة هادئة على ساحل البحر — ملف اختبار");
        await model.Queue.RunJobAsync(deleted.Id, (_, _, _) => Task.FromResult(new DownloadResult { FilePath = deletedFile })); File.Delete(deletedFile);
        var audio = model.Queue.Add(Options("jNQXAC9IVRy", true), "Evening notes · Audio test media");
        var waiting = model.Queue.Add(Options("jNQXAC9IVRz"), "A new perspective · Waiting to download");
        string audioFile = Path.Combine(root, "completed-audio.mp3"); File.WriteAllBytes(audioFile, new byte[2048]);
        var completedAudio = model.Queue.Add(Options("jNQXAC9IVRa", true), "Collected sounds · Completed audio");
        await model.Queue.RunJobAsync(completedAudio.Id, (_, _, _) => Task.FromResult(new DownloadResult { FilePath = audioFile }));
        await Task.Delay(120);
        Check(model.LibraryCount == 3 && model.WaitingCount == 2 && model.AudioCount == 1 && model.VideoCount == 2, "Library counts completed media while Queue counts unfinished jobs");
        model.LibrarySearch = "coastal";
        Check(model.LibraryItems.Cast<QueueItemView>().Single().Id == complete.Id, "Search finds a saved download by title");
        model.LibrarySearch = "no matching fixture"; Check(model.LibraryEmpty && model.HasLibrarySearch, "A failed search shows an explicit empty result");
        model.LibrarySearch = ""; model.LibraryFilterCommand.Execute("audio"); Check(model.LibraryItems.Cast<QueueItemView>().Single().Id == completedAudio.Id, "Media filters identify completed audio without including queued audio");
        model.LibraryFilterCommand.Execute("all");
        model.SelectedLibraryItem = model.QueueItems.First(q => q.Id == deleted.Id);
        Check(model.SelectedLibraryItem.MissingFile && !model.LibraryActionCommand.CanExecute(deleted.Id) && model.SelectedLibraryItem.MediaStatus == "File moved or deleted", "Missing output files are marked explicitly and cannot be opened");
        model.SelectedLibraryItem = model.QueueItems.First(q => q.Id == complete.Id);
        Check(model.SelectedLibraryItem.OutputFile == firstFile && model.SelectedLibraryItem.TransferSummary.Contains("MB") && model.SelectedLibraryItem.Speed == "—", "Completed size comes from the file and inactive transfer speed is unknown: " + model.SelectedLibraryItem.TransferSummary + " speed=" + model.SelectedLibraryItem.Speed + " output=" + model.SelectedLibraryItem.OutputFile + " expected=" + firstFile);
        var reloaded = new DownloadQueue(new LocalQueueStorage(root));
        Check(reloaded.Entries.Count == 5 && reloaded.Entries[0].Options.ThumbnailUrl == complete.Options.ThumbnailUrl, "Library metadata extends the existing durable queue without replacing its schema");
        int presentationResets = 0;
        void CountReset(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs args) { if (args.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset) presentationResets++; }
        model.LibraryItems.CollectionChanged += CountReset;
        foreach (string language in new[] { "en", "ar" }) foreach (bool dark in new[] { false, true })
        {
            model.SelectLanguageCommand.Execute(language); if (model.IsDark != dark) model.ThemeCommand.Execute(null);
            window.Width = 1280; await Task.Delay(300); window.UpdateLayout();
            bool wide = window.ManagerContent.ActualWidth >= 1080;
            for (int settle = 0; settle < 50 && (wide ? !window.ManagerContent.DetailsBorder.IsVisible : window.ManagerContent.DetailsColumn.Width.Value != 0); settle++) { await Task.Delay(20); window.UpdateLayout(); }
            Check(wide ? window.ManagerContent.DetailsBorder.IsVisible && window.ManagerContent.DetailsColumn.Width.Value > 0 : window.ManagerContent.DetailsColumn.Width.Value == 0 && window.ManagerContent.CompactDetailsButton.IsVisible,
                $"The inspector follows the actual viewport: {language}/{dark}, width={window.ManagerContent.ActualWidth}, window={window.ActualWidth}, column={window.ManagerContent.DetailsColumn.Width.Value}, visible={window.ManagerContent.DetailsBorder.IsVisible}, parent={window.ManagerContent.DetailsBorder.Parent}, add={model.AddDownloadVisible}, settings={model.SettingsVisible}, history={model.HistoryVisible}");
            Check(window.ManagerContent.AddDownloadButton.ActualWidth < 215 && window.ManagerContent.SelectedFolderPath.ActualHeight < 30,
                "The Add action stays content-sized and long save paths stay on one line: " + language + "/" + dark);
            Capture(window, Path.Combine(captures, "manager-" + language + (dark ? "-dark" : "-light") + ".png"));
        }
        model.LibraryItems.CollectionChanged -= CountReset;
        Check(presentationResets == 0, "Changing language, theme and window size preserves the existing virtualized rows");
        model.SelectLanguageCommand.Execute("en");
        window.Width = 800; await Task.Delay(100); window.UpdateLayout();
        Check(window.ManagerContent.DetailsColumn.Width.Value == 0 && window.ManagerContent.CompactDetailsButton.IsVisible, "Compact windows keep the library usable and offer an explicit Details button");
        var detailsPeer = new ButtonAutomationPeer(window.ManagerContent.CompactDetailsButton);
        ((IInvokeProvider)detailsPeer.GetPattern(PatternInterface.Invoke)).Invoke(); await Task.Delay(80);
        Check(window.ManagerContent.DetailOverlay.IsVisible && window.ManagerContent.DetailsBorder.IsVisible, "The compact details drawer exposes the same selected download");
        Capture(window, Path.Combine(captures, "manager-compact-details.png"));
        var closePeer = new ButtonAutomationPeer(window.ManagerContent.DetailsCloseButton);
        ((IInvokeProvider)closePeer.GetPattern(PatternInterface.Invoke)).Invoke();
        window.Width = 1280; await Task.Delay(80);
        var receipt = model.ReceiveBrowserRequest(new BrowserRequest(2, "open-link", "https://youtu.be/jNQXAC9IVRw", "manager_browser", Output: new("video", "webm", 720)));
        await Task.Delay(800);
        Check(receipt.Accepted && model.AddDownloadVisible && model.SelectedVideoFormat.Container == VideoContainer.WebM && model.SelectedQuality.Height == 720, "A browser output choice opens the real editor with the requested supported format and height");
        Capture(window, Path.Combine(captures, "manager-add-download.png"));
        model.SelectLanguageCommand.Execute("ar"); await Task.Delay(80);
        Capture(window, Path.Combine(captures, "manager-add-ar-wide.png"));
        window.Width = 800; await Task.Delay(80); window.UpdateLayout();
        Check(window.DownloadForm.LinkInput.IsVisible && window.MediaPreview.IsVisible && window.ManagerContent.DetailsColumn.Width.Value == 0, "The compact Arabic editor keeps the preview and form inline");
        Capture(window, Path.Combine(captures, "manager-add-ar-compact.png"));
        model.SelectLanguageCommand.Execute("en"); window.Width = 1280;
        model.CloseAddDownloadCommand.Execute(null); model.Url = "";
        provider.DownloadHandler = (_, token, progress) =>
        {
            progress.Report(new DownloadProgress { Stage = "Downloading", Percent = 42, DownloadedBytes = 4200000, TotalBytes = 10000000, BytesPerSecond = 600000, RemainingSeconds = 10 });
            token.WaitHandle.WaitOne(); token.ThrowIfCancellationRequested(); return new DownloadResult();
        };
        model.LibraryFilterCommand.Execute("queue");
        var running = model.LibraryActionCommand.ExecuteAsync(waiting.Id);
        for (int wait = 0; wait < 100 && model.QueueItems.First(q => q.Id == waiting.Id).Speed == "—"; wait++) await Task.Delay(25);
        model.SelectedLibraryItem = model.QueueItems.First(q => q.Id == waiting.Id);
        Check(model.SelectedLibraryItem.IsActive && model.SelectedLibraryItem.Speed != "—" && model.SelectedLibraryItem.TransferSummary.Contains('/'), "Starting through the manager displays engine progress and measured transfer data");
        Check(Equals(model.SelectedLibraryItem.StateBrush, window.FindResource("Accent")),
            "Active transfers use the brand accent alongside their download icon and label");
        Check(model.SelectedLibraryItem.RowStatus.Contains("42%", StringComparison.Ordinal) && model.SelectedLibraryItem.RowDescription.Contains("MB", StringComparison.Ordinal), "The active row exposes measured percentage and known size beside its compact controls");
        bool compactTransfer = window.ManagerContent.ActualWidth < 1080;
        if (compactTransfer) { ((IInvokeProvider)new ButtonAutomationPeer(window.ManagerContent.CompactDetailsButton).GetPattern(PatternInterface.Invoke)).Invoke(); await Task.Delay(60); }
        Check(model.LibraryActionCommand.CanExecute(waiting.Id) && window.ManagerContent.ManagerCancelButton.IsVisible && window.ManagerContent.ManagerCancelButton.IsEnabled, "A manager-started transfer retains enabled Pause and Cancel actions while the command is executing");
        if (compactTransfer) ((IInvokeProvider)new ButtonAutomationPeer(window.ManagerContent.DetailsCloseButton).GetPattern(PatternInterface.Invoke)).Invoke();
        var more = Descendants<System.Windows.Controls.Button>(window.ManagerContent.DownloadsList).Single(button => System.Windows.Automation.AutomationProperties.GetName(button) == model.MoreActionsLabel && button.DataContext is QueueItemView row && row.Id == waiting.Id);
        ((IInvokeProvider)new ButtonAutomationPeer(more).GetPattern(PatternInterface.Invoke)).Invoke(); await Task.Delay(60);
        Check(more.ContextMenu.IsOpen && more.ContextMenu.Items.OfType<System.Windows.Controls.MenuItem>().All(item => Equals(item.CommandParameter, waiting.Id)), "The accessible More menu targets exactly the selected row");
        CaptureVisual(more.ContextMenu, Path.Combine(captures, "manager-active-menu.png"));
        more.ContextMenu.IsOpen = false;
        var rowProgress = Descendants<System.Windows.Controls.ProgressBar>(window.ManagerContent.DownloadsList).Single(bar => bar.DataContext is QueueItemView item && item.Id == waiting.Id);
        var rowArt = Descendants<System.Windows.Controls.Image>(window.ManagerContent.DownloadsList).Single(image => image.DataContext is QueueItemView item && item.Id == waiting.Id);
        double progressBottom = rowProgress.TransformToAncestor(window).Transform(new System.Windows.Point(0, rowProgress.ActualHeight)).Y;
        double artBottom = rowArt.TransformToAncestor(window).Transform(new System.Windows.Point(0, rowArt.ActualHeight)).Y;
        Check(Math.Abs(progressBottom - artBottom) <= 1, "The row progress ends at the same height as its thumbnail");
        Capture(window, Path.Combine(captures, "manager-active.png"));
        model.LibraryActionCommand.Execute(waiting.Id); await running; await Task.Delay(80);
        Check(model.Queue.Entries.First(e => e.Id == waiting.Id).State == QueueState.Paused && model.LibraryActionCommand.CanExecute(waiting.Id), "The selected transfer can pause and exposes Resume without losing its job");
        Check(Equals(model.SelectedLibraryItem.StateBrush, window.FindResource("Warning")) &&
            Equals(rowProgress.Foreground, window.FindResource("Warning")),
            "Pausing changes the actual row status and progress to amber while retaining the pause label and icon");
        Capture(window, Path.Combine(captures, "manager-paused.png"));
        provider.DownloadHandler = (options, _, _) => new DownloadResult { FilePath = firstFile };
        await model.LibraryActionCommand.ExecuteAsync(waiting.Id);
        Check(model.Queue.Entries.First(e => e.Id == waiting.Id).State == QueueState.Completed && provider.LastRequest.JobId == waiting.Id, "Resuming from the manager uses the existing durable job and download service");
        Check(model.RecentDownloadCount > 0 && window.ManagerContent.HistoryButton.IsVisible && System.Windows.Automation.AutomationProperties.GetName(window.ManagerContent.HistoryButton) == model.HistoryAccessibleName,
            "The visible History navigation announces new completions using the preserved unseen-download count");
        model.HistoryCommand.Execute(null); await Task.Delay(60);
        Check(model.RecentDownloadCount == 0 && window.ManagerContent.HistoryButton.IsVisible && System.Windows.Automation.AutomationProperties.GetName(window.ManagerContent.HistoryButton) == model.HistoryLabel,
            "Opening History acknowledges new completions without hiding its navigation action");
        model.LibraryFilterCommand.Execute("queue");
        var sentinel = model.Queue.Add(Options("jNQXAC9IVR1"), "Keep waiting · Cancel regression");
        provider.DownloadHandler = (_, token, _) => { token.WaitHandle.WaitOne(); token.ThrowIfCancellationRequested(); return new DownloadResult(); };
        var cancelled = model.LibraryActionCommand.ExecuteAsync(audio.Id);
        for (int wait = 0; wait < 100 && !model.QueueItems.First(q => q.Id == audio.Id).IsActive; wait++) await Task.Delay(25);
        model.LibraryCancelCommand.Execute(audio.Id); await cancelled;
        Check(model.Queue.Entries.First(e => e.Id == audio.Id).State == QueueState.Cancelled && model.Queue.Entries.First(e => e.Id == sentinel.Id).State == QueueState.Pending, "Cancelling the selected active transfer preserves other pending jobs");
        ThemeResources.ContrastOverride = new(System.Windows.Media.Brushes.Black, System.Windows.Media.Brushes.White, System.Windows.Media.Brushes.Yellow, System.Windows.Media.Brushes.Black);
        ThemeResources.Apply(true); model.RefreshThemeBindings(); model.LibraryFilterCommand.Execute("all"); model.SelectedLibraryItem = model.QueueItems.First(q => q.Id == complete.Id); window.ManagerContent.SearchInput.Focus(); await Task.Delay(60);
        var selectedContainer = (System.Windows.Controls.ListBoxItem)window.ManagerContent.DownloadsList.ItemContainerGenerator.ContainerFromItem(model.SelectedLibraryItem);
        Check(selectedContainer.IsSelected && selectedContainer.BorderThickness.Left == 3 && Equals(selectedContainer.BorderBrush, window.FindResource("Accent")), "High-contrast selection remains visibly outlined after keyboard focus moves to search");
        Capture(window, Path.Combine(captures, "manager-high-contrast.png"));
        Check(Equals(window.FindResource("Navigation"), System.Windows.Media.Brushes.Black), "High contrast uses system-equivalent navigation colors");
        foreach (string role in new[] { "NavigationIcon", "MediaAudio", "MediaVideo", "Warning" })
            Check(Equals(window.FindResource(role), System.Windows.Media.Brushes.White),
                "Semantic icon and status colors respect high-contrast system foreground: " + role);
        ThemeResources.ContrastOverride = null; ThemeResources.Apply(model.IsDark);
    }
}
