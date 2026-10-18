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
    public IReadOnlyList<SetupBrowser> Browsers { get; } = Enum.GetValues<SetupBrowser>();
    public SetupBrowser SelectedBrowser
    {
        get => selectedBrowser;
        set
        {
            if (!SetProperty(ref selectedBrowser, value)) return;
            Message = "";
            OnPropertyChanged(string.Empty);
            CopyPathCommand.NotifyCanExecuteChanged();
            ShowFolderCommand.NotifyCanExecuteChanged();
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
    public string Message { get => message; private set { if (SetProperty(ref message, value)) OnPropertyChanged(nameof(HasMessage)); } }
    public bool HasMessage => Message.Length > 0;
    public bool HasError { get => hasError; private set => SetProperty(ref hasError, value); }
    public bool IsFirefox => SelectedBrowser == SetupBrowser.Firefox;
    public string ExtensionPath => actions.ExtensionPath(SelectedBrowser);
    public bool HasExtension => actions.HasExtension(SelectedBrowser);
    public string BrowserAddress => BrowserSetupActions.BrowserAddress(SelectedBrowser);
    public string Heading => t("Browser extension setup");
    public string Intro => t("Send a video to Vydrop, then review it before downloading.");
    public string BrowserLabel => t("Your browser");
    public string ConnectHeading => t("1. Connect the app");
    public string ConnectionStatus => t(IsConnected ? "Connected to this Vydrop app" : "App connection is not enabled");
    public string KeepFolderHint => t("Keep Vydrop in a permanent folder. Reconnect here if you move it.");
    public string EnableLabel => t(IsConnected ? "Reconnect app" : "Enable connection");
    public string DisableLabel => t("Disable connection");
    public string ConnectionOptionsLabel => t("Connection options");
    public string InstallHeading => string.Format(System.Globalization.CultureInfo.CurrentCulture, t("2. Add the extension to {0}"), SelectedBrowser);
    public string OpenPageLabel => t("Open extensions page");
    public string CopyAddressLabel => t("Copy browser address");
    public string LoadHint => t(IsFirefox
        ? "Choose This Firefox → Load Temporary Add-on…, then open the manifest.json file shown below."
        : "Turn on Developer mode, choose Load unpacked, then select the folder shown below.");
    public string PathLabel => t(IsFirefox ? "File to select" : "Folder to select");
    public string CopyPathLabel => t(IsFirefox ? "Copy file path" : "Copy folder path");
    public string ShowFolderLabel => t("Show extension files");
    public string MissingFilesHint => t("Companion files are missing. Extract the complete Vydrop ZIP and reopen the app.");
    public string FirefoxHint => t("Firefox removes this temporary add-on when it restarts. Load it again next session.");
    public string TestHeading => t("3. Try it on a video");
    public string TestHint => t("Close setup, refresh a video page and play the video. Click its Vydrop button or the extension in your browser toolbar.");
    public string ReviewHint => t("Nothing downloads until you choose Download in Vydrop.");
    public string GuideLabel => t("View illustrated guide");
    public string CloseLabel => t("Close");
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
        this.actions = actions;
        IsConnected = actions.IsConnected;
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
        GuideCommand = new RelayCommand(() => Run(() => actions.OpenGuide(SelectedBrowser, arabic),
            "The illustrated guide opened in your browser.", "Could not open the guide. Visit the Vydrop documentation website."));
    }

    private void SetConnection(bool enable)
    {
        try { if (enable) actions.Enable(); else actions.Disable(); }
        finally { IsConnected = actions.IsConnected; }
        if (IsConnected != enable) throw new IOException();
    }

    private void Run(Action action, string success, string failure)
    {
        try { action(); HasError = false; Message = t(success); }
        catch (Exception error) when (error is not OutOfMemoryException)
        { HasError = true; Message = t(failure); }
    }
}
