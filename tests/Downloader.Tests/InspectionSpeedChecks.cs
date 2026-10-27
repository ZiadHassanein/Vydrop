using System.Diagnostics;
using System.Text.Json;
using YouTubeDownloader;

internal static class InspectionSpeedChecks
{
    private static void Check(bool valid, string message) { if (!valid) throw new Exception(message); Console.WriteLine("PASS: " + message); }
    internal static async Task Run(string fixtureDirectory)
    {
        string root = Path.Combine(AppContext.BaseDirectory, "inspection-speed-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
        foreach (string file in Directory.GetFiles(fixtureDirectory)) File.Copy(file, Path.Combine(root, Path.GetFileName(file)));
        foreach (string name in new[] { "yt-dlp.exe", "deno.exe", "ffmpeg.exe", "ffprobe.exe" }) File.Copy(Path.Combine(root, "FakeDownloader.exe"), Path.Combine(root, name));
        var tools = ToolSet.Validated(Path.Combine(root, "yt-dlp.exe"), Path.Combine(root, "ffmpeg.exe"), Path.Combine(root, "ffprobe.exe"), Path.Combine(root, "deno.exe"));
        var updates = new Updates();
        using var provider = new YtDlpProvider(tools, new AutomaticToolUpdates(Path.Combine(root, "updates"), updates));
        using var service = new DownloadService(provider, new LocalPlaylistStorage(Path.Combine(root, "playlists")));
        var now = DateTimeOffset.UtcNow; provider.InspectionClock = () => now;
        const string url = "https://www.youtube.com/watch?v=jNQXAC9IVRw";
        int Count() => File.ReadAllLines(Path.Combine(root, "metadata-inspections.txt")).Length;
        var first = await service.InspectAsync(url, null, default); var selected = first.Qualities[0].Selection;
        first.Title = "Changed by caller"; first.Qualities.Clear(); first.Captions.Clear();
        var second = await service.InspectAsync(url, null, default);
        Check(Count() == 1 && second.Title == "Public media fixture" && second.Qualities.Count == 1 && second.Captions.Count == 1, "Repeated previews reuse one process without sharing mutable caller metadata");
        Check(updates.Checks == 0, "Preview inspection does not wait for routine media-tool maintenance");
        using (var cancel = new CancellationTokenSource()) { cancel.Cancel(); bool cancelled = false; try { await service.InspectAsync(url, null, cancel.Token); } catch (OperationCanceledException) { cancelled = true; } Check(cancelled && Count() == 1, "A cached preview still respects cancellation"); }
        await service.InspectAsync("https://vimeo.com/76979871", null, default); second = await service.InspectAsync(url, null, default);
        Check(Count() == 2 && second.Qualities[0].Selection == selected, "A/B/A preview reuse retains the original media and immutable output choice");
        now = now.AddSeconds(46); await service.InspectAsync(url, null, default);
        Check(Count() == 3, "Expired metadata is fetched again instead of retaining stale source URLs");
        File.WriteAllText(Path.Combine(root, "missing-quality.flag"), "fixture");
        bool rejected = false;
        try { await service.DownloadAsync(new() { Url = url, Folder = Path.Combine(root, "media"), Selection = selected, Height = selected.Resolution }, null, default); } catch (InvalidOperationException) { rejected = true; }
        Check(rejected && updates.Checks == 1 && Count() == 4, "Downloads still run daily maintenance and freshly reject disappearing quality choices");
        Check((await service.InspectAsync(url, null, default)).Qualities[0].Resolution == 720 && Count() == 5, "A failed download evicts its preview so refreshed formats are immediately available");
        File.Delete(Path.Combine(root, "missing-quality.flag"));
        for (int i = 0; i < 9; i++) await service.InspectAsync("https://example.com/video/" + i, null, default);
        int before = Count(); await service.InspectAsync("https://example.com/video/0", null, default);
        Check(Count() == before + 1, "Preview reuse stays bounded to eight sources");
        using var dependencies = new DependencyManager(Path.Combine(root, "native-cache"), new[] { root });
        dependencies.AcquireArtifact = (_, _, _) => throw new Exception("Prepared tools should never be downloaded again.");
        using var native = new YtDlpProvider(dependencies, checkAutomaticUpdates: false);
        var preparation = await BrowserHost.PrepareInspectionAsync(new(2, "inspect", url, "speed"), dependencies, () => throw new Exception("A ready host should not open the app."), default, native.UsePreparedTools);
        Check(preparation == null && native.IsReady, "Native preflight hands its verified tools directly to inspection without a second discovery");
    }
    private sealed class Updates : IToolUpdateClient
    {
        internal int Checks;
        public Task<(ToolSet Tools, MediaToolUpdateResult Result)> RefreshAsync(ToolSet current, IProgress<DownloadProgress> progress, CancellationToken token) { Checks++; return Task.FromResult((current, new MediaToolUpdateResult(false, "fixture"))); }
        public Task<ToolSet> RollbackAsync(ToolSet current, ToolSet fallback, CancellationToken token) => throw new Exception("Unexpected rollback");
    }
    internal static async Task<int> Live(string url)
    {
        using var provider = new YtDlpProvider(new DependencyManager(), checkAutomaticUpdates: false);
        using var service = new DownloadService(provider, new LocalPlaylistStorage());
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(75));
        var timer = Stopwatch.StartNew(); var first = await service.InspectAsync(url, null, deadline.Token); double initial = timer.Elapsed.TotalSeconds;
        timer.Restart(); var repeated = await service.InspectAsync(url, null, deadline.Token);
        Console.WriteLine(JsonSerializer.Serialize(new { firstSeconds = initial, repeatSeconds = timer.Elapsed.TotalSeconds, qualities = first.Qualities.Count, sameQualities = first.Qualities.Select(q=>q.Resolution).SequenceEqual(repeated.Qualities.Select(q=>q.Resolution)), audio = repeated.HasAudio }));
        return 0;
    }
}
