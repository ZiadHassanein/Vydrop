using System.Text;
using System.Text.Json;
using YouTubeDownloader;

internal static class SocialVideoLiveChecks
{
    // Explicit network/download verification in a named test output folder.
    // It uses public media URLs and never reads a browser profile or account.
    internal static async Task<int> Run(string url, string folder)
    {
        using var dependencies = new DependencyManager();
        using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(4));
        var tools = await dependencies.EnsureAsync(null, deadline.Token);
        using var provider = new YtDlpProvider(tools);
        var info = provider.Inspect(url, deadline.Token);
        if (info.Qualities.Count == 0) throw new Exception("The selected video has no measured video formats.");
        var quality = info.Qualities.OrderBy(q => q.Resolution).First();
        var options = new DownloadOptions { Url = url, Folder = Path.GetFullPath(folder), Height = quality.Resolution, Selection = quality.Selection,
            JobId = Guid.NewGuid(), StableOutputName = "Selected social video verification" };
        var saved = provider.DownloadItem(options, deadline.Token);
        if (saved.FilePath == null || !MediaFiles.IsCompleted(options.Folder, saved.FilePath, options)) throw new Exception("The media transfer did not produce a completed file.");
        using var runner = new WindowsProcessRunner();
        var output = new StringBuilder();
        int code = runner.Run(tools.FFprobe, new[] { "-v", "error", "-show_entries", "stream=codec_type,codec_name:format=duration", "-of", "json", saved.FilePath },
            line => output.AppendLine(line), _ => { }, deadline.Token, TimeSpan.FromSeconds(20));
        using var facts = JsonDocument.Parse(output.ToString());
        var streams = facts.RootElement.GetProperty("streams").EnumerateArray().ToArray();
        if (code != 0 || !streams.Any(s => s.GetProperty("codec_type").GetString() == "video") || info.HasAudio && !streams.Any(s => s.GetProperty("codec_type").GetString() == "audio"))
            throw new Exception("The downloaded file is missing video or its expected audio.");
        Console.WriteLine(JsonSerializer.Serialize(new { result = "PASS", source = new Uri(url).Host, qualities = info.Qualities.Select(q => q.Resolution),
            hasAudio = info.HasAudio, file = saved.FilePath, bytes = new FileInfo(saved.FilePath).Length, measured = facts.RootElement }));
        return 0;
    }
}
