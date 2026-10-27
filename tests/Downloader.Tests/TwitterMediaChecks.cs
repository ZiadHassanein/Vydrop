using YouTubeDownloader;

internal static class TwitterMediaChecks
{
    internal static void Run()
    {
        const string page = "https://x.com/example/status/1790637656616943991";
        static Dictionary<string, object> Format(string url) => new() { ["format_id"] = "http-720", ["url"] = url, ["height"] = 720, ["width"] = 1280, ["vcodec"] = "avc1", ["ext"] = "mp4", ["protocol"] = "https" };
        static Dictionary<string, object> Data(params Dictionary<string, object>[] formats) => new() { ["extractor_key"] = "Twitter", ["formats"] = formats };
        var format = Format("https://video.twimg.com/amplify_video/fixture/vid/720x1280/main.mp4");
        Check(TwitterMediaMetadata.ProbeFormats(Data(format), page).Single() == format, "X progressive media with missing audio metadata is measured");
        foreach (string invalid in new[] { page.Replace("x.com", "x.com.evil.example"), page.Replace("https:", "http:"), page.Replace("x.com", "user:secret@x.com"), page.Replace("x.com", "x.com:444"), "https://x.com/home" })
            Check(TwitterMediaMetadata.ProbeFormats(Data(format), invalid).Length == 0, "X probing requires a public HTTPS post");
        foreach (string invalid in new[] { "https://video.twimg.com.evil.example/file.mp4", "http://video.twimg.com/file.mp4", "https://127.0.0.1/file.mp4", "https://user:secret@video.twimg.com/file.mp4", "https://video.twimg.com:444/file.mp4", "https://video.twimg.com/file.m3u8" })
            Check(TwitterMediaMetadata.ProbeFormats(Data(Format(invalid)), page).Length == 0, "X probing rejects unrelated, credentialed or nonprogressive resources");
        format["acodec"] = "aac";
        Check(TwitterMediaMetadata.ProbeFormats(Data(format), page).Length == 0, "Complete X metadata avoids an extra probe");
        format.Remove("acodec"); var data = Data(format); data["has_drm"] = true;
        Check(TwitterMediaMetadata.ProbeFormats(data, page).Length == 0, "Protected X media is not probed");
        Check(TwitterMediaMetadata.ProbeFormats(Data(Enumerable.Range(0, 20).Select(i => Format("https://video.twimg.com/" + i + ".mp4")).ToArray()), page).Length == 6, "X probing is bounded to six declared renditions");
        var facts = new Dictionary<string, object> { ["streams"] = new[] { new Dictionary<string, object> { ["codec_type"] = "video", ["codec_name"] = "h264", ["width"] = 1280, ["height"] = 720 }, new Dictionary<string, object> { ["codec_type"] = "audio", ["codec_name"] = "aac", ["bit_rate"] = 128000 } } };
        data = Data(format); DirectMediaMetadata.ApplyFormat(data, format, facts);
        Check(AvailableFormats.HasAudio(data) && AvailableFormats.Read(data).Count == 1, "Measured X video retains its real audio and output choices");
    }
    private static void Check(bool valid, string message) { if (!valid) throw new Exception(message); Console.WriteLine("PASS: " + message); }
}
