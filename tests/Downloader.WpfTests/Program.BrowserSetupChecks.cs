using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using YouTubeDownloader;

internal static partial class Program
{
    private sealed class SetupFixture : IBrowserSetupActions
    {
        public bool IsConnected { get; private set; }
        internal bool FilesPresent = true;
        public bool CanPrepareExtension { get; set; }
        public void PrepareExtension(SetupBrowser browser) { Action("prepare"); FilesPresent = true; }
        internal BrowserStoreCatalog Catalog = new("", "", "");
        public string StoreAddress(SetupBrowser browser) => Catalog.Address(browser.ToString());
        public void OpenStore(SetupBrowser browser) { Action("store"); Opened = browser; }
        internal bool ApplyRegistration = true;
        internal string AppFolder = @"C:\Apps\Vydrop";
        internal string Failure, Copied;
        internal int Calls;
        internal readonly List<string> Trace = new();
        internal SetupBrowser? Opened, Folder, Guide;
        internal bool ArabicGuide;
        public string ExtensionPath(SetupBrowser browser) => AppFolder + @"\Browser\" + (browser == SetupBrowser.Firefox ? @"firefox\manifest.json" : "chromium");
        public bool HasExtension(SetupBrowser browser) => FilesPresent;
        private void Action(string name)
        {
            Calls++;
            Trace.Add(name);
            if (Failure == name) throw new InvalidOperationException("Fixture-only raw diagnostic");
        }
        public void Enable() { Action("enable"); if (ApplyRegistration) IsConnected = true; }
        public void Disable() { Action("disable"); IsConnected = false; }
        public void Copy(string text) { Action("copy"); Copied = text; }
        public void OpenExtensions(SetupBrowser browser) { Action("open"); Opened = browser; }
        public void ShowFolder(SetupBrowser browser) { Action("folder"); Folder = browser; }
        public void OpenGuide(SetupBrowser browser, bool arabic) { Action("guide"); Guide = browser; ArabicGuide = arabic; }
    }

