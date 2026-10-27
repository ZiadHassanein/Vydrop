using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace YouTubeDownloader;

internal sealed class BrowserSetupViewModel : ObservableObject
{
    private readonly Func<string, string> t;
    private readonly IBrowserSetupActions actions;
    private SetupBrowser selectedBrowser;
    private bool isConnected;
    private string message = "";
    private bool hasError;
    private bool isRecoveryExpanded;
    private bool localSetupStarted;
    private bool installerSetupStarted;
    private bool guideArabic;
    internal void RefreshLanguage(bool arabic) { guideArabic = arabic; OnPropertyChanged(string.Empty); }
    public bool IsChrome => SelectedBrowser == SetupBrowser.Chrome;
    public bool IsEdge => SelectedBrowser == SetupBrowser.Edge;
    public IRelayCommand<string> SelectBrowserCommand { get; }
    public SetupBrowser SelectedBrowser
    {
        get => selectedBrowser;
        set
        {
            if (!SetProperty(ref selectedBrowser, value)) return;
            localSetupStarted = false;
            IsRecoveryExpanded = false;
            Message = "";
            OnPropertyChanged(string.Empty);
            CopyPathCommand.NotifyCanExecuteChanged();
            ShowFolderCommand.NotifyCanExecuteChanged();
            StartSetupCommand.NotifyCanExecuteChanged();
            LocalSetupCommand.NotifyCanExecuteChanged();
        }
    }
    public bool IsConnected
    {
        get => isConnected;
        private set
        {
            if (!SetProperty(ref isConnected, value)) return;
            OnPropertyChanged(nameof(ConnectionStatus));
            OnPropertyChanged(nameof(EnableLabel));
        }
    }
    public string Message { get => t(message); private set { if (SetProperty(ref message, value)) OnPropertyChanged(nameof(HasMessage)); } }
    public bool HasMessage => Message.Length > 0;
    public bool HasError { get => hasError; private set => SetProperty(ref hasError, value); }
    public bool IsRecoveryExpanded { get => isRecoveryExpanded; set => SetProperty(ref isRecoveryExpanded, value); }
    public bool IsFirefox => SelectedBrowser == SetupBrowser.Firefox;
    public bool UseStoreSetup => HasStoreListing;
    public bool HasStoreListing => actions.StoreAddress(SelectedBrowser).Length > 0;
    public bool ShowFirefoxTemporaryHint => IsFirefox && (!UseStoreSetup || localSetupStarted);
    public bool ShowMissingFiles => !UseStoreSetup && !HasExtension && !actions.CanPrepareExtension;
    public string ExtensionPath => actions.ExtensionPath(SelectedBrowser);
    public bool HasExtension => actions.HasExtension(SelectedBrowser);
    public string BrowserAddress => BrowserSetupActions.BrowserAddress(SelectedBrowser);
    public string Heading => t("Browser integration");
    public string Intro => t("Load the extension once. Vydrop connects automatically.");
    public string BrowserLabel => t("Your browser");
    public string ConnectionStatus => t(IsConnected ? "Connected to this Vydrop app" : "App connection is not enabled");
    public bool IsVerified => actions.IsVerified;
    public string VerificationStatus => t(IsVerified ? "Browser connection verified" : "Browser connection not tested yet");
    public string VerificationHint => TestHint;
    public string UpdateHint => t("If Reload shows an old version, use Load unpacked with the copied folder once.");
    private static string ExpectedExtensionVersion => BrowserIdentity.CompanionVersion;
    public string ReportedExtensionVersion => actions.CompanionVersion(SelectedBrowser);
    public bool HasOldExtension => Version.TryParse(ReportedExtensionVersion, out var version) && version < Version.Parse(ExpectedExtensionVersion);
    public string ExtensionVersionStatus => string.Format(System.Globalization.CultureInfo.CurrentCulture,
        t(ReportedExtensionVersion == null ? "Extension {0} is ready to load. Browser version is unconfirmed." : HasOldExtension ? "Browser extension {1} is old. Load {0} from the copied folder." : "Browser extension {0} is up to date."), ExpectedExtensionVersion, ReportedExtensionVersion);
    internal void RefreshVerification()
    {
        OnPropertyChanged(nameof(IsVerified)); OnPropertyChanged(nameof(VerificationStatus));
        OnPropertyChanged(nameof(ReportedExtensionVersion)); OnPropertyChanged(nameof(HasOldExtension));
        OnPropertyChanged(nameof(ExtensionVersionStatus)); OnPropertyChanged(nameof(StartSetupLabel));
    }
    public string KeepFolderHint => t("This extension folder stays the same when Vydrop moves or updates.");
    public string EnableLabel => t(IsConnected ? "Reconnect app" : "Enable connection");
    public string DisableLabel => t("Disable connection");
    public string OpenPageLabel => t("Open extensions page");
    public string CopyAddressLabel => t("Copy browser address");
    public string PathLabel => t(IsFirefox ? "File to select" : "Folder to select");
    public string CopyPathLabel => t(IsFirefox ? "Copy file path" : "Copy folder path");
    public string ShowFolderLabel => t("Show extension files");
    public string MissingFilesHint => t("Extension files are unavailable. Open the illustrated guide to restore them.");
    public string FirefoxHint => t("Firefox removes this temporary add-on when it restarts. Load it again next session.");
    public string TestHint => t("Refresh a video page and click its Vydrop button. Choose the format and quality, then Download.");
    public string GuideLabel => t("View illustrated guide");
    public string StartSetupLabel => HasOldExtension && !UseStoreSetup ? t("Update extension") : ShowMissingFiles ? GuideLabel : UseStoreSetup ? string.Format(System.Globalization.CultureInfo.CurrentCulture, t("Add to {0}"), SelectedBrowser) : string.Format(System.Globalization.CultureInfo.CurrentCulture, t("Set up {0}"), SelectedBrowser);
    public string StartSetupHint => ShowMissingFiles ? MissingFilesHint : t(UseStoreSetup
        ? "Add the extension from the official store."
        : "Load the extension in your browser. Its folder is copied for you.");
    public string ManualStepsLabel => t("Finish in your browser");
    public string BrowserSteps => UseStoreSetup ? t("Choose Add or Get on the official store page, approve the extension, then refresh your video page.") : t(IsFirefox
        ? "Choose This Firefox → Load Temporary Add-on…, paste the copied file path and choose Open."
        : "Turn on Developer mode → Load unpacked. Paste the copied folder path and choose Select Folder.");
    public string LocalSetupHint => t(IsFirefox
        ? "Choose This Firefox → Load Temporary Add-on…, paste the copied file path and choose Open."
        : "Turn on Developer mode → Load unpacked. Paste the copied folder path and choose Select Folder.");
    public string RecoveryLabel => t("Connection help");
    public string ClosedAppHint => t("Once connected, the extension can open Vydrop even when the app is closed.");
    public IRelayCommand StartSetupCommand { get; }
    public IRelayCommand LocalSetupCommand { get; }
    public string LocalSetupLabel => t("Load the local companion");
    public IRelayCommand EnableCommand { get; }
    public IRelayCommand DisableCommand { get; }
    public IRelayCommand OpenPageCommand { get; }
    public IRelayCommand CopyAddressCommand { get; }
    public IRelayCommand CopyPathCommand { get; }
    public IRelayCommand ShowFolderCommand { get; }
    public IRelayCommand GuideCommand { get; }

