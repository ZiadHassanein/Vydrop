using System.Diagnostics;
using System.IO.Pipes;
using YouTubeDownloader;

internal static class NativeHostChecks
{
    internal static async Task<int> Run(string executable)
    {
        // Never forward a fixture URL into a user's already-running stable app.
        using (var probe = new NamedPipeServerStream(BrowserLinkTransport.PipeName, PipeDirection.InOut, 1,
            PipeTransmissionMode.Byte, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly)) { }
        using var lifetime = new CancellationTokenSource();
        int links = 0;
        bool accepting = true;
        string responseCode = null;
        BrowserRequest lastRequest = null;
        var listener = BrowserLinkTransport.ListenRequestsAsync(request => { lastRequest = request; Interlocked.Increment(ref links); return new(accepting, request.Type == "download" ? "started" : request.Type == "enqueue" ? "queued" : "opened"); }, lifetime.Token);
        async Task<bool> Request(string origin)
        {
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var start = new ProcessStartInfo(Path.GetFullPath(executable)) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true };
            start.ArgumentList.Add(origin);
            start.ArgumentList.Add("--parent-window=0");
            using var process = Process.Start(start);
            try
            {
                await BrowserProtocol.WriteAsync(process.StandardInput.BaseStream, new { version = 1, type = "open-link", url = "https://example.com/public-native-fixture" }, deadline.Token);
                process.StandardInput.Close();
                using var response = await BrowserProtocol.ReadAsync(process.StandardOutput.BaseStream, deadline.Token);
                responseCode = response.RootElement.TryGetProperty("code", out var code) ? code.GetString() : null;
                await process.WaitForExitAsync(deadline.Token);
                return response.RootElement.GetProperty("ok").GetBoolean();
            }
            finally { if (!process.HasExited) { process.Kill(true); await process.WaitForExitAsync(); } }
        }
        async Task ModernRequests()
        {
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var start = new ProcessStartInfo(Path.GetFullPath(executable)) { UseShellExecute = false, CreateNoWindow = true, RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true };
            start.ArgumentList.Add("chrome-extension://" + BrowserIdentity.ChromiumId + "/");
            start.ArgumentList.Add("--parent-window=0");
            using var process = Process.Start(start);
            try
            {
                foreach (string id in new[] { "framed_first", "framed_second" })
                {
                    await BrowserProtocol.WriteAsync(process.StandardInput.BaseStream, new { version = 2, type = "enqueue", requestId = id, url = "https://example.com/public-native-fixture", sourcePage = "https://example.com/post", mediaId = "asset" }, deadline.Token);
                    using var response = await BrowserProtocol.ReadAsync(process.StandardOutput.BaseStream, deadline.Token);
                    if (!response.RootElement.GetProperty("ok").GetBoolean() || response.RootElement.GetProperty("requestId").GetString() != id || response.RootElement.GetProperty("code").GetString() != "queued" || lastRequest is not { SourcePage: "https://example.com/post", MediaId: "asset" })
                        throw new Exception("Packaged native host lost a v2 request identity or source context.");
                }
                await BrowserProtocol.WriteAsync(process.StandardInput.BaseStream, new { version = 2, type = "ping", requestId = "framed_health" }, deadline.Token);
                using (var health = await BrowserProtocol.ReadAsync(process.StandardOutput.BaseStream, deadline.Token))
                    if (!health.RootElement.GetProperty("capabilities").EnumerateArray().Any(v => v.GetString() == "start-download")) throw new Exception("The packaged host does not advertise confirmed starts.");
                await BrowserProtocol.WriteAsync(process.StandardInput.BaseStream, new { version = 2, type = "download", requestId = "framed_download", url = "https://example.com/public-native-fixture", output = new { kind = "video", format = "webm", height = 720 } }, deadline.Token);
                using (var started = await BrowserProtocol.ReadAsync(process.StandardOutput.BaseStream, deadline.Token))
                    if (started.RootElement.GetProperty("code").GetString() != "started" || lastRequest.Type != "download" || lastRequest.Output != new BrowserOutputChoice("video", "webm", 720)) throw new Exception("The packaged host lost the confirmed start/output choice.");
                process.StandardInput.Close();
                await process.WaitForExitAsync(deadline.Token);
            }
            finally { if (!process.HasExited) { process.Kill(true); await process.WaitForExitAsync(); } }
        }
        try
        {
            if (!await Request("chrome-extension://" + BrowserIdentity.ChromiumId + "/") || links != 1) throw new Exception("Packaged native host did not deliver its framed link through the current-user pipe.");
            accepting = false;
            var clock = Stopwatch.StartNew();
            if (await Request("chrome-extension://" + BrowserIdentity.ChromiumId + "/") || links != 2 || responseCode != "busy" || clock.Elapsed > TimeSpan.FromSeconds(3))
                throw new Exception("Packaged native host did not report an immediate busy rejection.");
            if (await Request("chrome-extension://aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa/") || links != 2) throw new Exception("Packaged native host accepted an unapproved origin.");
            accepting = true;
            await ModernRequests();
            if (links != 6) throw new Exception("Packaged native host lost a persistent-port request.");
            Console.WriteLine("PASS: Packaged GUI EXE handles native stdin/stdout framing, current-user delivery and origin rejection without a visible window or download.");
            Console.WriteLine("PASS: Packaged GUI EXE serves multiple v2 queue messages on one port with request identities and source context.");
            Console.WriteLine("PASS: Packaged host advertises confirmed starts and carries exact output choices over real native frames.");
            return 0;
        }
        finally { lifetime.Cancel(); await listener; }
    }
}
