using System.Text.Json;
using System.Text.RegularExpressions;

namespace YouTubeDownloader;

public sealed record BrowserStoreCatalog(string ChromeId, string EdgeId, string FirefoxListingUrl)
{
    public string Address(string browser) => browser switch
    {
        "Chrome" when ChromeId.Length > 0 => "https://chromewebstore.google.com/detail/" + ChromeId,
        "Edge" when EdgeId.Length > 0 => "https://microsoftedge.microsoft.com/addons/detail/" + EdgeId,
        "Firefox" => FirefoxListingUrl,
        _ => ""
    };
    internal static BrowserStoreCatalog Parse(string json)
    {
        using var document = JsonDocument.Parse(json, new() { MaxDepth = 4 });
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object || root.GetProperty("format").GetInt32() != 1) throw new InvalidDataException("Invalid store catalog.");
        string Read(string name) => root.GetProperty(name).GetString() ?? throw new InvalidDataException("Invalid store value.");
        string chrome = Read("chromeId"), edge = Read("edgeId"), firefox = Read("firefoxListingUrl");
        foreach (string id in new[] { chrome, edge }) if (id.Length > 0 && !Regex.IsMatch(id, "^[a-p]{32}$")) throw new InvalidDataException("Use the assigned browser-store ID.");
        if (firefox.Length > 0 && (!Uri.TryCreate(firefox, UriKind.Absolute, out var url) || url.Scheme != "https" || url.Host != "addons.mozilla.org" || url.UserInfo.Length > 0 || !url.IsDefaultPort || url.Query.Length > 0 || url.Fragment.Length > 0 || !Regex.IsMatch(url.AbsolutePath, @"^/(?:[A-Za-z-]+/)?firefox/addon/[a-z0-9-]+/$")))
            throw new InvalidDataException("Use the official Firefox listing URL.");
        return new(chrome, edge, firefox);
    }
}

public static class BrowserStores
{
    private static readonly BrowserStoreCatalog Empty = new("", "", "");
    public static BrowserStoreCatalog Current { get; } = Load();
    private static BrowserStoreCatalog Load()
    {
        // Public store companions belong to stable; previews retain their separate native host.
        if (ReleaseChannel.IsPreview) return Empty;
        using var stream = typeof(BrowserStores).Assembly.GetManifestResourceStream("YouTubeDownloader.BrowserStores");
        if (stream == null) return Empty;
        using var reader = new StreamReader(stream);
        return BrowserStoreCatalog.Parse(reader.ReadToEnd());
    }
    public static bool IsInstalled(string executable)
    {
        try
        {
            string file = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(executable)), "installation.json");
            if (new FileInfo(file).Length > 4096) return false;
            using var document = JsonDocument.Parse(File.ReadAllText(file));
            var root = document.RootElement;
            return root.GetProperty("format").GetInt32() == 1 && root.GetProperty("kind").GetString() == "installer" &&
                root.GetProperty("channel").GetString() == (ReleaseChannel.IsPreview ? "preview" : "stable");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or ArgumentException or InvalidOperationException or KeyNotFoundException) { return false; }
    }
}
