using YouTubeDownloader;

internal static class BrowserFreshPcChecks
{
    internal static async Task Run()
    {
        string previous = BrowserLinkTransport.PipeNameOverride;
        string root = Path.Combine(AppContext.BaseDirectory, "fresh-browser-" + Guid.NewGuid().ToString("N"));
        using var dependencies = new DependencyManager(root, Array.Empty<string>());
        dependencies.AcquireArtifact = (_, _, _) => throw new Exception("The native format host must not bootstrap tools invisibly.");
        try
        {
            foreach (bool cold in new[] { false, true })
            {
                foreach (bool accept in new[] { true, false })
                {
                    BrowserLinkTransport.PipeNameOverride = "Vydrop.FreshPC." + Guid.NewGuid().ToString("N");
                    using var lifetime = new CancellationTokenSource(TimeSpan.FromSeconds(8));
                    BrowserRequest received = null; int starts = 0; Task listener = null;
                    void Listen() => listener = BrowserLinkTransport.ListenRequestsAsync(request => { received = request; return new(accept, accept ? "opened" : "busy"); }, lifetime.Token);
                    if (!cold) Listen();
                    var source = new BrowserRequest(2, "inspect", "https://example.com/video.mp4", "fresh_setup", "https://example.com/post", "fixture");
                    try
                    {
                        var result = await BrowserHost.PrepareInspectionAsync(source, dependencies, () => { starts++; Listen(); }, lifetime.Token);
                        if (result.Accepted != accept || result.Code != (accept ? "preparing" : "busy") ||
                            starts != (cold ? 1 : 0) || received != source with { Type = "open-link" })
                            throw new Exception("Fresh-PC setup lost its manual-review handoff, source identity or error state.");
                        Console.WriteLine("PASS: Fresh-PC tools prepare through visible desktop review without automatic download: cold=" + cold + ", accepted=" + accept);
                    }
                    finally { lifetime.Cancel(); if (listener != null) await listener; }
                }
            }
            if (Directory.Exists(root)) throw new Exception("Native inspection unexpectedly wrote a private tool cache.");
            Console.WriteLine("PASS: A clean native host does not download tools or create staging before desktop handoff");
        }
        finally { BrowserLinkTransport.PipeNameOverride = previous; }
    }
}