    private static async Task CheckBrowserSetup(MainWindow owner, MainViewModel main, string captures)
    {
        double width = owner.Width, height = owner.Height;
        foreach (bool arabic in new[] { false, true })
            foreach (bool dark in new[] { false, true })
                foreach (bool compact in new[] { false, true })
                {
                    main.SelectLanguageCommand.Execute(arabic ? "ar" : "en");
                    if (main.IsDark != dark) main.ThemeCommand.Execute(null);
                    owner.Width = compact ? 660 : 900;
                    owner.Height = compact ? 500 : 800;
                    await Task.Delay(200);
                    var fixture = new SetupFixture();
                    string variant = (arabic ? "ar" : "en") + (dark ? "-dark" : "-light") + (compact ? "-short" : "-normal");
                    Exception failure = null;
                    var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    _ = owner.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(async () =>
                    {
                        BrowserIntegrationView dialog = null;
                        try
                        {
                            dialog = (BrowserIntegrationView)owner.WorkspaceView;
                            await Task.Delay(150);
                            var model = (BrowserSetupViewModel)dialog.DataContext;
                            var viewport = (ScrollViewer)WpfTree.Find(dialog, "SetupViewport");
                            var footer = (FrameworkElement)WpfTree.Find(dialog, "SetupFooter");
                            Check(fixture.Calls == 0 && !model.IsConnected && dialog.FlowDirection == main.ContentDirection && Equals(dialog.Background, owner.Background), "Opening setup follows language/theme and does not register, launch or copy anything");
                            Check(((System.Windows.Controls.Primitives.ToggleButton)WpfTree.Find(dialog, "SetupChrome")).IsKeyboardFocused, "Setup starts with the selected browser tab focused for keyboard access");
                            foreach (string name in new[] { "Edge", "Firefox", "Firefox", "Chrome" })
                            {
                                model.SelectBrowserCommand.Execute(name); dialog.UpdateLayout();
                                var selected = (System.Windows.Controls.Primitives.ToggleButton)WpfTree.Find(dialog, "Setup" + name);
                                Check(selected.IsChecked == true && fixture.Calls == 0, "Selecting a browser tab changes no files, registration, clipboard or browser windows: " + name);
                            }
                            var manual = (Expander)WpfTree.Find(dialog, "SetupManual");
                            Check(!manual.IsExpanded && ((Button)WpfTree.Find(dialog, "SetupStart")).IsVisible, "Setup offers one primary action and keeps manual troubleshooting collapsed");
                            manual.IsExpanded = true;
                            foreach (var browser in Enum.GetValues<SetupBrowser>())
                            {
                                model.SelectedBrowser = browser;
                                fixture.Trace.Clear();
                                model.StartSetupCommand.Execute(null);
                                Check(fixture.Trace.SequenceEqual(new[] { "enable", "copy", "open" }) && model.IsConnected && !model.HasError &&
                                    fixture.Copied == fixture.ExtensionPath(browser) && fixture.Opened == browser,
                                    "One setup action verifies connection, copies the right path and opens the selected browser in order: " + browser);
                                model.CopyAddressCommand.Execute(null);
                                Check(fixture.Copied == BrowserSetupActions.BrowserAddress(browser), "Copy browser address uses the selected browser: " + browser);
                                model.CopyPathCommand.Execute(null);
                                Check(fixture.Copied == fixture.ExtensionPath(browser) && model.IsFirefox == (browser == SetupBrowser.Firefox), "The selected extension path is the Chromium folder or Firefox manifest file");
                                model.OpenPageCommand.Execute(null);
                                model.ShowFolderCommand.Execute(null);
                                model.GuideCommand.Execute(null);
                                Check(fixture.Opened == browser && fixture.Folder == browser && fixture.Guide == browser && fixture.ArabicGuide == arabic, "Setup routes page, folder and translated guide actions to the chosen browser");
                            }
                            model.SelectedBrowser = SetupBrowser.Chrome;
                            fixture.FilesPresent = false;
                            model.SelectedBrowser = SetupBrowser.Edge;
                            await Task.Delay(50);
                            Check(model.StartSetupCommand.CanExecute(null) && !model.CopyPathCommand.CanExecute(null) && !model.ShowFolderCommand.CanExecute(null), "Unavailable files keep the guide recovery action enabled while disabling nonexistent paths");
                            fixture.Trace.Clear(); model.StartSetupCommand.Execute(null);
                            Check(fixture.Trace.SequenceEqual(new[] { "guide" }), "Missing files open recovery without registration, clipboard or browser-setup writes");
                            Check(!((Button)WpfTree.Find(dialog, "SetupCopyPath")).IsEnabled && !((FrameworkElement)WpfTree.Find(dialog, "SetupInstructions")).IsVisible && Descendants<TextBlock>(dialog).Any(label => label.IsVisible && label.Text == model.MissingFilesHint),
                                "Missing extension files explain recovery without instructions to load nonexistent files");
                            if (!compact && !dark) Capture(dialog, System.IO.Path.Combine(captures, "setup-missing-files-" + variant + ".png"));
                            fixture.FilesPresent = true;
                            model.SelectedBrowser = SetupBrowser.Chrome;
                            model.EnableCommand.Execute(null);
                            Check(model.IsConnected && !model.HasError && model.HasMessage, "Enabling updates persistent app-connection status without claiming the extension is installed");
                            fixture.Failure = "copy";
                            fixture.Trace.Clear();
                            model.StartSetupCommand.Execute(null);
                            Check(fixture.Trace.SequenceEqual(new[] { "enable", "copy", "open" }) && fixture.Opened == SetupBrowser.Chrome && model.IsConnected && model.HasError && manual.IsExpanded &&
                                model.Message == Texts.For(arabic ? "ar" : "en", "Extensions page opened. Copy the extension path from Connection help to finish setup."),
                                "A clipboard failure still opens the selected browser and reveals the selectable path with translated recovery");
                            model.CopyPathCommand.Execute(null);
                            Check(model.HasError && !model.Message.Contains("Fixture-only", StringComparison.Ordinal) && model.Message == Texts.For(arabic ? "ar" : "en", "Could not copy to the clipboard. Select the address or path and copy it manually."), "Clipboard failures have translated manual-copy recovery");
                            fixture.Failure = "open";
                            fixture.Trace.Clear();
                            model.StartSetupCommand.Execute(null);
                            Check(fixture.Trace.SequenceEqual(new[] { "enable", "copy", "open" }) && model.IsConnected && model.HasError &&
                                model.Message == Texts.For(arabic ? "ar" : "en", "Could not open this browser. Copy its address below and paste it into the browser address bar."),
                                "Browser failure reports the completed steps and exposes manual recovery without claiming installation");
                            model.OpenPageCommand.Execute(null);
                            Check(model.HasError && model.Message == Texts.For(arabic ? "ar" : "en", "Could not open this browser. Copy its address below and paste it into the browser address bar."), "A missing browser retains a selectable address and translated recovery");
                            fixture.Failure = null;
                            model.DisableCommand.Execute(null);
                            fixture.ApplyRegistration = false;
                            model.EnableCommand.Execute(null);
                            Check(!model.IsConnected && model.HasError, "A registration that cannot be verified never reports success");
                            fixture.ApplyRegistration = true;
                            fixture.Failure = "enable";
                            fixture.Trace.Clear();
                            model.StartSetupCommand.Execute(null);
                            Check(fixture.Trace.SequenceEqual(new[] { "enable" }) && !model.IsConnected && model.HasError,
                                "An unverified connection stops automatic setup before clipboard or browser changes");
                            model.EnableCommand.Execute(null);
                            Check(!model.IsConnected && model.HasError, "A failed registration never shows a connected state");
                            fixture.Failure = null;
                            model.EnableCommand.Execute(null);
                            manual.IsExpanded = false;
                            await Task.Delay(75);
                            dialog.UpdateLayout();
                            double footerTop = footer.TranslatePoint(new Point(), dialog).Y;
                            Check(viewport.ScrollableWidth <= 1 && footerTop + footer.ActualHeight <= dialog.ActualHeight &&
                                owner.OwnedWindows.Count == 0 && owner.ManagerContent.NavigationSurface.IsEnabled,
                                "Setup stays within the owner, avoids horizontal scrolling and keeps footer actions reachable");
                            Capture(dialog, System.IO.Path.Combine(captures, "setup-" + variant + ".png"));
                            if (compact) fixture.AppFolder = @"C:\Apps\A long permanent application folder with spaces\Vydrop downloaded packages\Current desktop application";
                            model.SelectedBrowser = SetupBrowser.Firefox;
                            manual.IsExpanded = true;
                            viewport.ScrollToEnd();
                            await Task.Delay(75);
                            dialog.UpdateLayout();
                            var hint = Descendants<TextBlock>(dialog).Single(label => label.Text == model.FirefoxHint);
                            Check(hint.IsVisible && model.CopyPathLabel == Texts.For(arabic ? "ar" : "en", "Copy file path") &&
                                Math.Abs(footerTop - footer.TranslatePoint(new Point(), dialog).Y) < 1 && viewport.ScrollableWidth <= 1,
                                "Firefox explains temporary loading and scrolling preserves the fixed footer");
                            var path = (TextBox)WpfTree.Find(dialog, "SetupExtensionPath");
                            Check(path.IsReadOnly && path.FlowDirection == FlowDirection.LeftToRight && (!compact || path.LineCount > 1),
                                "Long extension paths wrap and remain selectable in either language");
                            Capture(dialog, System.IO.Path.Combine(captures, "setup-firefox-" + variant + ".png"));
                            model.EnableCommand.Execute(null);
                            main.SelectLanguageCommand.Execute(arabic ? "en" : "ar");
                            await Task.Delay(60);
                            Check(dialog.FlowDirection == main.ContentDirection && model.Heading == Texts.For(main.IsArabic ? "ar" : "en", "Browser integration") && model.Message == Texts.For(main.IsArabic ? "ar" : "en", "Browser connection enabled. Load the companion extension to send links."), "An open browser section and its feedback follow a language change: " + $"language={main.IsArabic}, direction={dialog.FlowDirection}/{main.ContentDirection}, heading={model.Heading}, message={model.Message}");
                            model.GuideCommand.Execute(null);
                            Check(fixture.ArabicGuide == main.IsArabic, "The guide uses the current language after switching within the section");
                            main.SelectLanguageCommand.Execute(arabic ? "ar" : "en");
                            main.LibraryFilterCommand.Execute("queue");
                        }
                        catch (Exception error) { failure = error; }
                        finally { if (dialog?.IsVisible == true) main.LibraryFilterCommand.Execute("queue"); done.TrySetResult(); }
                    }));
                    new DesktopActions(owner).ShowBrowserIntegration(text => Texts.For(main.IsArabic ? "ar" : "en", text), fixture);
                    await done.Task;
                    if (failure != null) throw failure;
                }
        owner.Width = width; owner.Height = height;
        main.SelectLanguageCommand.Execute("en");
    }
}
