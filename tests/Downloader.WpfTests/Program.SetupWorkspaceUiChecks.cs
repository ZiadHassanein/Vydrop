using System.IO;
using System.IO.Compression;
using System.Windows.Controls;
using YouTubeDownloader;

internal static partial class Program
{
    private static async Task CheckSetupWorkspaceUi(MainWindow window, MainViewModel model, FakeProvider provider, string root, string captures)
    {
        string versionText = "v" + AppUpdates.Current.ToString(3) + (ReleaseChannel.IsPreview ? " · Preview" : "");
        model.Url = "";
        foreach (string language in new[] { "en", "ar" }) foreach (bool dark in new[] { false, true }) foreach (double width in new[] { 1280d, 760d })
        {
            model.SelectLanguageCommand.Execute(language); if (model.IsDark != dark) model.ThemeCommand.Execute(null);
            window.Width = width; window.Height = width == 760 ? 640 : 800;
            model.NewDownloadCommand.Execute(null); await Task.Delay(100); window.UpdateLayout();
            Check(window.ManagerContent.DetailsColumn.Width.Value == 0 && !window.ManagerContent.DetailsBorder.IsVisible && window.DownloadForm.LinkInput.IsVisible, "Empty Add download has no reserved inspector column: " + language + "/" + dark + "/" + width);
            Check(!window.DownloadForm.InlineActivity.IsVisible && !model.IsCheckingMedia, "An empty link does not show stale checking or transfer feedback");
            Check(!Descendants<TextBlock>(window).Any(text => text.IsVisible && text.Text == versionText), "The version is absent from the visible workspace");
            Capture(window, Path.Combine(captures, $"add-empty-{language}-{dark}-{width}.png"));
            model.SettingsVisible = true; await Task.Delay(100); window.UpdateLayout();
            Check(window.ManagerContent.DetailsColumn.Width.Value == 0 && !window.ManagerContent.DetailsBorder.IsVisible, "Settings uses the available center width without an empty inspector");
            Check(Descendants<TextBlock>(window.EditorOverlay).Count(text => text.IsVisible && text.Text == model.SettingsLabel) == 1 && !Descendants<Button>(window.EditorHeader).Any(), "Settings has one workspace title and uses tab navigation without a modal Close action");
            Check(!Descendants<TextBlock>(window).Any(text => text.IsVisible && text.Text == versionText), "Settings also omits the version label");
            Capture(window, Path.Combine(captures, $"settings-{language}-{dark}-{width}.png"));
            model.SettingsVisible = false;
        }
        window.Width = 1280; window.Height = 800; model.SelectLanguageCommand.Execute("en");
        using (var hold = new ManualResetEventSlim())
        {
            provider.InspectionHook = token => hold.Wait(token);
            model.Url = "https://youtu.be/jNQXAC9IVRw";
            await WaitFor(() => model.IsCheckingMedia, "The stale-state fixture starts a real held inspection");
            model.Url = "";
            await WaitFor(() => model.IsIdle, "Clearing the link cancels the obsolete inspection");
            Check(!model.IsCheckingMedia && !model.ShowInlineActivity && !model.HasMediaPreview && window.ManagerContent.DetailsColumn.Width.Value == 0, "A late cancelled inspection cannot resurrect the empty form status or preview");
        }
        provider.InspectionHook = null;
        model.Url = "https://youtu.be/jNQXAC9IVRw";
        await WaitFor(() => model.CanDownload && model.PreviewImage != null, "The real preview still loads after an empty form");
        Check(window.ManagerContent.DetailsColumn.Width.Value > 0 && window.MediaPreview.IsVisible, "Ready metadata restores the wide preview inspector");
        Capture(window, Path.Combine(captures, "add-ready-inspector.png"));
        model.Url = "";
        CheckEmbeddedCompanion(root);
        foreach (string language in new[] { "en", "ar" }) foreach (bool dark in new[] { false, true })
        {
            model.SelectLanguageCommand.Execute(language); if (model.IsDark != dark) model.ThemeCommand.Execute(null);
            var recovery = new SetupFixture { FilesPresent = false, CanPrepareExtension = true };
            Exception failure = null;
            var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _ = window.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ApplicationIdle, new Action(async () =>
            {
                BrowserSetupWindow dialog = null;
                try
                {
                    dialog = window.OwnedWindows.OfType<BrowserSetupWindow>().Single(); await Task.Delay(120);
                    var setupModel = (BrowserSetupViewModel)dialog.DataContext;
                    var viewport = (ScrollViewer)WpfTree.Find(dialog, "SetupViewport"); dialog.UpdateLayout();
                    Check(recovery.Calls == 0 && !setupModel.ShowMissingFiles && ((Button)WpfTree.Find(dialog, "SetupStart")).IsEnabled, "A standalone app offers setup without missing-file errors or opening-time actions");
                    Check(viewport.ScrollableHeight <= 1, "Normal setup keeps the primary steps and footer visible without scrolling");
                    Capture(dialog, Path.Combine(captures, $"setup-embedded-before-{language}-{dark}.png"));
                    setupModel.StartSetupCommand.Execute(null); await Task.Delay(60); dialog.UpdateLayout();
                    Check(setupModel.IsConnected && !setupModel.IsVerified && !setupModel.HasError && recovery.Trace.SequenceEqual(new[] { "prepare", "enable", "copy", "open" }), "Explicit recovery enables the native app without falsely claiming verified browser connection");
                    Check(viewport.ScrollableHeight <= 1, "Successful setup also keeps the next steps and troubleshooting visible without scrolling");
                    Capture(dialog, Path.Combine(captures, $"setup-embedded-after-{language}-{dark}.png"));
                }
                catch (Exception error) { failure = error; }
                finally { if (dialog?.IsVisible == true) dialog.Close(); done.TrySetResult(); }
            }));
            new DesktopActions(window).ShowBrowserIntegration(key => Texts.For(language, key), recovery);
            await done.Task; if (failure != null) throw failure;
        }
        var fixture = new SetupFixture { FilesPresent = false, CanPrepareExtension = true };
        var setup = new BrowserSetupViewModel(key => key, fixture, false);
        Check(fixture.Calls == 0 && !setup.HasExtension && !setup.ShowMissingFiles && setup.StartSetupCommand.CanExecute(null), "Embedded recovery is offered without writes when setup opens");
        setup.StartSetupCommand.Execute(null);
        Check(fixture.Trace.SequenceEqual(new[] { "prepare", "enable", "copy", "open" }) && setup.HasExtension && setup.CopyPathCommand.CanExecute(null), "Explicit setup prepares and verifies files before registration, clipboard and browser launch");
        fixture = new SetupFixture { FilesPresent = false, CanPrepareExtension = true, Failure = "prepare" };
        setup = new BrowserSetupViewModel(key => key, fixture, false); setup.StartSetupCommand.Execute(null);
        Check(fixture.Trace.SequenceEqual(new[] { "prepare" }) && setup.HasError && !setup.IsConnected && !setup.HasExtension, "Failed preparation stops before connection changes and exposes a retry");
    }

    private static void CheckEmbeddedCompanion(string root)
    {
        string app = Path.Combine(root, "standalone-app"), data = Path.Combine(root, "companion-recovery");
        var companion = new BrowserCompanionFiles(app, data);
        Check(companion.CanPrepare && !companion.HasExtension(SetupBrowser.Chrome) && !Directory.Exists(data), "The app embeds a complete recovery archive and opening it creates no files");
        companion.Prepare(SetupBrowser.Chrome);
        string chrome = companion.ExtensionPath(SetupBrowser.Chrome);
        Check(companion.HasExtension(SetupBrowser.Chrome) && File.Exists(Path.Combine(chrome, "quality-picker.js")) && File.Exists(Path.Combine(chrome, "icon.png")), "A standalone executable prepares all Chromium scripts, artwork and manifest");
        string sentinel = Path.Combine(chrome, "user-sentinel.txt"); File.WriteAllText(sentinel, "Keep this file");
        string script = Path.Combine(chrome, "quality-picker.js"); string expected = File.ReadAllText(script); File.WriteAllText(script, "corrupt");
        Check(!companion.HasExtension(SetupBrowser.Chrome), "A manifest alone cannot hide a missing or corrupt companion script");
        companion.Prepare(SetupBrowser.Chrome);
        Check(File.ReadAllText(script) == expected && File.Exists(sentinel), "Recovery repairs known files atomically without deleting unrelated files");
        companion.Prepare(SetupBrowser.Firefox);
        Check(companion.HasExtension(SetupBrowser.Firefox) && companion.ExtensionPath(SetupBrowser.Firefox).EndsWith("manifest.json", StringComparison.Ordinal), "Firefox recovery exposes its verified manifest file");
        Check(new BrowserCompanionFiles(app, data).HasExtension(SetupBrowser.Chrome), "Prepared companions remain available after reopening the app");
        using var buffer = new MemoryStream();
        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, true))
        {
            using var writer = new StreamWriter(zip.CreateEntry("chromium/../../escape.txt").Open()); writer.Write("Invalid");
        }
        var unsafeArchive = new BrowserCompanionFiles(app, Path.Combine(root, "unsafe-archive"), buffer.ToArray());
        Check(!unsafeArchive.CanPrepare && !Directory.Exists(Path.Combine(root, "unsafe-archive")), "Malformed embedded archive paths cannot write outside the recovery directory");
        foreach (bool wrongChannel in new[] { false, true })
        {
            using var resource = typeof(BrowserCompanionFiles).Assembly.GetManifestResourceStream("YouTubeDownloader.BrowserCompanion");
            using var source = new ZipArchive(resource, ZipArchiveMode.Read);
            using var variant = new MemoryStream();
            using (var zip = new ZipArchive(variant, ZipArchiveMode.Create, true))
                foreach (var entry in source.Entries)
                {
                    if (!wrongChannel && entry.FullName == "chromium/quality-picker.js") continue;
                    using var input = entry.Open(); using var output = zip.CreateEntry(entry.FullName).Open();
                    if (wrongChannel && entry.FullName.EndsWith("manifest.json", StringComparison.Ordinal))
                    {
                        using var writer = new StreamWriter(output);
                        writer.Write("{\"manifest_version\":3,\"name\":\"Wrong channel\"}");
                    }
                    else input.CopyTo(output);
                }
            var rejected = new BrowserCompanionFiles(app, Path.Combine(root, "invalid-companion"), variant.ToArray());
            Check(!rejected.CanPrepare && !Directory.Exists(Path.Combine(root, "invalid-companion")), "Incomplete or mismatched-channel companions cannot be offered as recoverable: " + wrongChannel);
        }
    }
}
