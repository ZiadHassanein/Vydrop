using System.Collections;
using System.Globalization;
using System.Text.RegularExpressions;

namespace YouTubeDownloader;

internal static class TwitterMediaMetadata
{
    internal static Dictionary<string, object>[] ProbeFormats(Dictionary<string, object> data, string requestedUrl)
    {
        if (!Uri.TryCreate(requestedUrl, UriKind.Absolute, out var page) || page.Scheme != "https" ||
            page.UserInfo.Length != 0 || !page.IsDefaultPort ||
            !Regex.IsMatch(page.IdnHost, @"^(www\.|m\.|mobile\.)?(twitter|x)\.com$", RegexOptions.IgnoreCase) ||
            !Regex.IsMatch(page.AbsolutePath, @"^/(?:[A-Za-z0-9_]+/status|i/web/status|statuses)/[0-9]{8,25}(?:/video/[0-9]+)?/?$") ||
            Text(data, "extractor_key") != "Twitter" || Text(data, "has_drm") == "True" ||
            !data.TryGetValue("formats", out var value) || value is not IEnumerable formats)
            return Array.Empty<Dictionary<string, object>>();

        // Progressive X video metadata can omit the audio codec even when the
        // selected MP4 contains sound. Measure only those declared public files.
        return formats.OfType<Dictionary<string, object>>().Where(format =>
            Text(format, "has_drm") != "True" && Text(format, "ext") == "mp4" &&
            Text(format, "protocol") == "https" && Text(format, "vcodec") != "none" &&
            (Text(format, "vcodec") is "" or "unknown" || Text(format, "acodec") is "" or "unknown") &&
            Uri.TryCreate(Text(format, "url"), UriKind.Absolute, out var media) && media.Scheme == "https" &&
            media.UserInfo.Length == 0 && media.IsDefaultPort && media.IdnHost == "video.twimg.com" &&
            Path.GetExtension(media.AbsolutePath).Equals(".mp4", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(format => Number(format, "height")).Take(6).ToArray();
    }

    private static string Text(Dictionary<string, object> data, string key) => data.TryGetValue(key, out var value) ? Convert.ToString(value, CultureInfo.InvariantCulture) ?? "" : "";
    private static double Number(Dictionary<string, object> data, string key) => double.TryParse(Text(data, key), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && double.IsFinite(value) ? value : 0;
}
