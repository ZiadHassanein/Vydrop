using System.Globalization;
using System.Text;
using System.Text.Json;
using YouTubeDownloader;

internal static class AudioArtworkChecks
{
    internal static async Task<int> Run(string folder, ToolSet prepared = null)
    {
        folder = Path.GetFullPath(folder); Directory.CreateDirectory(folder);
        using var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        using var dependencies = new DependencyManager();
        var tools = prepared ?? await dependencies.EnsureAsync(null, deadline.Token);
        using var runner = new WindowsProcessRunner();
        string Execute(string executable, IEnumerable<string> args)
        {
            var text = new StringBuilder(); var errors = new StringBuilder();
            int code = runner.Run(executable, args.ToArray(), line => text.AppendLine(line), line => errors.AppendLine(line), deadline.Token, TimeSpan.FromSeconds(45));
            if (code != 0) throw new Exception(errors.ToString());
            return text.ToString();
        }
        string cover = Path.Combine(folder, "fixture-cover.jpg");
        Execute(tools.FFmpeg, ["-nostdin", "-y", "-f", "lavfi", "-i", "color=c=red:s=160x160", "-frames:v", "1", cover]);
        foreach (AudioEncoding encoding in new[] { AudioEncoding.Mp3, AudioEncoding.M4a, AudioEncoding.Opus, AudioEncoding.Flac })
        {
            string ext = OutputProfiles.AudioExtension(encoding), codec = encoding switch { AudioEncoding.Mp3 => "mp3", AudioEncoding.M4a => "aac", AudioEncoding.Opus => "opus", _ => "flac" };
            string encoder = encoding switch { AudioEncoding.Mp3 => "libmp3lame", AudioEncoding.M4a => "aac", AudioEncoding.Opus => "libopus", _ => "flac" };
            string source = Path.Combine(folder, "fixture-source." + ext);
            var generation = new List<string> { "-nostdin", "-y", "-f", "lavfi", "-i", "sine=frequency=440:duration=3", "-c:a", encoder };
            if (encoding != AudioEncoding.Flac) generation.AddRange(["-b:a", "192k"]);
            generation.Add(source); Execute(tools.FFmpeg, generation);
            string info = Path.Combine(folder, "fixture-" + ext + ".json");
            File.WriteAllText(info, JsonSerializer.Serialize(new { id = "fixture-" + ext, title = "Artwork fixture " + ext, uploader = "Vydrop local test", extractor = "generic",
                webpage_url = "https://example.com/artwork-fixture", thumbnail = new Uri(cover).AbsoluteUri,
                formats = new[] { new { format_id = "source", url = new Uri(source).AbsoluteUri, ext, vcodec = "none", acodec = codec, abr = 192 } } }));
            var options = new DownloadOptions { Url = "https://example.com/artwork-fixture", Folder = folder, Audio = true, AudioEncoding = encoding,
                AudioBitrate = encoding == AudioEncoding.Flac ? 0 : 64, AudioSource = new(codec, 192) };
            var args = Arguments.ForDownload(tools, options);
            args.RemoveRange(args.Count - 2, 2);
            // Only these generated fixtures enable local-file URLs. Production arguments never do.
            args.InsertRange(0, ["--enable-file-urls", "--load-info-json", info]);
            string output = Execute(tools.YtDlp, args);
            File.WriteAllText(Path.Combine(folder, "result-" + ext + ".log"), output);
            string saved = output.Split('\n').Where(line => line.StartsWith("YTDL_FILE:", StringComparison.Ordinal)).Select(line => JsonSerializer.Deserialize<string>(line[10..].Trim())).Last();
            using var measured = JsonDocument.Parse(Execute(tools.FFprobe, ["-v", "error", "-show_streams", "-show_format", "-of", "json", saved]));
            var streams = measured.RootElement.GetProperty("streams").EnumerateArray().ToArray();
            if (!streams.Any(s => s.GetProperty("codec_type").GetString() == "audio") ||
                !streams.Any(s => s.TryGetProperty("disposition", out var d) && d.TryGetProperty("attached_pic", out var pic) && pic.GetInt32() == 1))
                throw new Exception("Audio output lost its embedded cover: " + ext);
            if (encoding != AudioEncoding.Flac)
            {
                using var packets = JsonDocument.Parse(Execute(tools.FFprobe, ["-v", "error", "-select_streams", "a", "-show_packets", "-show_entries", "packet=size,duration_time", "-of", "json", saved]));
                var rows = packets.RootElement.GetProperty("packets").EnumerateArray().ToArray();
                double bits = rows.Sum(p => double.Parse(p.GetProperty("size").GetString(), CultureInfo.InvariantCulture)) * 8;
                double duration = rows.Sum(p => p.TryGetProperty("duration_time", out var value) ? double.Parse(value.GetString(), CultureInfo.InvariantCulture) : 0);
                if (duration <= 0 || bits / duration >= 128000) throw new Exception("Matching-codec conversion did not lower the bitrate: " + ext);
            }
            if (!MediaFiles.IsCompleted(folder, saved, options)) throw new Exception("The covered audio was not accepted as a completed audio file.");
            Console.WriteLine(JsonSerializer.Serialize(new { result = "PASS", format = ext, embeddedCover = true, lowerBitrate = encoding != AudioEncoding.Flac, file = saved }));
        }
        return 0;
    }
}
