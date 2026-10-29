using System.IO;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Threading;
using YouTubeDownloader;

internal static partial class Program
{
    private static void CheckQueueOutput(string captures)
    {
        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext());
        app.Resources.MergedDictionaries.Add(new ResourceDictionary
        { Source = new Uri("pack://application:,,,/Vydrop;component/Themes/Controls.xaml") });
        MotionPreferences.SetTestOverride(false);
        Directory.CreateDirectory(captures);
        string root = Path.Combine(captures, "state-" + Guid.NewGuid().ToString("N"));
        int inspected = 0;
        var provider = new FakeProvider { SourceAudio = new AudioSourceInfo("aac", 128), DownloadDelayMilliseconds = 1,
            InspectionHook = _ => Interlocked.Increment(ref inspected) };
        using var service = new DownloadService(provider, new EmptyStorage());
        var model = new MainViewModel(service, new SettingsRepository(root), new DesktopActions(new Window()), new FixtureThumbnailLoader());
        model.Folder = Path.Combine(root, "media"); Directory.CreateDirectory(model.Folder);
        model.OpenWorkspace(WorkspacePage.QueueComposer);
        var video = model.QueueLinkInputs[0];
        Check(!video.HasOutputOptions && video.Formats.Count == 0 && video.Qualities.Count == 0, "Empty links advertise no unverified output choices");
        model.PasteQueueLinks(video, "https://youtu.be/jNQXAC9IVRw\nhttps://youtu.be/aqz-KE-bpKQ\nhttps://youtu.be/BaW_jenozKc");
        Until(() => model.QueueLinkInputs.All(field => field.HasOutputOptions), "Pasted links load their available formats automatically");
        var audio = model.QueueLinkInputs[1]; var webm = model.QueueLinkInputs[2];
        Check(inspected == 3 && video.Qualities.Select(choice => choice.Height).Order().SequenceEqual(new[] { 480, 720 }),
            "Automatic discovery is serialized and exposes real resolutions without generic quality caps");
        video.SelectedFormat = "MKV"; video.SelectedQuality = video.Qualities.Single(choice => choice.Height == 720);
        audio.Audio = true;
        Check(audio.Qualities.Any(choice => choice.Bitrate == 128) && audio.Qualities.All(choice => choice.Bitrate <= 128),
            "Only source-bounded audio bitrates are suggested");
        audio.SelectedFormat = "FLAC";
        Check(!audio.CanChooseQuality && audio.SelectedQuality.Bitrate == 0, "Lossless output exposes no bitrate setting");
        audio.SelectedFormat = "MP3"; audio.SelectedQuality = audio.Qualities.Single(choice => choice.Bitrate == 128);
        webm.SelectedFormat = "WebM";
        foreach (var (language, dark, width) in new[] { ("en", false, 760d), ("en", true, 760d), ("en", true, 450d), ("ar", true, 760d), ("ar", false, 450d) })
        {
            model.SelectLanguageCommand.Execute(language); if (model.IsDark != dark) model.ThemeCommand.Execute(null);
            var composer = new QueueComposerView { DataContext = model, FontSize = 14, FontFamily = model.ContentFont };
            var frame = new Border { Child = composer }; frame.SetResourceReference(Border.BackgroundProperty, "Page");
            frame.Measure(new Size(width, 740)); frame.Arrange(new Rect(0, 0, width, 740)); frame.UpdateLayout();
            var combos = Descendants<ComboBox>(composer).ToArray();
            Check(combos.Length == 9 && combos.All(combo => !string.IsNullOrWhiteSpace(AutomationProperties.GetName(combo))) &&
                composer.QueueViewport.ScrollableWidth <= 1 && !Descendants<Button>(composer).Any(button => button.Name == "QueueCheckFormats"),
                "Named output controls fit without a Check formats button: " + language + "/" + width);
            var type = combos.Single(combo => combo.Name == "QueueMediaType" && ReferenceEquals(combo.DataContext, webm));
            type.SetCurrentValue(System.Windows.Controls.Primitives.Selector.SelectedValueProperty, true);
            Check(webm.Audio, "The real Type selector updates its own link");
            type.SetCurrentValue(System.Windows.Controls.Primitives.Selector.SelectedValueProperty, false);
            Complete(Dispatcher.CurrentDispatcher.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle).Task);
            frame.UpdateLayout();
            Check(!webm.Audio && video.SelectedFormat == "MKV" && video.SelectedQuality.Height == 720 && audio.Audio &&
                audio.SelectedFormat == "MP3" && audio.SelectedQuality.Bitrate == 128 && webm.SelectedFormat == "WebM" && webm.SelectedQuality.Height == 720 &&
                combos.Where(combo => combo.Name == "QueueOutputQuality").All(combo => combo.SelectedItem is QualityChoice choice &&
                    choice.Key == ((QueueLinkInputView)combo.DataContext).SelectedQualityKey), "Visible selections survive language, theme and media-type changes");
            CaptureVisual(frame, Path.Combine(captures, $"queue-output-{language}-{(dark ? "dark" : "light")}-{width}.png"));
            frame.Child = null;
        }
        Check(inspected == 3, "Changing type, format, theme or language reuses metadata without another source request");
        model.SelectLanguageCommand.Execute("en");
        model.LibraryFilterCommand.Execute("all"); model.OpenWorkspace(WorkspacePage.QueueComposer);
        Check(video.SelectedFormat == "MKV" && audio.SelectedQuality.Bitrate == 128, "Navigation preserves each draft's output choices");
        var stale = new DownloadOptions { StableOutputName = "Wrong editor video", SourcePage = "https://example.test/wrong", MediaId = "wrong" };
        video.ApplyOutput(stale);
        Check(stale.StableOutputName == video.InspectedTitle && stale.StableOutputName != "Wrong editor video" && stale.SourcePage == null && stale.MediaId == null && stale.Selection == null,
            "Batch jobs use their own identity and never another editor video's selection");
        model.AddLinkFieldCommand.Execute(null); var changed = model.QueueLinkInputs[^1];
        using var hold = new ManualResetEventSlim(); provider.InspectionHook = token => hold.Wait(token);
        changed.Url = "https://youtu.be/abcdefghijk";
        Until(() => changed.IsChecking, "Automatic inspection starts after URL editing settles");
        changed.Url = "https://youtu.be/lmnopqrstuv"; provider.WebMSupported = false; provider.VideoTitle = "Replacement fixture";
        provider.InspectionHook = null; hold.Set();
        Until(() => changed.HasOutputOptions, "A replaced URL loads its own fresh choices");
        Check(!changed.HasError && changed.InspectedTitle == "Replacement fixture" && !changed.Formats.Contains("WebM"),
            "Stale results are ignored and unsupported formats are not suggested");
        changed.Url = "invalid link"; Until(() => changed.HasError, "Invalid URLs receive automatic field-level feedback");
        Check(!changed.HasOutputOptions && changed.Formats.Count == 0 && changed.Qualities.Count == 0 && audio.SelectedQuality.Bitrate == 128,
            "A failed link exposes no unverified choices and cannot change another link");
        provider.InspectionHook = _ => throw new System.Net.Http.HttpRequestException("Connection reset");
        changed.Url = "https://youtu.be/abcdefghijk"; Until(() => changed.HasError, "Network failures leave an editable link");
        provider.InspectionHook = null; model.PasteQueueLinks(changed, changed.Url);
        Until(() => changed.HasOutputOptions, "Pasting the same link retries automatic discovery without an extra button");
        model.RemoveLinkFieldCommand.Execute(changed);
        model.AddLinksCommand.Execute(null);
        var jobs = model.Queue.Entries;
        Check(jobs.Count == 3 && !jobs[0].Options.Audio && jobs[0].Options.VideoContainer == VideoContainer.Mkv && jobs[0].Options.Height == 720 &&
            jobs[1].Options.Audio && jobs[1].Options.AudioEncoding == AudioEncoding.Mp3 && jobs[1].Options.AudioBitrate == 128 &&
            jobs[2].Options.VideoContainer == VideoContainer.WebM && jobs[2].Options.Height == 720, "Each mixed queue job captures its own verified profile");
        var restored = new DownloadQueue(new LocalQueueStorage(root));
        Check(restored.Entries.Select(job => OutputProfiles.Key(job.Options)).SequenceEqual(jobs.Select(job => OutputProfiles.Key(job.Options))) &&
            restored.Entries.All(job => job.Options.Folder == model.Folder), "Per-link choices and the saved folder survive queue reopening");
        provider.WebMSupported = true;
        var received = new List<DownloadOptions>();
        provider.DownloadHandler = (request, _, _) =>
        {
            received.Add(request.Snapshot()); string output = Path.Combine(model.Folder, Guid.NewGuid() + "." + OutputProfiles.Extension(request));
            File.WriteAllText(output, "local handoff fixture"); return new DownloadResult { Completed = 1, FilePath = output };
        };
        Complete(model.QueueStartCommand.ExecuteAsync(null));
        Check(received.Count == 3 && received[0].VideoContainer == VideoContainer.Mkv && received[0].QualityKey == "mkv-hd" &&
            received[1].Audio && received[1].AudioBitrate == 128 && received[2].VideoContainer == VideoContainer.WebM &&
            received[2].QualityKey == "webm-hd" && jobs.All(job => job.State == QueueState.Completed), "The normal queue pipeline executes all independently selected profiles with fresh source checks");
        MotionPreferences.SetTestOverride(null); app.Shutdown();

        void Until(Func<bool> predicate, string message)
        {
            DateTime deadline = DateTime.UtcNow.AddSeconds(10);
            while (!predicate() && DateTime.UtcNow < deadline) Pump();
            Check(predicate(), message);
        }
        void Complete(Task task) { Until(() => task.IsCompleted, "The queue operation finishes within the bounded fixture window"); task.GetAwaiter().GetResult(); }
        void Pump()
        {
            var frame = new DispatcherFrame();
            var pulse = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(20) };
            pulse.Tick += (_, _) => { pulse.Stop(); frame.Continue = false; }; pulse.Start(); Dispatcher.PushFrame(frame);
        }
    }
}
