using System.Collections;
using System.Globalization;
using System.Text.RegularExpressions;

namespace YouTubeDownloader;

internal static class RedditMediaMetadata
{
    internal static Dictionary<string, object>[] ProbeFormats(Dictionary<string, object> data, string requestedUrl)
    {
        if (!Uri.TryCreate(requestedUrl, UriKind.Absolute, out var page) || !PublicReddit(page) ||
            !Regex.IsMatch(page.AbsolutePath, @"^/[a-z0-9]{5,32}/HLSPlaylist\.m3u8$") ||
            Text(data, "extractor_key") != "Generic" || Text(data, "has_drm") == "True" ||
            !data.TryGetValue("formats", out var value) || value is not IEnumerable formats)
            return Array.Empty<Dictionary<string, object>>();
        string asset = page.AbsolutePath.Split('/')[1];
        // Reddit's public HLS master omits the audio codec. Probe only its
        // declared audio playlists so quality selection cannot produce a silent file.
        return formats.OfType<Dictionary<string, object>>().Where(format =>
            Text(format, "has_drm") != "True" && Text(format, "vcodec") == "none" &&
            Text(format, "acodec") is "" or "unknown" && Text(format, "protocol") == "m3u8_native" &&
            Uri.TryCreate(Text(format, "url"), UriKind.Absolute, out var audio) && PublicReddit(audio) &&
            Regex.IsMatch(audio.AbsolutePath, "^/" + Regex.Escape(asset) + @"/(?:CMAF|HLS)_AUDIO_[A-Za-z0-9_-]{1,40}\.m3u8$"))
            .Take(4).ToArray();
    }

    internal static string AudioFormat(Dictionary<string, object> data, string requestedUrl, VideoContainer container = VideoContainer.Mp4)
    {
        if (!Uri.TryCreate(requestedUrl, UriKind.Absolute, out var page) || !PublicReddit(page) ||
            !Regex.IsMatch(page.AbsolutePath, @"^/[a-z0-9]{5,32}/HLSPlaylist\.m3u8$") ||
            Text(data, "extractor_key") != "Generic" || Text(data, "has_drm") == "True" ||
            !data.TryGetValue("formats", out var value) || value is not IEnumerable formats) return null;
        string prefix = "/" + page.AbsolutePath.Split('/')[1] + "/";
        return formats.OfType<Dictionary<string, object>>().Where(format =>
            Text(format, "vcodec") == "none" && Text(format, "acodec") is not ("" or "none" or "unknown") && AvailableFormats.CompatibleAudio(Text(format, "acodec"), container) && Text(format, "has_drm") != "True" &&
            Regex.IsMatch(Text(format, "format_id"), @"^[A-Za-z0-9_.-]{1,128}$") &&
            Uri.TryCreate(Text(format, "url"), UriKind.Absolute, out var audio) && PublicReddit(audio) &&
            audio.AbsolutePath.StartsWith(prefix, StringComparison.Ordinal) &&
            Regex.IsMatch(audio.AbsolutePath[prefix.Length..], @"^(?:CMAF|HLS)_AUDIO_[A-Za-z0-9_-]{1,40}\.m3u8$"))
            .OrderByDescending(format => double.TryParse(Text(format, "abr"), NumberStyles.Float, CultureInfo.InvariantCulture, out double bitrate) && double.IsFinite(bitrate) ? bitrate : 0)
            .Select(format => Text(format, "format_id")).FirstOrDefault();
    }

    private static bool PublicReddit(Uri url) => url.Scheme == "https" && url.IdnHost == "v.redd.it" && url.UserInfo.Length == 0 && url.IsDefaultPort;
    private static string Text(Dictionary<string, object> data, string key) => data.TryGetValue(key, out var value) ? Convert.ToString(value, CultureInfo.InvariantCulture) ?? "" : "";
}
