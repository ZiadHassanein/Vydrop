using YouTubeDownloader;

internal static class MediaTitleChecks
{
    internal static async Task Run(string fixtures)
    {
        string root = Path.Combine(AppContext.BaseDirectory, "title-preview-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        foreach (string file in Directory.GetFiles(fixtures)) File.Copy(file, Path.Combine(root, Path.GetFileName(file)));
        foreach (string name in new[] { "yt-dlp.exe", "deno.exe", "ffmpeg.exe", "ffprobe.exe" })
            File.Copy(Path.Combine(fixtures, "FakeDownloader.exe"), Path.Combine(root, name));
        var tools = ToolSet.Validated(Path.Combine(root, "yt-dlp.exe"), Path.Combine(root, "ffmpeg.exe"), Path.Combine(root, "ffprobe.exe"), Path.Combine(root, "deno.exe"));
        var lookup = new MediaTitleLookup(_ => tools);
        Check(await lookup.FindAsync("https://youtu.be/jNQXAC9IVRw", default) == "Public media fixture", "Title previews inspect a single video through the real process adapter");
        Check(await lookup.FindAsync("https://www.youtube.com/playlist?list=PLfixture", default) == "Test playlist", "Title previews recognise a playlist without starting its downloads");
        Check(File.ReadAllLines(Path.Combine(root, "metadata-inspections.txt")).Length == 2 &&
            !Directory.GetFiles(root, "*", SearchOption.AllDirectories).Any(path => OutputProfiles.IsMediaExtension(Path.GetExtension(path))),
            "Title previews issue metadata requests and never create media output");
        Check(await new MediaTitleLookup(_ => null).FindAsync("https://youtu.be/jNQXAC9IVRw", default) == null, "Missing tools leave optional titles unavailable without installing anything");
        bool discovered = false, rejected = false;
        try { await new MediaTitleLookup(_ => { discovered = true; return tools; }).FindAsync("http://127.0.0.1/video", default); }
        catch (ArgumentException) { rejected = true; }
        Check(rejected && !discovered, "Invalid or private links are rejected before discovering or starting tools");
        using var cancel = new CancellationTokenSource();
        cancel.Cancel();
        bool cancelled = false;
        try { await lookup.FindAsync("https://youtu.be/jNQXAC9IVRw", cancel.Token); }
        catch (OperationCanceledException) { cancelled = true; }
        Check(cancelled, "Title lookups preserve cancellation before process startup");
        Console.WriteLine("6 media title checks passed.");
    }

    private static void Check(bool value, string message)
    {
        if (!value) throw new Exception(message);
        Console.WriteLine("PASS: " + message);
    }
}
