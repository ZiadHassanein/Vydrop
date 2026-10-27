using System.Text.Json;
namespace YouTubeDownloader;
public static class BrowserHealth
{
    internal static string DirectoryOverride { get; set; }
    private static string Root => DirectoryOverride ?? Path.Combine(AppPaths.UserData, "Browser");
    private static string FilePath => Path.Combine(Root, "verified-connection.json");
    internal static void Record(string executable, string companionVersion = null, string browser = null)
    {
        try
        {
            UpdateTransaction.CheckRegularPath(FilePath);
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
            var report = JsonSerializer.Serialize(new { executable, build = File.GetLastWriteTimeUtc(executable), verifiedAt = DateTimeOffset.UtcNow, companionVersion });
            File.WriteAllText(FilePath, report);
            if (browser is "Chrome" or "Edge" or "Firefox" && companionVersion != null)
            {
                string path = Path.Combine(Root, "verified-companion-" + browser + ".json");
                UpdateTransaction.CheckRegularPath(path);
                File.WriteAllText(path, report);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
    }
    public static bool IsRecentlyVerified(string executable)
    {
        try
        {
            UpdateTransaction.CheckRegularPath(FilePath);
            if (new FileInfo(FilePath).Length > 4096) return false;
            using var data = JsonDocument.Parse(File.ReadAllText(FilePath));
            var root = data.RootElement;
            return root.GetProperty("executable").GetString() == executable && root.GetProperty("build").GetDateTime() == File.GetLastWriteTimeUtc(executable) &&
                DateTimeOffset.UtcNow - root.GetProperty("verifiedAt").GetDateTimeOffset() is var elapsed && elapsed >= TimeSpan.Zero && elapsed < TimeSpan.FromMinutes(2);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or FormatException or InvalidOperationException or KeyNotFoundException) { return false; }
    }
    public static string CompanionVersion(string executable, string browser)
    {
        if (browser is not ("Chrome" or "Edge" or "Firefox")) return null;
        try
        {
            string path = Path.Combine(Root, "verified-companion-" + browser + ".json");
            UpdateTransaction.CheckRegularPath(path);
            if (new FileInfo(path).Length > 4096) return null;
            using var data = JsonDocument.Parse(File.ReadAllText(path));
            var root = data.RootElement;
            string version = root.GetProperty("companionVersion").GetString();
            return root.GetProperty("executable").GetString() == executable && root.GetProperty("build").GetDateTime() == File.GetLastWriteTimeUtc(executable) &&
                DateTimeOffset.UtcNow - root.GetProperty("verifiedAt").GetDateTimeOffset() is var elapsed && elapsed >= TimeSpan.Zero && elapsed < TimeSpan.FromMinutes(2) &&
                version?.Length <= 24 && System.Text.RegularExpressions.Regex.IsMatch(version, @"^\d{1,5}(?:\.\d{1,5}){2,3}$") ? version : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or FormatException or InvalidOperationException or KeyNotFoundException) { return null; }
    }

}
