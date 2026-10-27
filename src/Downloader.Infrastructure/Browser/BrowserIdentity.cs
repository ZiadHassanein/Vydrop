namespace YouTubeDownloader;
public static class BrowserIdentity
{
    public const string CompanionVersion = "1.5.9";
    // Retain installed companion IDs and native host names through the Vydrop rebrand.
    public static string HostName => ReleaseChannel.IsPreview ? "com.vifree.preview" : "com.vifree";
    // Public development-extension identity; no signing secret is shipped.
    public const string ChromiumId = "cpoiefoeogidkjcgbacikpcdjkjeekcf";
    public static IReadOnlyList<string> ChromiumIds => new[] { ChromiumId, BrowserStores.Current.ChromeId, BrowserStores.Current.EdgeId }.Where(id => id.Length > 0).Distinct(StringComparer.Ordinal).ToArray();
    public static string FirefoxId => ReleaseChannel.IsPreview ? "vifree-preview@ziadhassanein.github.io" : "vifree@ziadhassanein.github.io";
}
