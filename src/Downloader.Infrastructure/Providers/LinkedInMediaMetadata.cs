using System.Collections;
using System.Globalization;

namespace YouTubeDownloader;

internal static class LinkedInMediaMetadata
{
    internal static Dictionary<string, object>[] ProbeFormats(Dictionary<string, object> data, string requestedUrl)
    {
        if (!Uri.TryCreate(requestedUrl, UriKind.Absolute, out var page) || !new LinkedInLinkResolver().Supports(page) ||
            (Text(data, "extractor_key") != "LinkedIn" && Text(data, "extractor") != "LinkedIn") ||
            Text(data, "has_drm") == "True" || !data.TryGetValue("formats", out var value) || value is not IEnumerable formats)
            return Array.Empty<Dictionary<string, object>>();
        // LinkedIn reports combined bitrate, but often omits all stream facts.
        // Measure only its direct public CDN resources, never infer audio or size from the URL.
        return formats.OfType<Dictionary<string, object>>().Where(format =>
            Text(format, "has_drm") != "True" && Text(format, "ext") == "mp4" &&
            (Text(format, "vcodec") is "" or "unknown" || Text(format, "acodec") is "" or "unknown" ||
                Text(format, "vcodec") != "none" && !Positive(format, "height")) &&
            Uri.TryCreate(Text(format, "url"), UriKind.Absolute, out var media) && media.Scheme == "https" &&
            media.UserInfo.Length == 0 && media.IsDefaultPort && media.IdnHost.EndsWith(".licdn.com", StringComparison.OrdinalIgnoreCase))
            .Take(6).ToArray();
    }

    private static string Text(Dictionary<string, object> data, string key) => data.TryGetValue(key, out var value) ? Convert.ToString(value, CultureInfo.InvariantCulture) ?? "" : "";
    private static bool Positive(Dictionary<string, object> data, string key) => double.TryParse(Text(data, key), NumberStyles.Float, CultureInfo.InvariantCulture, out double value) && value > 0 && double.IsFinite(value);
}
