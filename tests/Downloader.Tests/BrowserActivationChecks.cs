using Microsoft.Win32.SafeHandles;
using YouTubeDownloader;

internal static class BrowserActivationChecks
{
    internal static void CheckPolicy()
    {
        Action<SafePipeHandle> original = BrowserForegroundPermission.Grant;
        using var sentinel = new SafePipeHandle(new IntPtr(1), false);
        int grants = 0;
        BrowserForegroundPermission.Grant = handle => { if (handle != sentinel) throw new Exception("Wrong server handle."); grants++; };
        try
        {
            foreach (string type in new[] { "ping", "inspect", "unknown", "open-link", "enqueue", "download" })
            {
                int before = grants;
                BrowserForegroundPermission.ForRequest(sentinel, type);
                if (grants - before != (type is "open-link" or "enqueue" or "download" ? 1 : 0))
                    throw new Exception("Unexpected foreground delegation: " + type);
            }
            Console.WriteLine("PASS: Foreground delegation is limited to explicit open/download/queue actions.");
        }
        finally { BrowserForegroundPermission.Grant = original; }
    }

    internal static async Task Run()
    {
        string previousPipe = BrowserLinkTransport.PipeNameOverride;
        Action<SafePipeHandle> previousGrant = BrowserForegroundPermission.Grant;
        int grants = 0;
        BrowserForegroundPermission.Grant = pipe =>
        {
            if (pipe.IsClosed || pipe.IsInvalid) throw new Exception("Foreground permission must use the connected server pipe.");
            grants++;
        };
        BrowserLinkTransport.PipeNameOverride = "Vydrop.Activation." + Guid.NewGuid().ToString("N");
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var listener = BrowserLinkTransport.ListenRequestsAsync(_ => new(true, "accepted"), deadline.Token);
        try
        {
            foreach (string type in new[] { "ping", "inspect", "open-link", "enqueue", "download" })
            {
                int before = grants;
                var request = new BrowserRequest(2, type, type == "ping" ? null : "https://example.com/video", "activation-test",
                    Output: type == "download" ? new("audio", "mp3", 0) : null);
                var result = await BrowserLinkTransport.TrySendRequestAsync(request, 1000, deadline.Token);
                if (result?.Accepted != true || grants - before != (type is "ping" or "inspect" ? 0 : 1))
                    throw new Exception("Foreground permission was not restricted to explicit browser actions: " + type);
            }
            int beforeLegacy = grants;
            if (await BrowserLinkTransport.TrySendAsync("https://example.com/video", 1000, deadline.Token) != BrowserDelivery.Accepted || grants != beforeLegacy + 1)
                throw new Exception("Legacy explicit link delivery lost foreground delegation.");
            Console.WriteLine("PASS: Explicit browser actions delegate to the connected desktop before delivery; health and inspection stay passive.");
        }
        finally
        {
            deadline.Cancel(); await listener;
            BrowserLinkTransport.PipeNameOverride = previousPipe;
            BrowserForegroundPermission.Grant = previousGrant;
        }
    }
}
