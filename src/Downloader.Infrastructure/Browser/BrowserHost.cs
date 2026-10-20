using System.Diagnostics;
using static YouTubeDownloader.BrowserIdentity;

namespace YouTubeDownloader;
public static class BrowserHost
{
    public static bool IsInvocation(string[] args) => args.Length > 0 && (args[0].StartsWith("chrome-extension://", StringComparison.Ordinal) || args[0].EndsWith(HostName + ".firefox.json", StringComparison.OrdinalIgnoreCase));
    public static bool IsAllowedCaller(string[] args)
    {
        // Browser origin filtering; command-line arguments do not authenticate local processes.
        if (args.Length is < 1 or > 3) return false;
        if (ChromiumIds.Any(id => args[0] == "chrome-extension://" + id + "/"))
            return args.Skip(1).All(a => a.StartsWith("--parent-window=", StringComparison.Ordinal) && ulong.TryParse(a[16..], out _));
        return args.Length == 2 && args[0].EndsWith(HostName + ".firefox.json", StringComparison.OrdinalIgnoreCase) && args[1] == FirefoxId;
    }
    public static async Task RunAsync(string[] args)
    {
        var input = Console.OpenStandardInput(); var output = Console.OpenStandardOutput();
        // Short-lived ports support several explicit actions; idle hosts exit without starting the desktop UI.
        for (int count = 0; count < 100; count++)
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));
            object response; string requestId = null;
            try
            {
                if (!IsAllowedCaller(args)) throw new InvalidDataException("This browser extension is not authorized.");
                using var message = await BrowserProtocol.ReadAsync(input, timeout.Token).ConfigureAwait(false);
                var request = BrowserProtocol.ReadRequest(message.RootElement); requestId = request.RequestId;
                if (request.Type == "ping")
                {
                    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                    var health = await BrowserLinkTransport.TrySendRequestAsync(request, 500, deadline.Token).ConfigureAwait(false);
                    if (health?.Accepted == true) BrowserHealth.Record(Environment.ProcessPath);
                    response = new { ok = true, code = "ready", protocol = 2, requestId, appRunning = health?.Accepted == true, capabilities = new[] { "ping", "enqueue", "source-context" } };
                }
                else
                {
                    timeout.CancelAfter(TimeSpan.FromSeconds(15));
                    BrowserReceipt receipt;
                    if (request.Version == 1)
                    {
                        var legacy = await DeliverAsync(request.Url, Launch, timeout.Token).ConfigureAwait(false);
                        receipt = new(legacy == BrowserDelivery.Accepted, legacy == BrowserDelivery.Accepted ? "opened" : "busy");
                    }
                    else receipt = await DeliverRequestAsync(request, Launch, timeout.Token).ConfigureAwait(false);
                    response = new { ok = receipt.Accepted, code = receipt.Code, protocol = 2, requestId, message = receipt.Accepted ? "Link accepted in Vydrop. Review it before downloading." : "Could not accept the link. Check the app connection and queue." };
                }
            }
            catch (EndOfStreamException) { return; }
            catch (OperationCanceledException) { return; }
            catch (Exception error)
            {
                response = new { ok = false, code = error is ArgumentException or InvalidDataException ? "invalid" : "unavailable", requestId,
                    message = "Could not send this link. Open Vydrop and enable browser integration, then try again." };
            }
            try { await BrowserProtocol.WriteAsync(output, response, CancellationToken.None).ConfigureAwait(false); } catch (IOException) { return; }
            if (!IsAllowedCaller(args)) return;
        }
    }
    private static void Launch() { using var process = Process.Start(new ProcessStartInfo(Environment.ProcessPath) { UseShellExecute = false, CreateNoWindow = true, ArgumentList = { "--browser-launch" } }); }
    internal static async Task<BrowserReceipt> DeliverRequestAsync(BrowserRequest request, Action launch, CancellationToken token)
    {
        for (int attempt = 0; attempt <= 20; attempt++)
        {
            var receipt = await BrowserLinkTransport.TrySendRequestAsync(request, 250, token).ConfigureAwait(false);
            if (receipt != null) return receipt;
            // A newer native host may share a pipe with an already running older desktop build.
            var legacy = await BrowserLinkTransport.TrySendAsync(request.Url, 250, token).ConfigureAwait(false);
            if (legacy != BrowserDelivery.NoListener)
                return new(legacy == BrowserDelivery.Accepted, legacy == BrowserDelivery.Accepted ? "opened" : "busy");
            if (attempt == 0) launch();
            await Task.Delay(200, token).ConfigureAwait(false);
        }
        throw new IOException("Open Vydrop and try sending the link again.");
    }

    internal static async Task<BrowserDelivery> DeliverAsync(string url, Action launch, CancellationToken token)
    {
        var delivery = await BrowserLinkTransport.TrySendAsync(url, 250, token).ConfigureAwait(false);
        if (delivery != BrowserDelivery.NoListener) return delivery;
        launch();
        for (int attempt = 0; attempt < 20; attempt++)
        {
            await Task.Delay(200, token).ConfigureAwait(false);
            delivery = await BrowserLinkTransport.TrySendAsync(url, 250, token).ConfigureAwait(false);
            if (delivery != BrowserDelivery.NoListener) return delivery;
        }
        throw new IOException("Open Vydrop and try sending the link again.");
    }
}
