using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using YouTubeDownloader;

internal static partial class Program
{
    private static async Task CheckStoreSetup(MainWindow owner, MainViewModel main, string captures)
    {
        CheckInstallerSetup();
        foreach (bool arabic in new[] { false, true })
        foreach (bool dark in new[] { false, true })
        foreach (bool published in new[] { false, true })
        {
            main.SelectLanguageCommand.Execute(arabic ? "ar" : "en");
            if (main.IsDark != dark) main.ThemeCommand.Execute(null);
            owner.Width = 740; owner.Height = published ? 800 : 500;
            await Task.Delay(100);
            var fixture = new SetupFixture();
            if (published) fixture.Catalog = new(new string('a', 32), new string('b', 32), "https://addons.mozilla.org/firefox/addon/vydrop/");
            Exception failure = null;
            var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _ = owner.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(async () =>
            {
                BrowserIntegrationView dialog = null;
                try
                {
                    dialog = (BrowserIntegrationView)owner.WorkspaceView;
                    await Task.Delay(100);
                    var model = (BrowserSetupViewModel)dialog.DataContext;
                    var manual = (Expander)WpfTree.Find(dialog, "SetupManual");
                    Check(model.UseStoreSetup == published && !manual.IsExpanded && fixture.Calls == 0, "Onboarding chooses only an available store and has no opening side effects");
                    foreach (var browser in Enum.GetValues<SetupBrowser>())
                    {
                        model.SelectedBrowser = browser;
                        fixture.Trace.Clear();
                        Check(model.StartSetupCommand.CanExecute(null) && model.ShowFirefoxTemporaryHint == (!published && browser == SetupBrowser.Firefox), "Store availability is honest and temporary Firefox instructions stay out of the normal flow");
                        if (published)
                        {
                            model.StartSetupCommand.Execute(null);
                            Check(fixture.Trace.SequenceEqual(new[] { "enable", "store" }) && fixture.Opened == browser && !model.HasError && fixture.Copied == null, "Store setup registers and opens the chosen official listing without clipboard or developer-mode steps");
                            fixture.Failure = "store";
                            model.StartSetupCommand.Execute(null);
                            Check(model.HasError && model.IsConnected, "Store-launch failure retains the app connection and provides recovery");
                            fixture.Failure = null;
                            model.StartSetupCommand.Execute(null);
                        }
                        else
                        {
                            model.StartSetupCommand.Execute(null);
                            Check(fixture.Trace.SequenceEqual(new[] { "enable", "copy", "open" }) && !model.HasError, "An unpublished store falls back to working local setup instead of a disabled button");
                        }
                    }
                    var viewport = (ScrollViewer)WpfTree.Find(dialog, "SetupViewport");
                    var footer = (FrameworkElement)WpfTree.Find(dialog, "SetupFooter");
                    dialog.UpdateLayout();
                    Check(viewport.ScrollableWidth <= 1 && footer.TranslatePoint(new Point(), dialog).Y + footer.ActualHeight <= dialog.ActualHeight, "Store setup has no horizontal overflow and keeps footer reachable at short height");
                    Capture(dialog, System.IO.Path.Combine(captures, $"store-{(published ? "ready" : "pending")}-{(arabic ? "ar" : "en")}-{(dark ? "dark" : "light")}.png"));
                    manual.IsExpanded = true;
                    model.SelectedBrowser = SetupBrowser.Firefox;
                    fixture.Trace.Clear();
                    model.LocalSetupCommand.Execute(null);
                    await Task.Delay(50);
                    dialog.UpdateLayout();
                    Check(fixture.Trace.SequenceEqual(new[] { "enable", "copy", "open" }) && model.ShowFirefoxTemporaryHint && (((Button)WpfTree.Find(dialog, "SetupLocal")).IsVisible == published), "Manual recovery retains local loading and explains Firefox temporary installation");
                    fixture.FilesPresent = false;
                    model.SelectedBrowser = SetupBrowser.Chrome;
                    Check(!model.LocalSetupCommand.CanExecute(null) && model.StartSetupCommand.CanExecute(null), "Store installation works without local files while manual loading is disabled");
                }
                catch (Exception error) { failure = error; }
                finally { if (dialog?.IsVisible == true) ((MainViewModel)owner.DataContext).LibraryFilterCommand.Execute("queue"); done.TrySetResult(); }
            }));
            new DesktopActions(owner).ShowBrowserIntegration(text => Texts.For(arabic ? "ar" : "en", text), fixture);
            await done.Task;
            if (failure != null) throw failure;
        }
        await CheckInstallerDialog(owner);
    }
    private static async Task CheckInstallerDialog(MainWindow owner)
    {
        var fixture = new SetupFixture { Catalog = new(new string('a', 32), new string('b', 32), "https://addons.mozilla.org/firefox/addon/vydrop/") };
        fixture.Enable(); fixture.Trace.Clear();
        Exception failure = null;
        var finished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _ = owner.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(async () =>
        {
            BrowserIntegrationView dialog = null;
            try
            {
                dialog = (BrowserIntegrationView)owner.WorkspaceView;
                await Task.Delay(120);
                Check(((BrowserSetupViewModel)dialog.DataContext).SelectedBrowser == SetupBrowser.Edge && fixture.Trace.SequenceEqual(new[] { "store" }),
                    "The actual installer-owned dialog continues into the selected browser without another Set up click or registration rewrite");
            }
            catch (Exception error) { failure = error; }
            finally { ((MainViewModel)owner.DataContext).LibraryFilterCommand.Execute("queue"); finished.TrySetResult(); }
        }));
        new DesktopActions(owner).ShowBrowserIntegration(text => text, fixture, SetupBrowser.Edge);
        await finished.Task;
        if (failure != null) throw failure;
    }

    private static void CheckInstallerSetup()
    {
        foreach (var browser in Enum.GetValues<SetupBrowser>())
        foreach (bool published in new[] { false, true })
        foreach (bool registered in new[] { false, true })
        {
            var fixture = new SetupFixture();
            if (published) fixture.Catalog = new(new string('a', 32), new string('b', 32), "https://addons.mozilla.org/firefox/addon/vydrop/");
            if (registered) fixture.Enable();
            fixture.Trace.Clear();
            var model = new BrowserSetupViewModel(text => text, fixture, false);
            Check(fixture.Trace.Count == 0, "Ordinary setup construction stays passive");
            model.BeginInstallerSetup(browser);
            var expected = new List<string>();
            if (!registered) expected.Add("enable");
            expected.AddRange(published ? new[] { "store" } : new[] { "copy", "open" });
            Check(model.SelectedBrowser == browser && fixture.Opened == browser && fixture.Trace.SequenceEqual(expected) && !model.HasError,
                "Installer hands off directly to its chosen browser and preserves the installed registration: " + browser + "/" + published + "/" + registered);
            int countBefore = fixture.Calls;
            model.BeginInstallerSetup(browser);
            Check(fixture.Calls == countBefore, "A repeated render cannot launch the installer handoff twice");
        }
        var missing = new SetupFixture { FilesPresent = false, CanPrepareExtension = true };
        missing.Enable(); missing.Trace.Clear();
        var recovery = new BrowserSetupViewModel(text => text, missing, false);
        recovery.BeginInstallerSetup(SetupBrowser.Edge);
        Check(missing.Trace.SequenceEqual(new[] { "prepare", "copy", "open" }), "Installer fallback restores companion files before copying their path");
        var failure = new SetupFixture { Failure = "copy" };
        failure.Enable(); failure.Trace.Clear();
        var failed = new BrowserSetupViewModel(text => text, failure, false);
        failed.BeginInstallerSetup(SetupBrowser.Chrome);
        Check(failed.IsConnected && failed.HasError && failure.Trace.SequenceEqual(new[] { "copy" }), "Installer handoff stops on clipboard failure without undoing the connection or launching a broken step");
    }

}
