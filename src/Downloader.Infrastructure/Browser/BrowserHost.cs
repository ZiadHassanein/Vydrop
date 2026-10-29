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
        // Reuse verified tools between explicit format checks. Daily maintenance remains on the download path.
        var dependencies = new DependencyManager();
        var provider = new YtDlpProvider(dependencies, checkAutomaticUpdates: false);
        using var inspector = new DownloadService(provider, new LocalPlaylistStorage());
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
                    if (health?.Accepted == true && (request.CompanionVersion != null || !BrowserHealth.IsRecentlyVerified(Environment.ProcessPath)))
                        BrowserHealth.Record(Environment.ProcessPath, request.CompanionVersion, request.CompanionBrowser);
                    response = new { ok = true, code = "ready", protocol = 2, requestId, appRunning = health?.Accepted == true, language = health?.Language, expectedCompanionVersion = CompanionVersion, capabilities = new[] { "ping", "enqueue", "source-context", "formats", "output-selection", "start-download", "playlist-download" } };
                }
                else if (request.Type == "inspect")
                {
                    timeout.CancelAfter(TimeSpan.FromSeconds(75));
                    var preparation = inspector.IsReady ? null : await PrepareInspectionAsync(request, dependencies, Launch, timeout.Token, provider.UsePreparedTools).ConfigureAwait(false);
                    if (preparation != null)
                        response = new { ok = false, code = preparation.Code, protocol = 2, requestId };
                    else
                    {
                        try
                        {
                            var media = await inspector.InspectAsync(request.Url, null, timeout.Token).ConfigureAwait(false);
                            response = new { ok = true, code = "formats", protocol = 2, requestId, media = BrowserMediaFormats.From(media, request.SourcePage ?? request.Url) };
                        }
                        catch (Exception error) when (error is not OutOfMemoryException)
                        {
                            // A site/metadata failure is distinct from a missing native connection.
                            response = new { ok = false, code = "formats", protocol = 2, requestId };
                        }
                    }
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
                    response = new { ok = receipt.Accepted, code = receipt.Code, protocol = 2, requestId, message = receipt.Accepted ? receipt.Code is "started" or "queued" ? "Download accepted in the Vydrop queue." : "Link opened in Vydrop. Review and start it there." : "Could not accept the link. Check the app connection and queue." };
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
    internal static async Task<BrowserReceipt> PrepareInspectionAsync(BrowserRequest request, DependencyManager dependencies, Action launch, CancellationToken token, Action<ToolSet> prepared = null)
    {
        // A fresh PC may need hundreds of MB of tools. Prepare them in the visible desktop
        // workflow, never behind the browser's short native-message inspection deadline.
        var available = await Task.Run(() => dependencies.Discover(token), token).ConfigureAwait(false);
        if (available != null) { prepared?.Invoke(available); return null; }
        using var delivery = CancellationTokenSource.CreateLinkedTokenSource(token);
        delivery.CancelAfter(TimeSpan.FromSeconds(15));
        var receipt = await DeliverRequestAsync(request with { Type = "open-link" }, launch, delivery.Token).ConfigureAwait(false);
        return receipt.Accepted ? new(true, "preparing") : receipt;
    }

    private static void Launch()
    {
        // Launch the GUI independently: inheriting the native host's redirected
        // standard handles keeps the browser's native-messaging pipes alive.
        using var process = Process.Start(new ProcessStartInfo(Environment.ProcessPath) { UseShellExecute = true, ArgumentList = { "--browser-launch" } });
    }
    internal static async Task<BrowserReceipt> DeliverRequestAsync(BrowserRequest request, Action launch, CancellationToken token)
    {
        bool useLegacy = false;
        for (int attempt = 0; attempt <= 20; attempt++)
        {
            if (!useLegacy)
            {
                var (receipt, legacyListener) = await BrowserLinkTransport.TrySendCompatibleRequestAsync(request, 250, token).ConfigureAwait(false);
                if (request.WholePlaylist && (legacyListener || receipt is { Accepted: false, Code: "invalid" })) return new(false, "unsupported");
                if ((request.Output != null || request.Type == "download") && receipt is { Accepted: false, Code: "invalid" })
                {
                    // Early v2 desktops reject unknown fields but still support link delivery.
                    var fallback = request.Type == "download" ? request with { Type = "open-link" } : request with { Output = null };
                    var compatible = await BrowserLinkTransport.TrySendRequestAsync(fallback, 250, token).ConfigureAwait(false);
                    if (compatible is { Accepted: false, Code: "invalid" } && fallback.Output != null)
                        compatible = await BrowserLinkTransport.TrySendRequestAsync(fallback with { Output = null }, 250, token).ConfigureAwait(false);
                    return compatible?.Accepted == true ? new(true, "opened-legacy") : compatible ?? receipt;
                }
                if (receipt != null) return receipt;
                useLegacy = legacyListener;
            }
            // A newer native host may share a pipe with an already running older desktop build.
            if (useLegacy)
            {
                var legacy = await BrowserLinkTransport.TrySendAsync(request.Url, 250, token).ConfigureAwait(false);
                if (legacy != BrowserDelivery.NoListener)
                    return new(legacy == BrowserDelivery.Accepted, legacy == BrowserDelivery.Accepted ? request.Output != null ? "opened-legacy" : "opened" : "busy");
            }
            if (attempt == 0 && !useLegacy) launch();
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
