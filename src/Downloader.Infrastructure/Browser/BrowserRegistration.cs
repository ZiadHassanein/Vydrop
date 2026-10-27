using Microsoft.Win32;
using System.Text.Json;
using static YouTubeDownloader.BrowserIdentity;

namespace YouTubeDownloader;
public static class BrowserRegistration
{
    private static string DirectoryPath => Path.Combine(AppPaths.UserData, "Browser");

    private static readonly string[] RegistryPaths = { @"Software\Google\Chrome\NativeMessagingHosts\" + HostName, @"Software\Microsoft\Edge\NativeMessagingHosts\" + HostName, @"Software\Mozilla\NativeMessagingHosts\" + HostName };
    private static string ManifestDirectory(string executable) => BrowserStores.IsInstalled(executable) ? Path.Combine(Path.GetDirectoryName(Path.GetFullPath(executable)), "Integration") : DirectoryPath;

    public static bool EnsureAutomatic(string executable)
        => EnsureAutomatic(executable, AppPaths.UserData, IsEnabled, Enable);

    internal static bool EnsureAutomatic(string executable, string userData, Func<string, bool> connected, Action<string> enable)
    {
        try
        {
            if (File.Exists(AutomaticOptOut(userData))) return false;
            if (!connected(executable)) enable(executable);
            return connected(executable);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Security.SecurityException or ArgumentException)
        { return false; } // Keep the app usable; Connection help offers explicit repair.
    }

    private static string AutomaticOptOut(string userData) => Path.Combine(userData, "Browser", "automatic-connection.disabled");
    internal static void SetAutomaticDisabled(string userData, bool disabled)
    {
        string marker = AutomaticOptOut(userData);
        if (disabled) { Directory.CreateDirectory(Path.GetDirectoryName(marker)); File.WriteAllText(marker, "disabled"); }
        else if (File.Exists(marker)) File.Delete(marker);
    }

    public static bool IsEnabled(string executable)
    {
        try
        {
            if (!File.Exists(executable)) return false;
            foreach (RegistryView view in new[] { RegistryView.Registry32, RegistryView.Registry64 })
                using (var user = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, view))
                    for (int index = 0; index < RegistryPaths.Length; index++)
                    {
                        using var key = user.OpenSubKey(RegistryPaths[index]);
                        string manifest = Path.Combine(ManifestDirectory(executable), HostName + (index == 2 ? ".firefox.json" : ".chromium.json"));
                        if (!string.Equals(key?.GetValue("") as string, manifest, StringComparison.OrdinalIgnoreCase) ||
                            !MatchesManifest(File.ReadAllText(manifest), executable, index == 2)) return false;
                    }
            return true;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.Security.SecurityException or JsonException or ArgumentException)
        { return false; }
    }

    internal static bool MatchesManifest(string json, string executable, bool firefox)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        static string Text(JsonElement value, string key) => value.TryGetProperty(key, out var text) && text.ValueKind == JsonValueKind.String ? text.GetString() : null;
        if (root.ValueKind != JsonValueKind.Object || Text(root, "name") != HostName || Text(root, "type") != "stdio" ||
            !string.Equals(Text(root, "path"), executable, StringComparison.OrdinalIgnoreCase)) return false;
        var expected = firefox ? new[] { FirefoxId } : ChromiumIds.Select(id => "chrome-extension://" + id + "/").ToArray();
        return root.TryGetProperty(firefox ? "allowed_extensions" : "allowed_origins", out var origins) && origins.ValueKind == JsonValueKind.Array &&
            origins.GetArrayLength() == expected.Length && origins.EnumerateArray().All(value => value.ValueKind == JsonValueKind.String) &&
            origins.EnumerateArray().Select(value => value.GetString()).Order(StringComparer.Ordinal).SequenceEqual(expected.Order(StringComparer.Ordinal));
    }

    public static void Enable(string executable)
    {
        if (!Path.IsPathFullyQualified(executable) || !File.Exists(executable) || Path.GetExtension(executable) != ".exe") throw new IOException("Keep Vydrop.exe in a permanent folder before connecting your browser.");
        string directory = ManifestDirectory(executable);
        Directory.CreateDirectory(directory);
        var chromium = new { name = HostName, description = "Send a media link to Vydrop", path = executable, type = "stdio", allowed_origins = ChromiumIds.Select(id => "chrome-extension://" + id + "/").ToArray() };
        var firefox = new { name = HostName, description = "Send a media link to Vydrop", path = executable, type = "stdio", allowed_extensions = new[] { FirefoxId } };
        string chromePath = Path.Combine(directory, HostName + ".chromium.json"), firefoxPath = Path.Combine(directory, HostName + ".firefox.json");
        File.WriteAllText(chromePath, JsonSerializer.Serialize(chromium));
        File.WriteAllText(firefoxPath, JsonSerializer.Serialize(firefox));
        foreach (RegistryView view in new[] { RegistryView.Registry32, RegistryView.Registry64 })
            using (var user = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, view))
                for (int i = 0; i < RegistryPaths.Length; i++)
                {
                    using var key = user.CreateSubKey(RegistryPaths[i]);
                    key.SetValue("", i == 2 ? firefoxPath : chromePath, RegistryValueKind.String);
                }
        SetAutomaticDisabled(AppPaths.UserData, false);
    }
    public static void Disable()
    {
        SetAutomaticDisabled(AppPaths.UserData, true);
        foreach (RegistryView view in new[] { RegistryView.Registry32, RegistryView.Registry64 })
            using (var user = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, view))
                foreach (string path in RegistryPaths)
                {
                    using var key = user.OpenSubKey(path, true);
                    if (key?.GetValue("") is string value && Path.GetDirectoryName(value) == DirectoryPath) key.DeleteValue("", false);
                }
        foreach (string name in new[] { HostName + ".chromium.json", HostName + ".firefox.json" })
        {
            string file = Path.Combine(DirectoryPath, name);
            if (File.Exists(file)) File.Delete(file);
        }
    }
    public static void Disable(string executable)
    {
        SetAutomaticDisabled(AppPaths.UserData, true);
        string directory = ManifestDirectory(executable);
        foreach (RegistryView view in new[] { RegistryView.Registry32, RegistryView.Registry64 })
            using (var user = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, view))
                for (int index = 0; index < RegistryPaths.Length; index++)
                {
                    using var key = user.OpenSubKey(RegistryPaths[index], true);
                    string manifest = Path.Combine(directory, HostName + (index == 2 ? ".firefox.json" : ".chromium.json"));
                    if (string.Equals(key?.GetValue("") as string, manifest, StringComparison.OrdinalIgnoreCase) && File.Exists(manifest) && MatchesManifest(File.ReadAllText(manifest), executable, index == 2)) key.DeleteValue("", false);
                }
        // Keep manifests: uninstall owns installed files, and another copy may still refer to them.
    }
}
