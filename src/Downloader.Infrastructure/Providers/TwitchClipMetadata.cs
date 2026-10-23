using System.Collections;
using System.Globalization;
using System.Text.RegularExpressions;

namespace YouTubeDownloader;

internal static class TwitchClipMetadata
{
    internal static Dictionary<string, object>[] ProbeFormats(Dictionary<string, object> data, string requestedUrl)
    {
        if (!Uri.TryCreate(requestedUrl, UriKind.Absolute, out var page) || page.Scheme != "https" ||
            page.UserInfo.Length != 0 || !page.IsDefaultPort || !IsClip(page) ||
            Text(data, "extractor_key") != "TwitchClips" || Text(data, "has_drm") == "True" ||
            !data.TryGetValue("formats", out var value) || value is not IEnumerable formats)
            return Array.Empty<Dictionary<string, object>>();

        // Clip metadata can omit codecs and width for every rendition. Measure
        // the actual streams rather than inventing codecs from an MP4 suffix.
        // Prioritize the original landscape renditions and bound network work.
        return formats.OfType<Dictionary<string, object>>().Where(format =>
            Text(format, "has_drm") != "True" && Text(format, "ext") == "mp4" &&
            Text(format, "protocol") == "https" && Text(format, "vcodec") != "none" &&
            (Text(format, "vcodec") is "" or "unknown" || Text(format, "acodec") is "" or "unknown" ||
                Number(format, "height") <= 0 || Number(format, "width") <= 0) &&
            Uri.TryCreate(Text(format, "url"), UriKind.Absolute, out var media) && media.Scheme == "https" &&
            media.UserInfo.Length == 0 && media.IsDefaultPort &&
            Path.GetExtension(media.AbsolutePath).Equals(".mp4", StringComparison.OrdinalIgnoreCase) &&
            (Regex.IsMatch(media.IdnHost, @"^[a-z0-9-]+\.cloudfront\.net$", RegexOptions.IgnoreCase) ||
                media.IdnHost.EndsWith(".twitchcdn.net", StringComparison.OrdinalIgnoreCase) ||
                Regex.IsMatch(media.IdnHost, @"^clips-media-assets\d*\.twitch\.tv$", RegexOptions.IgnoreCase)))
            .OrderBy(format => Text(format, "format_id").StartsWith("portrait-", StringComparison.Ordinal) ? 1 : 0)
            .ThenByDescending(format => Number(format, "height")).Take(6).ToArray();
    }

    private static bool IsClip(Uri page) => page.IdnHost.Equals("clips.twitch.tv", StringComparison.OrdinalIgnoreCase)
        ? Regex.IsMatch(page.AbsolutePath, @"^/[A-Za-z0-9_-]+/?$")
        : Regex.IsMatch(page.IdnHost, @"^(www\.|m\.)?twitch\.tv$", RegexOptions.IgnoreCase) &&
            Regex.IsMatch(page.AbsolutePath, @"^/[A-Za-z0-9_]+/clip/[A-Za-z0-9_-]+/?$");
    private static string Text(Dictionary<string, object> data, string key) => data.TryGetValue(key, out var value) ? Convert.ToString(value, CultureInfo.InvariantCulture) ?? "" : "";
    private static double Number(Dictionary<string, object> data, string key) => double.TryParse(Text(data, key), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && double.IsFinite(value) ? value : 0;
}
