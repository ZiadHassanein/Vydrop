using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using YouTubeDownloader;

internal static partial class Program
{
    private static async Task CheckStoreSetup(MainWindow owner, MainViewModel main, string captures)
    {
        foreach (bool arabic in new[] { false, true })
        foreach (bool dark in new[] { false, true })
        foreach (bool published in new[] { false, true })
        {
            main.SelectLanguageCommand.Execute(arabic ? "ar" : "en");
            if (main.IsDark != dark) main.ThemeCommand.Execute(null);
            owner.Width = 740; owner.Height = published ? 800 : 500;
            await Task.Delay(100);
            var fixture = new SetupFixture { IsInstalled = true };
            if (published) fixture.Catalog = new(new string('a', 32), new string('b', 32), "https://addons.mozilla.org/firefox/addon/vydrop/");
            Exception failure = null;
            var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            _ = owner.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(async () =>
            {
                BrowserSetupWindow dialog = null;
                try
                {
                    dialog = owner.OwnedWindows.OfType<BrowserSetupWindow>().Single();
                    await Task.Delay(100);
                    var model = (BrowserSetupViewModel)dialog.DataContext;
                    var manual = (Expander)WpfTree.Find(dialog, "SetupManual");
                    Check(model.UseStoreSetup && !manual.IsExpanded && fixture.Calls == 0, "Installed onboarding offers store setup without side effects");
                    foreach (var browser in Enum.GetValues<SetupBrowser>())
                    {
                        model.SelectedBrowser = browser;
                        fixture.Trace.Clear();
                        Check(model.StartSetupCommand.CanExecute(null) == published && !model.ShowFirefoxTemporaryHint, "Store availability is honest and temporary Firefox instructions stay out of the normal flow");
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
                        else Check(model.StartSetupHint == Texts.For(arabic ? "ar" : "en", "Vydrop is ready. The browser-store release is being prepared. Until it is published, use Manual setup and troubleshooting below."), "Pending publication is explained in the selected language");
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
                    Check(fixture.Trace.SequenceEqual(new[] { "enable", "copy", "open" }) && model.ShowFirefoxTemporaryHint && ((Button)WpfTree.Find(dialog, "SetupLocal")).IsVisible, "Manual recovery retains local loading and explains Firefox temporary installation");
                    fixture.FilesPresent = false;
                    model.SelectedBrowser = SetupBrowser.Chrome;
                    Check(!model.LocalSetupCommand.CanExecute(null) && model.StartSetupCommand.CanExecute(null) == published, "Store installation works without local files while manual loading is disabled");
                }
                catch (Exception error) { failure = error; }
                finally { if (dialog?.IsVisible == true) dialog.Close(); done.TrySetResult(); }
            }));
            new DesktopActions(owner).ShowBrowserIntegration(text => Texts.For(arabic ? "ar" : "en", text), fixture);
            await done.Task;
            if (failure != null) throw failure;
        }
    }
}
