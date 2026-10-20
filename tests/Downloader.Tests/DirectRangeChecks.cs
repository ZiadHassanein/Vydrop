using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using YouTubeDownloader;

internal static class DirectRangeChecks
{
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); Console.WriteLine("PASS: " + message); }
    private sealed class InlineProgress(Action<DownloadProgress> report) : IProgress<DownloadProgress> { public void Report(DownloadProgress p) => report(p); }
    private sealed class Server(byte[] bytes, string mode = "normal") : HttpMessageHandler
    {
        internal int Requests;
        internal bool Resumed;
        internal readonly List<(long From, long To)> Ranges = new();
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            var range = request.Headers.Range.Ranges.Single(); long from = range.From.Value, to = range.To.Value;
            lock (Ranges) Ranges.Add((from, to));
            int number = Interlocked.Increment(ref Requests);
            if (mode == "ignored" || mode == "no-validator")
            {
                var fallback = new HttpResponseMessage(mode == "ignored" ? HttpStatusCode.OK : HttpStatusCode.PartialContent) { Content = new ByteArrayContent(new byte[] { bytes[0] }) };
                fallback.Content.Headers.ContentType = new("video/mp4"); fallback.Content.Headers.ContentRange = new(0, 0, bytes.Length); return Task.FromResult(fallback);
            }
            if (from > 0 && to == bytes.Length / 2 - 1) Resumed = true;
            byte[] payload = bytes[(int)from..((int)to + 1)];
            if (mode == "truncated-once" && number == 2) payload = payload[..32768];
            var response = new HttpResponseMessage(HttpStatusCode.PartialContent) { Content = new ByteArrayContent(payload) };
            response.Headers.ETag = new(mode == "changed" && number > 1 ? "\"changed\"" : "\"stable\"");
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("video/mp4");
            response.Content.Headers.ContentRange = new ContentRangeHeaderValue(from, mode == "wrong-range" && number > 1 ? to - 1 : to, bytes.Length);
            if (!(from == 0 && to == 0) && request.Headers.IfRange?.EntityTag?.Tag != "\"stable\"") throw new Exception("Range request omitted its strong validator");
            return Task.FromResult(response);
        }
    }
    internal static async Task Run(string root)
    {
        byte[] bytes = new byte[2 * 1024 * 1024]; new Random(42).NextBytes(bytes);
        DownloadOptions Options(string folder, int workers = 2) => new() { Url = "https://media.example.com/movie.mp4", Folder = folder, JobId = Guid.NewGuid(), StableOutputName = "Fixture", DirectFileAcceleration = true, Connections = workers, Height = 720 };
        foreach (int workers in new[] { 2, 4 })
        {
            var server = new Server(bytes); using var client = new HttpClient(server); using var engine = new DirectRangeDownloader(client);
            var options = Options(Path.Combine(root, "range-" + workers), workers);
            string file = await engine.TryDownloadAsync(options, null, CancellationToken.None);
            Check(file != null && SHA256.HashData(File.ReadAllBytes(file)).SequenceEqual(SHA256.HashData(bytes)) && server.Requests == workers + 1, "Parallel ranges assemble exact original bytes using strong validators with " + workers + " workers");
            string original = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file)));
            Check(await engine.TryDownloadAsync(options, null, CancellationToken.None) == null && original == Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file))), "Existing completed files are never overwritten by the range engine");
        }
        foreach (string mode in new[] { "ignored", "no-validator" })
        {
            using var client = new HttpClient(new Server(bytes, mode)); using var engine = new DirectRangeDownloader(client);
            var options = Options(Path.Combine(root, mode));
            Check(await engine.TryDownloadAsync(options, null, CancellationToken.None) == null && !Directory.Exists(options.Folder), "Unsupported range server falls back before creating partial files: " + mode);
        }
        foreach (string mode in new[] { "changed", "wrong-range" })
        {
            using var client = new HttpClient(new Server(bytes, mode)); using var engine = new DirectRangeDownloader(client);
            var options = Options(Path.Combine(root, mode)); bool rejected = false;
            try { await engine.TryDownloadAsync(options, null, CancellationToken.None); } catch (InvalidDataException) { rejected = true; }
            Check(rejected && Directory.GetFiles(options.Folder, "*.mp4").Length == 0, "Unsafe range responses never promote a final file: " + mode);
        }
        var retryServer = new Server(bytes, "truncated-once"); using (var client = new HttpClient(retryServer)) using (var engine = new DirectRangeDownloader(client))
        {
            string file = await engine.TryDownloadAsync(Options(Path.Combine(root, "retry")), null, CancellationToken.None);
            Check(retryServer.Resumed && File.ReadAllBytes(file).SequenceEqual(bytes), "A truncated chunk resumes at its actual saved offset without duplicate bytes");
        }
        var resumeServer = new Server(bytes); using (var client = new HttpClient(resumeServer)) using (var engine = new DirectRangeDownloader(client))
        {
            var options = Options(Path.Combine(root, "pause")); using var stop = new CancellationTokenSource(); bool cancelled = false;
            try { await engine.TryDownloadAsync(options, new InlineProgress(_ => stop.Cancel()), stop.Token); } catch (OperationCanceledException) { cancelled = true; }
            long partial = Directory.GetFiles(options.Folder, "*.part", SearchOption.AllDirectories).Sum(f => new FileInfo(f).Length);
            string file = await engine.TryDownloadAsync(options, null, CancellationToken.None);
            Check(cancelled && partial > 0 && File.ReadAllBytes(file).SequenceEqual(bytes), "Cancelled range downloads retain partial bytes and resume with exact output");
        }
        using (var client = new HttpClient(new Server(bytes))) using (var engine = new DirectRangeDownloader(client))
        {
            var options = Options(Path.Combine(root, "invalid-media")); bool rejected = false;
            try { await engine.TryDownloadAsync(options, null, _ => throw new InvalidDataException("Fixture media validation failed"), CancellationToken.None); } catch (InvalidDataException) { rejected = true; }
            Check(rejected && Directory.GetFiles(options.Folder, "*.mp4").Length == 0 && Directory.GetFiles(options.Folder, "*.part", SearchOption.AllDirectories).Length == 2,
                "Media validation failure keeps resumable chunks and never exposes an invalid final file");
        }
        using (var client = new HttpClient(new Server(bytes))) using (var engine = new DirectRangeDownloader(client))
        {
            var options = Options(Path.Combine(root, "limit"), 4); options.BytesPerSecond = bytes.Length;
            var watch = System.Diagnostics.Stopwatch.StartNew();
            await engine.TryDownloadAsync(options, null, CancellationToken.None);
            Check(watch.Elapsed.TotalSeconds >= .85, "Parallel workers share one rate limit rather than multiplying the configured speed");
        }
    }
}
