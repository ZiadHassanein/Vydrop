using YouTubeDownloader;

internal static class OutputMediaLiveChecks
{
    internal static async Task<int> Run(string output)
    {
        try
        {
            string root = Path.GetFullPath(output), folder = Path.Combine(root, "OutputMediaPreview27");
            Directory.CreateDirectory(folder);
            using var dependencies = new DependencyManager(Path.Combine(root, "Tools"), Array.Empty<string>());
            var tools = dependencies.Discover(CancellationToken.None) ?? throw new IOException("Prepare the isolated tool cache first.");
            using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(5));
            using var runner = new WindowsProcessRunner();
            string[] baseArguments = { "-nostdin", "-hide_banner", "-loglevel", "error", "-y" };
            void Encode(params string[] args)
            {
                if (runner.Run(tools.FFmpeg, baseArguments.Concat(args).ToArray(), null, Console.Error.WriteLine, deadline.Token, TimeSpan.FromSeconds(30)) != 0)
                    throw new IOException("Synthetic media encoding failed.");
            }
            await AudioArtworkChecks.Run(Path.Combine(folder, "Artwork"), tools);
            string mp4 = Path.Combine(folder, "synthetic.mp4"), mkv = Path.Combine(folder, "synthetic.mkv"), webm = Path.Combine(folder, "synthetic.webm");
            Encode("-f", "lavfi", "-i", "testsrc2=duration=0.6:size=320x180:rate=12", "-f", "lavfi", "-i", "sine=frequency=440:duration=0.6", "-shortest", "-c:v", "libx264", "-preset", "ultrafast", "-c:a", "aac", mp4);
            Encode("-i", mp4, "-c", "copy", mkv);
            Encode("-i", mp4, "-c:v", "libvpx-vp9", "-deadline", "realtime", "-cpu-used", "8", "-c:a", "libopus", webm);
            foreach (var (container, file) in new[] { (VideoContainer.Mp4, mp4), (VideoContainer.Mkv, mkv), (VideoContainer.WebM, webm) })
            {
                MediaValidator.Validate(file, new() { VideoContainer = container }, tools, deadline.Token);
                Console.WriteLine("LIVE PASS: FFprobe verified actual synthetic " + container + " video output.");
            }
            using var service = new DownloadService(new YtDlpProvider(tools), new LocalPlaylistStorage(Path.Combine(folder, "PlaylistState")));
            const string url = "https://youtu.be/jNQXAC9IVRw";
            var metadata = await service.InspectAsync(url, null, deadline.Token);
            foreach (var container in Enum.GetValues<VideoContainer>())
            {
                var quality = metadata.QualitiesFor(container).OrderBy(choice => choice.Resolution).FirstOrDefault();
                if (quality == null) { Console.WriteLine("LIVE: " + container + " correctly unavailable for this source."); continue; }
                var result = await service.DownloadAsync(new() { Url = url, Folder = Path.Combine(folder, "YouTube"), VideoContainer = container, Height = quality.Resolution, Selection = quality.Selection }, null, deadline.Token);
                if (result.Cancelled || !File.Exists(result.FilePath)) throw new IOException("Live container output was not completed.");
                Console.WriteLine($"LIVE PASS: Anonymous YouTube {container}/{quality.Resolution}p download passed the real adapter and output validation ({new FileInfo(result.FilePath).Length}bytes).");
            }
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine("LIVE FAIL: " + error); return 1; }
    }
}