    internal BrowserSetupViewModel(Func<string, string> translate, IBrowserSetupActions actions, bool arabic)
    {
        t = translate;
        guideArabic = arabic;
        this.actions = actions;
        SelectBrowserCommand = new RelayCommand<string>(name =>
        {
            if (Enum.TryParse<SetupBrowser>(name, out var browser) && Enum.IsDefined(browser)) SelectedBrowser = browser;
            OnPropertyChanged(string.Empty);
        });
        IsConnected = actions.IsConnected;
        StartSetupCommand = new RelayCommand(StartSetup);
        LocalSetupCommand = new RelayCommand(StartLocalSetup, () => HasExtension || actions.CanPrepareExtension);
        EnableCommand = new RelayCommand(() => Run(() => SetConnection(true),
            "Browser connection enabled. Load the companion extension to send links.",
            "Could not change the browser connection. Keep Vydrop.exe in a writable folder and try again."));
        DisableCommand = new RelayCommand(() => Run(() => SetConnection(false),
            "Browser connection disabled.", "Could not change the browser connection. Keep Vydrop.exe in a writable folder and try again."));
        OpenPageCommand = new RelayCommand(() => Run(() => actions.OpenExtensions(SelectedBrowser),
            "Complete the loading step in your browser, then refresh your video page.", "Could not open this browser. Copy its address below and paste it into the browser address bar."));
        CopyAddressCommand = new RelayCommand(() => Run(() => actions.Copy(BrowserAddress),
            "Browser address copied. Paste it into your browser address bar.", "Could not copy to the clipboard. Select the address or path and copy it manually."));
        CopyPathCommand = new RelayCommand(() => Run(() => actions.Copy(ExtensionPath),
            "Extension path copied. Paste it into the browser file picker.", "Could not copy to the clipboard. Select the address or path and copy it manually."), () => HasExtension);
        ShowFolderCommand = new RelayCommand(() => Run(() => actions.ShowFolder(SelectedBrowser),
            "Choose these extension files in the browser loading dialog.", "Could not open the extension folder. Copy its path and open it in File Explorer."), () => HasExtension);
        GuideCommand = new RelayCommand(() => Run(() => actions.OpenGuide(SelectedBrowser, guideArabic),
            "The illustrated guide opened in your browser.", "Could not open the guide. Visit the Vydrop documentation website."));
    }

