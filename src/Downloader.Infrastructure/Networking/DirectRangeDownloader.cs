using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

namespace YouTubeDownloader;

// Direct progressive files only. HLS/DASH fragment concurrency remains owned by yt-dlp.
internal sealed class DirectRangeDownloader : IDisposable
{
    private readonly HttpClient client;
    private readonly bool ownsClient;
    internal DirectRangeDownloader(HttpClient client = null)
    {
        ownsClient = client == null;
        this.client = client ?? new HttpClient(new SocketsHttpHandler { AllowAutoRedirect = false, UseCookies = false, AutomaticDecompression = DecompressionMethods.None, ConnectTimeout = TimeSpan.FromSeconds(20) }) { Timeout = Timeout.InfiniteTimeSpan };
    }
    private async Task<HttpResponseMessage> RequestAsync(string url, long start, long end, string etag, CancellationToken token)
    {
        for (int redirects = 0; redirects <= 5; redirects++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, MediaLink.PublicUrl(url));
            request.Headers.Range = new RangeHeaderValue(start, end);
            request.Headers.AcceptEncoding.ParseAdd("identity");
            if (etag != null) { request.Headers.IfRange = new RangeConditionHeaderValue(new EntityTagHeaderValue(etag)); request.Headers.IfMatch.Add(new EntityTagHeaderValue(etag)); }
            using var headersDeadline = CancellationTokenSource.CreateLinkedTokenSource(token);
            headersDeadline.CancelAfter(TimeSpan.FromSeconds(20));
            HttpResponseMessage response;
            try { response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, headersDeadline.Token).ConfigureAwait(false); }
            catch (OperationCanceledException) when (!token.IsCancellationRequested) { throw new HttpRequestException("The media server did not send response headers in time."); }
            if ((int)response.StatusCode is not (301 or 302 or 303 or 307 or 308)) return response;
            var location = response.Headers.Location;
            response.Dispose();
            if (location == null) throw new HttpRequestException("The media server returned an invalid redirect.");
            url = new Uri(new Uri(url), location).AbsoluteUri;
        }
        throw new HttpRequestException("The media server redirected too many times.");
    }
    internal Task<string> TryDownloadAsync(DownloadOptions options, IProgress<DownloadProgress> progress, CancellationToken token) => TryDownloadAsync(options, progress, null, token);
    internal async Task<string> TryDownloadAsync(DownloadOptions options, IProgress<DownloadProgress> progress, Action<string> validate, CancellationToken token)
    {
        if (!options.DirectFileAcceleration || options.Connections is not (2 or 4 or 8) || options.JobId == Guid.Empty || options.Audio || options.PlaylistItem || options.Captions != null || options.VideoContainer != VideoContainer.Mp4 || !new Uri(options.Url).AbsolutePath.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase)) return null;
        using var probeTimeout = CancellationTokenSource.CreateLinkedTokenSource(token); probeTimeout.CancelAfter(TimeSpan.FromSeconds(20));
        using var probe = await RequestAsync(options.Url, 0, 0, null, probeTimeout.Token).ConfigureAwait(false);
        var range = probe.Content.Headers.ContentRange;
        string etag = probe.Headers.ETag is { IsWeak: false } validator ? validator.Tag : null;
        long size = range?.Length ?? 0;
        // Refuse multipart, compressed, changing or unvalidated objects. Ordinary downloads use the existing engine.
        if (probe.StatusCode != HttpStatusCode.PartialContent || range?.Unit != "bytes" || range.From != 0 || range.To != 0 || size < 1024 * 1024 || size > 1024L * 1024 * 1024 * 1024 || etag == null ||
            probe.Content.Headers.ContentType?.MediaType != "video/mp4" || probe.Content.Headers.ContentEncoding.Any(e => e != "identity")) return null;
        if (probe.Content.Headers.ContentLength is { } probeLength && probeLength != 1) return null;
        using (var body = await probe.Content.ReadAsStreamAsync(probeTimeout.Token).ConfigureAwait(false))
        {
            byte[] one = new byte[1];
            if (await body.ReadAsync(one, probeTimeout.Token).ConfigureAwait(false) != 1 || await body.ReadAsync(one, probeTimeout.Token).ConfigureAwait(false) != 0) return null;
        }
        int workers = options.Connections;
        string validatorId = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(etag + "\n" + size.ToString(System.Globalization.CultureInfo.InvariantCulture))))[..24];
        string folder = Path.GetFullPath(options.Folder);
        string stage = Path.Combine(folder, ".vydrop-partials", options.JobId.ToString("N"), options.PartialGeneration.ToString(System.Globalization.CultureInfo.InvariantCulture), "ranges-" + validatorId + "-" + workers);
        CreateOwnedPath(folder, stage);
        string profile = options.Height > 0 ? options.Height.ToString(System.Globalization.CultureInfo.InvariantCulture) + "p" : "best";
        string target = Path.Combine(folder, Arguments.StableTitle(options.StableOutputName) + " [" + options.JobId.ToString("N")[..12] + "] - " + profile + ".mp4");
        if (File.Exists(target)) return null;
        long downloaded = 0;
        for (int i = 0; i < workers; i++)
        {
            string part = Path.Combine(stage, i + ".part");
            RejectRedirectedFile(part);
            long length = File.Exists(part) ? new FileInfo(part).Length : 0;
            long expected = (i + 1) * size / workers - i * size / workers;
            if (length > expected) throw new InvalidDataException("A partial download is inconsistent. Refresh the source to restart safely.");
            downloaded += length;
        }
        var drive = new DriveInfo(Path.GetPathRoot(folder));
        if (drive.AvailableFreeSpace < (size - downloaded) + size + 16 * 1024 * 1024) throw new IOException("There is not enough free space to assemble this download.");
        var limiter = new SharedRateLimit(options.BytesPerSecond);
        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(token);
        async Task Transfer(int index)
        {
            string part = Path.Combine(stage, index + ".part");
            long first = index * size / workers, last = (index + 1) * size / workers - 1;
            try
            {
                for (int attempt = 0; ; attempt++)
                {
                    long present = File.Exists(part) ? new FileInfo(part).Length : 0;
                    if (present == last - first + 1) return;
                    try
                    {
                        using var response = await RequestAsync(options.Url, first + present, last, etag, lifetime.Token).ConfigureAwait(false);
                        if (response.StatusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.ServiceUnavailable)
                        {
                            if (attempt >= 2) response.EnsureSuccessStatusCode();
                            TimeSpan delay = response.Headers.RetryAfter?.Delta ?? (response.Headers.RetryAfter?.Date - DateTimeOffset.UtcNow) ?? TimeSpan.FromSeconds(1 << attempt);
                            await Task.Delay(TimeSpan.FromSeconds(Math.Clamp(delay.TotalSeconds, 1, 15)), lifetime.Token).ConfigureAwait(false); continue;
                        }
                        var received = response.Content.Headers.ContentRange;
                        if (response.StatusCode != HttpStatusCode.PartialContent || received?.From != first + present || received.To != last || received.Length != size || received.Unit != "bytes" ||
                            response.Headers.ETag?.Tag != etag || response.Headers.ETag.IsWeak || response.Content.Headers.ContentEncoding.Any(e => e != "identity"))
                            throw new InvalidDataException("The media changed or the server stopped supporting safe resume. Refresh the source.");
                        using var stream = await response.Content.ReadAsStreamAsync(lifetime.Token).ConfigureAwait(false);
                        using var file = new FileStream(part, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, 65536, true);
                        file.Position = present;
                        byte[] buffer = new byte[65536];
                        while (true)
                        {
                            using var readTimeout = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token); readTimeout.CancelAfter(TimeSpan.FromSeconds(20));
                            int count;
                            try { count = await stream.ReadAsync(buffer, readTimeout.Token).ConfigureAwait(false); }
                            catch (OperationCanceledException) when (!lifetime.IsCancellationRequested) { throw new HttpRequestException("The media connection timed out."); }
                            if (count == 0) break;
                            if (file.Position + count > last - first + 1) throw new InvalidDataException("The media server returned an unexpected range length.");
                            await limiter.WaitAsync(count, lifetime.Token).ConfigureAwait(false);
                            await file.WriteAsync(buffer.AsMemory(0, count), lifetime.Token).ConfigureAwait(false);
                            long current = Interlocked.Add(ref downloaded, count);
                            progress?.Report(new DownloadProgress { Stage = "Downloading", Percent = Math.Min(99, 100.0 * current / size), Detail = YtDlpProvider.Size(current) + " / " + YtDlpProvider.Size(size), DownloadedBytes = current, TotalBytes = size });
                        }
                        await file.FlushAsync(lifetime.Token).ConfigureAwait(false);
                        if (file.Length != last - first + 1) throw new HttpRequestException("The media connection was interrupted.");
                        return;
                    }
                    catch (HttpRequestException) when (attempt < 2)
                    { await Task.Delay(TimeSpan.FromSeconds(1 << attempt), lifetime.Token).ConfigureAwait(false); }
                }
            }
            catch { lifetime.Cancel(); throw; }
        }
        await Task.WhenAll(Enumerable.Range(0, workers).Select(Transfer)).ConfigureAwait(false);
        string assembled = Path.Combine(stage, "assembled.mp4");
        RejectRedirectedFile(assembled);
        try
        {
            using (var output = new FileStream(assembled, FileMode.Create, FileAccess.Write, FileShare.None, 65536, true))
            {
                for (int i = 0; i < workers; i++)
                {
                    using var input = new FileStream(Path.Combine(stage, i + ".part"), FileMode.Open, FileAccess.Read, FileShare.Read, 65536, true);
                    await input.CopyToAsync(output, token).ConfigureAwait(false);
                }
                await output.FlushAsync(token).ConfigureAwait(false);
                if (output.Length != size) throw new InvalidDataException("The completed media size is inconsistent.");
            }
            token.ThrowIfCancellationRequested();
            validate?.Invoke(assembled);
            File.Move(assembled, target, false);
            for (int i = 0; i < workers; i++) File.Delete(Path.Combine(stage, i + ".part"));
            return target;
        }
        finally { if (File.Exists(assembled)) File.Delete(assembled); }
    }
    private static void RejectRedirectedFile(string path)
    {
        if (File.Exists(path) && (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0) throw new IOException("The partial file is redirected outside its download.");
    }
    private static void CreateOwnedPath(string root, string path)
    {
        if (!Path.GetFullPath(path).StartsWith(Path.GetFullPath(root).TrimEnd('\\') + "\\", StringComparison.OrdinalIgnoreCase)) throw new IOException("The partial folder is outside the save folder.");
        string current = root;
        foreach (string segment in Path.GetRelativePath(root, path).Split(Path.DirectorySeparatorChar))
        {
            current = Path.Combine(current, segment);
            Directory.CreateDirectory(current);
            if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0) throw new IOException("The partial folder contains a redirected directory.");
        }
    }
    public void Dispose() { if (ownsClient) client.Dispose(); }
    private sealed class SharedRateLimit(long bytesPerSecond)
    {
        private readonly object gate = new();
        private readonly Stopwatch watch = Stopwatch.StartNew();
        private double due;
        public Task WaitAsync(int count, CancellationToken token)
        {
            if (bytesPerSecond == 0) return Task.CompletedTask;
            double delay;
            lock (gate) { due = Math.Max(due, watch.Elapsed.TotalSeconds) + (double)count / bytesPerSecond; delay = due - watch.Elapsed.TotalSeconds; }
            return Task.Delay(TimeSpan.FromSeconds(Math.Max(0, delay)), token);
        }
    }
}
