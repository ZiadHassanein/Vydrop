using YouTubeDownloader;

internal static class RedditMediaChecks
{
    internal static void Run()
    {
        const string page = "https://v.redd.it/selected123/HLSPlaylist.m3u8";
        var audio = new Dictionary<string, object> { ["format_id"] = "audio", ["url"] = "https://v.redd.it/selected123/CMAF_AUDIO_128.m3u8", ["vcodec"] = "none", ["protocol"] = "m3u8_native", ["ext"] = "mp4" };
        var video = new Dictionary<string, object> { ["format_id"] = "720", ["url"] = "https://v.redd.it/selected123/CMAF_720.m3u8", ["vcodec"] = "avc1.4d401f", ["acodec"] = "none", ["height"] = 720, ["width"] = 1280 };
        var data = new Dictionary<string, object> { ["extractor_key"] = "Generic", ["format_id"] = "audio", ["formats"] = new[] { audio, video } };
        Check(RedditMediaMetadata.ProbeFormats(data, page).Single() == audio, "Reddit probes declared audio instead of silently discarding it");
        foreach (string url in new[] { page.Replace("v.redd.it", "v.redd.it.evil.example", StringComparison.Ordinal), page.Replace("https:", "http:", StringComparison.Ordinal), page.Replace("v.redd.it", "user:secret@v.redd.it", StringComparison.Ordinal), page.Replace("v.redd.it", "v.redd.it:8443", StringComparison.Ordinal), page.Replace("HLSPlaylist", "CMAF_720", StringComparison.Ordinal) })
            Check(RedditMediaMetadata.ProbeFormats(data, url).Length == 0, "Only the selected public Reddit master qualifies");
        audio["url"] = "https://v.redd.it/neighbor123/CMAF_AUDIO_128.m3u8";
        Check(RedditMediaMetadata.ProbeFormats(data, page).Length == 0, "Another Reddit asset cannot supply the audio");
        audio["url"] = "https://v.redd.it/selected123/CMAF_AUDIO_128.m3u8";
        audio["has_drm"] = true;
        Check(RedditMediaMetadata.ProbeFormats(data, page).Length == 0, "Protected Reddit audio is not probed");
        audio.Remove("has_drm");
        DirectMediaMetadata.ApplyFormat(data, audio, new Dictionary<string, object> { ["streams"] = new[] {
            new Dictionary<string, object> { ["codec_type"] = "audio", ["codec_name"] = "aac", ["bit_rate"] = 128000 }
        } });
        Check(AvailableFormats.HasAudio(data) && AvailableFormats.Read(data, VideoContainer.Mp4, RedditMediaMetadata.AudioFormat(data, page)).Single().Selector == "720+audio", "Measured Reddit audio is merged into every video quality");
        Check(RedditMediaMetadata.ProbeFormats(data, page).Length == 0, "Complete Reddit audio facts avoid redundant probing");
        var opus = new Dictionary<string, object>(audio) { ["format_id"] = "opus", ["acodec"] = "opus", ["abr"] = 64 };
        data["formats"] = new[] { audio, video, opus };
        Check(RedditMediaMetadata.AudioFormat(data, page, VideoContainer.WebM) == "opus" && RedditMediaMetadata.AudioFormat(data, page) == "audio", "Measured audio selection respects the requested container");
    }
    private static void Check(bool condition, string text) { if (!condition) throw new Exception(text); Console.WriteLine("PASS: " + text); }
}