    private void StartSetup()
        => StartSetup(false);

    internal void BeginInstallerSetup(SetupBrowser browser)
    {
        // Only the explicit installer handoff starts setup on opening. Ordinary Settings stays passive.
        if (installerSetupStarted) return;
        installerSetupStarted = true;
        SelectedBrowser = browser;
        StartSetup(true);
    }

    private void StartSetup(bool reuseInstalledConnection)
    {
        if (ShowMissingFiles) { GuideCommand.Execute(null); return; }
        if (!UseStoreSetup) { StartLocalSetup(reuseInstalledConnection); return; }
        if (!EnsureConnection(reuseInstalledConnection)) return;
        Run(() => actions.OpenStore(SelectedBrowser), "Complete the loading step in your browser, then refresh your video page.", "Could not open the store page. Open the illustrated guide for help.");
    }

    private void StartLocalSetup()
        => StartLocalSetup(false);

    private bool EnsureConnection(bool reuseInstalledConnection)
        => reuseInstalledConnection && IsConnected || Run(() => SetConnection(true), "", "Could not change the browser connection. Keep Vydrop.exe in a writable folder and try again.");

    private void StartLocalSetup(bool reuseInstalledConnection)
    {
        if (!HasExtension)
        {
            if (!Run(() => actions.PrepareExtension(SelectedBrowser), "", "Could not prepare extension files. Check folder access and try again.")) return;
            OnPropertyChanged(string.Empty);
            CopyPathCommand.NotifyCanExecuteChanged(); ShowFolderCommand.NotifyCanExecuteChanged(); LocalSetupCommand.NotifyCanExecuteChanged();
        }
        localSetupStarted = true;
        OnPropertyChanged(nameof(ShowFirefoxTemporaryHint));
        // Clipboard access is optional; always attempt the selected browser after connecting.
        if (!EnsureConnection(reuseInstalledConnection)) return;
        bool copied = Run(() => actions.Copy(ExtensionPath), "", "Could not copy to the clipboard. Select the address or path and copy it manually.");
        Run(() => actions.OpenExtensions(SelectedBrowser),
            copied ? "App connected and extension path copied. Finish setup in your browser."
                : "Extensions page opened. Copy the extension path from Connection help to finish setup.",
            "Could not open this browser. Copy its address below and paste it into the browser address bar.");
        if (!copied) HasError = true;
    }

    private void SetConnection(bool enable)
    {
        try { if (enable) actions.Enable(); else actions.Disable(); }
        finally { IsConnected = actions.IsConnected; }
        if (IsConnected != enable) throw new IOException();
    }

    private bool Run(Action action, string success, string failure)
    {
        try { action(); HasError = false; Message = success; return true; }
        catch (Exception error) when (error is not OutOfMemoryException)
        { HasError = true; IsRecoveryExpanded = true; Message = failure; return false; }
    }
}
