using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Threading;
using YouTubeDownloader;

internal static partial class Program
{
    private sealed class SetupFixture : IBrowserSetupActions
    {
        public bool IsConnected { get; private set; }
        internal bool FilesPresent = true;
        internal bool ApplyRegistration = true;
        internal string AppFolder = @"C:\Apps\Vydrop";
        internal string Failure, Copied;
        internal int Calls;
        internal SetupBrowser? Opened, Folder, Guide;
        internal bool ArabicGuide;
        public string ExtensionPath(SetupBrowser browser) => AppFolder + @"\Browser\" + (browser == SetupBrowser.Firefox ? @"firefox\manifest.json" : "chromium");
        public bool HasExtension(SetupBrowser browser) => FilesPresent;
        private void Action(string name)
        {
            Calls++;
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
                        BrowserSetupWindow dialog = null;
                        try
                        {
                            dialog = owner.OwnedWindows.OfType<BrowserSetupWindow>().Single();
                            await Task.Delay(150);
                            var model = (BrowserSetupViewModel)dialog.DataContext;
                            var viewport = (ScrollViewer)WpfTree.Find(dialog, "SetupViewport");
                            var footer = (FrameworkElement)WpfTree.Find(dialog, "SetupFooter");
                            Check(fixture.Calls == 0 && !model.IsConnected && dialog.FlowDirection == main.ContentDirection && Equals(dialog.Background, owner.Background), "Opening setup follows language/theme and does not register, launch or copy anything");
                            Check(((ComboBox)WpfTree.Find(dialog, "SetupBrowserPicker")).IsKeyboardFocused, "Setup starts with the browser selector focused for keyboard access");
                            foreach (var browser in Enum.GetValues<SetupBrowser>())
                            {
                                model.SelectedBrowser = browser;
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
                            Check(!model.CopyPathCommand.CanExecute(null) && !model.ShowFolderCommand.CanExecute(null), "Missing package files cannot be copied or opened as if installed");
                            Check(!((Button)WpfTree.Find(dialog, "SetupCopyPath")).IsEnabled && Descendants<TextBlock>(dialog).Any(label => label.IsVisible && label.Text == model.MissingFilesHint),
                                "Missing extension files visibly disable path actions and explain how to recover");
                            if (!compact && !dark) Capture(dialog, System.IO.Path.Combine(captures, "setup-missing-files-" + variant + ".png"));
                            fixture.FilesPresent = true;
                            model.SelectedBrowser = SetupBrowser.Chrome;
                            model.EnableCommand.Execute(null);
                            Check(model.IsConnected && !model.HasError && model.HasMessage, "Enabling updates persistent app-connection status without claiming the extension is installed");
                            fixture.Failure = "copy";
                            model.CopyPathCommand.Execute(null);
                            Check(model.HasError && !model.Message.Contains("Fixture-only", StringComparison.Ordinal) && model.Message == Texts.For(arabic ? "ar" : "en", "Could not copy to the clipboard. Select the address or path and copy it manually."), "Clipboard failures have translated manual-copy recovery");
                            fixture.Failure = "open";
                            model.OpenPageCommand.Execute(null);
                            Check(model.HasError && model.Message == Texts.For(arabic ? "ar" : "en", "Could not open this browser. Copy its address below and paste it into the browser address bar."), "A missing browser retains a selectable address and translated recovery");
                            fixture.Failure = null;
                            model.DisableCommand.Execute(null);
                            fixture.ApplyRegistration = false;
                            model.EnableCommand.Execute(null);
                            Check(!model.IsConnected && model.HasError, "A registration that cannot be verified never reports success");
                            fixture.ApplyRegistration = true;
                            fixture.Failure = "enable";
                            model.EnableCommand.Execute(null);
                            Check(!model.IsConnected && model.HasError, "A failed registration never shows a connected state");
                            fixture.Failure = null;
                            model.EnableCommand.Execute(null);
                            await Task.Delay(75);
                            dialog.UpdateLayout();
                            double footerTop = footer.TranslatePoint(new Point(), dialog).Y;
                            Check(viewport.ScrollableWidth <= 1 && footerTop + footer.ActualHeight <= dialog.ActualHeight &&
                                dialog.ActualWidth <= owner.ActualWidth - 47 && dialog.ActualHeight <= owner.ActualHeight - 47,
                                "Setup stays within the owner, avoids horizontal scrolling and keeps footer actions reachable");
                            Capture(dialog, System.IO.Path.Combine(captures, "setup-" + variant + ".png"));
                            if (compact) fixture.AppFolder = @"C:\Apps\A long permanent application folder with spaces\Vydrop downloaded packages\Current desktop application";
                            model.SelectedBrowser = SetupBrowser.Firefox;
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
                            var close = (Button)WpfTree.Find(dialog, "SetupClose");
                            ((IInvokeProvider)new ButtonAutomationPeer(close).GetPattern(PatternInterface.Invoke)).Invoke();
                        }
                        catch (Exception error) { failure = error; }
                        finally { if (dialog?.IsVisible == true) dialog.Close(); done.TrySetResult(); }
                    }));
                    new DesktopActions(owner).ShowBrowserIntegration(text => Texts.For(arabic ? "ar" : "en", text), fixture);
                    await done.Task;
                    if (failure != null) throw failure;
                }
        owner.Width = width; owner.Height = height;
        main.SelectLanguageCommand.Execute("en");
    }
}
