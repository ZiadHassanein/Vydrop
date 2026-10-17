using System.Text.Json;
using YouTubeDownloader;

internal static class LinkedInLiveChecks
{
    // Explicit opt-in network check. No browser profile, account or cookies are used.
    internal static int Run(string toolsRoot, string output, string url, bool allOutputs = false)
    {
        toolsRoot = Path.GetFullPath(toolsRoot);
        var tools = ToolSet.Validated(Path.Combine(toolsRoot, "yt-dlp.exe"), Path.Combine(toolsRoot, "ffmpeg.exe"),
            Path.Combine(toolsRoot, "ffprobe.exe"), Path.Combine(toolsRoot, "deno.exe"));
        using var provider = new YtDlpProvider(tools);
        using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        var info = provider.Inspect(url, deadline.Token);
        if (info.Qualities.Count == 0 || !info.HasAudio || info.Duration <= 0 || info.AudioSource?.HasBitrate != true)
            throw new Exception("LinkedIn inspection did not produce measured video/audio source facts.");
        var quality = info.Qualities.OrderBy(q => q.Resolution).First();
        var options = new DownloadOptions { Url = url, Folder = Path.GetFullPath(output), Height = quality.Resolution, Selection = quality.Selection };
        var result = provider.DownloadItem(options, deadline.Token);
        if (result.FilePath == null || !MediaFiles.IsCompleted(options.Folder, result.FilePath, options))
            throw new Exception("The LinkedIn download did not pass completed-file validation.");
        var files = new List<string> { result.FilePath };
        if (allOutputs)
        {
            var mkvQuality = info.QualitiesFor(VideoContainer.Mkv).OrderBy(q => q.Resolution).First();
            var mkv = new DownloadOptions { Url = url, Folder = options.Folder, VideoContainer = VideoContainer.Mkv, Height = mkvQuality.Resolution, Selection = mkvQuality.Selection };
            var savedMkv = provider.DownloadItem(mkv, deadline.Token);
            if (savedMkv.FilePath == null || !savedMkv.FilePath.EndsWith(".mkv", StringComparison.OrdinalIgnoreCase) || !MediaFiles.IsCompleted(mkv.Folder, savedMkv.FilePath, mkv))
                throw new Exception("The browser media source did not produce a verified MKV.");
            files.Add(savedMkv.FilePath);
            var audio = new DownloadOptions { Url = url, Folder = options.Folder, Audio = true, AudioEncoding = AudioEncoding.M4a, AudioBitrate = 0 };
            var savedAudio = provider.DownloadItem(audio, deadline.Token);
            if (savedAudio.FilePath == null || !savedAudio.FilePath.EndsWith(".m4a", StringComparison.OrdinalIgnoreCase) || !MediaFiles.IsCompleted(audio.Folder, savedAudio.FilePath, audio))
                throw new Exception("The browser media source did not produce verified M4A audio.");
            files.Add(savedAudio.FilePath);
        }
        Console.WriteLine(JsonSerializer.Serialize(new { result = "PASS", normalizedUrl = provider.Normalize(url, false), resolutions = info.Qualities.Select(q => q.Resolution),
            audioKbps = info.AudioSource.BitrateKbps, seconds = info.Duration, bytes = new FileInfo(result.FilePath).Length, file = result.FilePath, files }));
        return 0;
    }
}
