using System.Text.Json;
namespace YouTubeDownloader;
public static class BrowserHealth
{
    private static string FilePath => Path.Combine(AppPaths.UserData, "Browser", "verified-connection.json");
    internal static void Record(string executable)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
            File.WriteAllText(FilePath, JsonSerializer.Serialize(new { executable, build = File.GetLastWriteTimeUtc(executable), verifiedAt = DateTimeOffset.UtcNow }));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
    }
    public static bool IsRecentlyVerified(string executable)
    {
        try
        {
            if (new FileInfo(FilePath).Length > 4096) return false;
            using var data = JsonDocument.Parse(File.ReadAllText(FilePath));
            var root = data.RootElement;
            return root.GetProperty("executable").GetString() == executable && root.GetProperty("build").GetDateTime() == File.GetLastWriteTimeUtc(executable) &&
                DateTimeOffset.UtcNow - root.GetProperty("verifiedAt").GetDateTimeOffset() is var elapsed && elapsed >= TimeSpan.Zero && elapsed < TimeSpan.FromMinutes(2);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or FormatException or InvalidOperationException or KeyNotFoundException) { return false; }
    }
}
