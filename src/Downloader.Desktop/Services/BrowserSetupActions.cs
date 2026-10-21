using Microsoft.Win32;
using System.Diagnostics;
using System.Windows;

namespace YouTubeDownloader;

internal enum SetupBrowser { Chrome, Edge, Firefox }

internal interface IBrowserSetupActions
{
    bool IsConnected { get; }
    bool IsVerified => false;
    string StoreAddress(SetupBrowser browser) => "";
    void OpenStore(SetupBrowser browser) => throw new InvalidOperationException();
    string ExtensionPath(SetupBrowser browser);
    bool HasExtension(SetupBrowser browser);
    bool CanPrepareExtension => false;
    void PrepareExtension(SetupBrowser browser) => throw new InvalidOperationException();
    void Enable();
    void Disable();
    void Copy(string text);
    void OpenExtensions(SetupBrowser browser);
    void ShowFolder(SetupBrowser browser);
    void OpenGuide(SetupBrowser browser, bool arabic);
}

internal sealed class BrowserSetupActions : IBrowserSetupActions
{
    public bool IsConnected => BrowserRegistration.IsEnabled(Environment.ProcessPath);
    public bool IsVerified => IsConnected && BrowserHealth.IsRecentlyVerified(Environment.ProcessPath);
    public string StoreAddress(SetupBrowser browser) => BrowserStores.Current.Address(browser.ToString());
    public void OpenStore(SetupBrowser browser)
    {
        string address = StoreAddress(browser);
        if (address.Length == 0) throw new InvalidOperationException();
        OpenBrowser(browser, address);
    }
    private readonly BrowserCompanionFiles companion = new(AppContext.BaseDirectory, AppPaths.UserData);
    public string ExtensionPath(SetupBrowser browser) => companion.ExtensionPath(browser);
    public bool HasExtension(SetupBrowser browser) => companion.HasExtension(browser);
    public bool CanPrepareExtension => companion.CanPrepare;
    public void PrepareExtension(SetupBrowser browser) => companion.Prepare(browser);
    public void Enable() => BrowserRegistration.Enable(Environment.ProcessPath);
    public void Disable() => BrowserRegistration.Disable(Environment.ProcessPath);
    public void Copy(string text) => Clipboard.SetText(text);
    public void ShowFolder(SetupBrowser browser)
    {
        string folder = browser == SetupBrowser.Firefox ? Path.GetDirectoryName(ExtensionPath(browser)) : ExtensionPath(browser);
        Process.Start(new ProcessStartInfo(folder) { UseShellExecute = true });
    }
    public void OpenGuide(SetupBrowser browser, bool arabic)
        => Process.Start(new ProcessStartInfo("https://ziadhassanein.github.io/Vydrop/" + (arabic ? "ar/" : "") + "browser-extension-setup.html#" + browser.ToString().ToLowerInvariant()) { UseShellExecute = true });

    internal static string BrowserAddress(SetupBrowser browser) => browser switch
    {
        SetupBrowser.Chrome => "chrome://extensions",
        SetupBrowser.Edge => "edge://extensions",
        _ => "about:debugging#/runtime/this-firefox"
    };

    public void OpenExtensions(SetupBrowser browser)
        => OpenBrowser(browser, BrowserAddress(browser));

    private static void OpenBrowser(SetupBrowser browser, string address)
    {
        string executable = browser switch { SetupBrowser.Chrome => "chrome.exe", SetupBrowser.Edge => "msedge.exe", _ => "firefox.exe" };
        string relative = browser switch
        {
            SetupBrowser.Chrome => @"Google\Chrome\Application\chrome.exe",
            SetupBrowser.Edge => @"Microsoft\Edge\Application\msedge.exe",
            _ => @"Mozilla Firefox\firefox.exe"
        };
        var candidates = new List<string>();
        foreach (var hive in new[] { RegistryHive.CurrentUser, RegistryHive.LocalMachine })
            foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
            {
                using var root = RegistryKey.OpenBaseKey(hive, view);
                using var key = root.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\App Paths\" + executable);
                if (key?.GetValue("") is string path) candidates.Add(path.Trim('"'));
            }
        foreach (var folder in new[] { Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolder.ProgramFiles, Environment.SpecialFolder.ProgramFilesX86 })
            candidates.Add(Path.Combine(Environment.GetFolderPath(folder), relative));
        string application = candidates.FirstOrDefault(path => Path.IsPathFullyQualified(path) && File.Exists(path));
        if (application == null) throw new FileNotFoundException();
        var start = new ProcessStartInfo(application) { UseShellExecute = false };
        start.ArgumentList.Add(address);
        Process.Start(start);
    }
}
