using System.Text.Json;
using YouTubeDownloader;

internal static class TwitchClipChecks
{
    private const string Page = "https://www.twitch.tv/nasa/clip/TestClip-123";

    internal static void Run()
    {
        var format = Format("720", "https://cdn.cloudfront.net/landscape/hevc/720/index.mp4");
        var data = Data(format);
        Check(TwitchClipMetadata.ProbeFormats(data, Page).Single() == format, "Twitch clips with omitted stream metadata request a real probe");
        Check(TwitchClipMetadata.ProbeFormats(data, "https://clips.twitch.tv/TestClip-123").Length == 1, "Short clip permalinks retain measured formats");
        foreach (string page in new[] { "https://www.twitch.tv/nasa", "https://twitch.tv.evil.example/nasa/clip/Test", "https://user:secret@www.twitch.tv/nasa/clip/Test", "http://www.twitch.tv/nasa/clip/Test" })
            Check(TwitchClipMetadata.ProbeFormats(data, page).Length == 0, "Only a public HTTPS clip page qualifies");
        foreach (string url in new[] { "http://cdn.cloudfront.net/clip.mp4", "https://cdn.cloudfront.net.evil.example/clip.mp4", "https://127.0.0.1/clip.mp4", "https://cdn.cloudfront.net:8443/clip.mp4", "https://user:secret@cdn.cloudfront.net/clip.mp4", "https://cdn.cloudfront.net/clip.m3u8" })
            Check(TwitchClipMetadata.ProbeFormats(Data(Format("720", url)), Page).Length == 0, "Untrusted or nonprogressive probe targets are rejected");
        data["has_drm"] = true;
        Check(TwitchClipMetadata.ProbeFormats(data, Page).Length == 0, "Protected clip metadata is never probed");
        data = Data(format); format["vcodec"] = "hevc"; format["acodec"] = "aac"; format["width"] = 1280;
        Check(TwitchClipMetadata.ProbeFormats(data, Page).Length == 0, "Complete stream metadata avoids redundant network probes");
        format.Remove("vcodec"); format.Remove("acodec"); format.Remove("width");
        var many = Enumerable.Range(1, 10).Select(i => Format("portrait-" + i, "https://cdn.cloudfront.net/portrait/" + i + ".mp4"))
            .Concat(Enumerable.Range(1, 4).Select(i => Format(i.ToString(System.Globalization.CultureInfo.InvariantCulture), "https://cdn.cloudfront.net/landscape/" + i + ".mp4"))).ToArray();
        var bounded = TwitchClipMetadata.ProbeFormats(Data(many), Page);
        Check(bounded.Length == 6 && bounded.Take(4).All(f => !((string)f["format_id"]).StartsWith("portrait-", StringComparison.Ordinal)), "Bounded probes prioritize original landscape renditions");
        var facts = new Dictionary<string, object> { ["streams"] = new[] {
            new Dictionary<string, object> { ["codec_type"] = "video", ["codec_name"] = "h264", ["width"] = 1280, ["height"] = 720 },
            new Dictionary<string, object> { ["codec_type"] = "audio", ["codec_name"] = "aac", ["bit_rate"] = 128000 }
        } };
        DirectMediaMetadata.ApplyFormat(data, format, facts);
        Check(AvailableFormats.Read(data).Single().Resolution == 720 && AvailableFormats.HasAudio(data), "Measured Twitch formats expose video and audio instead of empty choices");
    }

    internal static async Task<int> Live(string url)
    {
        using var dependencies = new DependencyManager();
        using var provider = new YtDlpProvider(dependencies, checkAutomaticUpdates: false);
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(75));
        await provider.EnsureReadyAsync(null, deadline.Token);
        var media = provider.Inspect(url, deadline.Token);
        if (media.Qualities.Count == 0 || !media.HasAudio) throw new Exception("The public clip has no measured video/audio formats.");
        Console.WriteLine(JsonSerializer.Serialize(BrowserMediaFormats.From(media, url)));
        return 0;
    }

    private static Dictionary<string, object> Format(string id, string url) => new() { ["format_id"] = id, ["url"] = url, ["height"] = 720, ["ext"] = "mp4", ["protocol"] = "https" };
    private static Dictionary<string, object> Data(params Dictionary<string, object>[] formats) => new() { ["extractor_key"] = "TwitchClips", ["formats"] = formats };
    private static void Check(bool valid, string message) { if (!valid) throw new Exception(message); Console.WriteLine("PASS: " + message); }
}
