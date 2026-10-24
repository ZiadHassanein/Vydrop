using System.IO;
using YouTubeDownloader;

internal static partial class Program
{
    private static async Task CheckBrowserPlaylists(MainViewModel model, FakeProvider provider, string root)
    {
        model.Folder = Path.Combine(root, "browser-playlists");
        provider.DownloadHandler = (request, _, _) => new() { FilePath = Path.Combine(request.Folder, "item.mp4"), Completed = 1 };
        try
        {
            foreach (string url in new[] { "https://www.youtube.com/playlist?list=PLFixture", "https://www.youtube.com/watch?v=jNQXAC9IVRw&list=RDjNQXAC9IVRw&start_radio=1" })
            {
                int before = provider.PlaylistInspections;
                var receipt = model.ReceiveBrowserRequest(new(2, "download", url, Guid.NewGuid().ToString("N"), Output: new("video", "mp4", 720), WholePlaylist: true));
                Check(receipt.Accepted, "A browser whole-playlist request is accepted: " + url);
                var job = model.Queue.Entries[^1];
                await WaitFor(() => job.State is QueueState.Completed or QueueState.Failed, "Browser playlist execution finishes");
                Check(job.State == QueueState.Completed && provider.PlaylistInspections == before + 1 && job.Options.Playlist?.Entries.Count == 1 && job.Options.PlaylistRequested &&
                    provider.LastRequest.PlaylistItem && provider.LastRequest.Height == 720, "The requested playlist, including a Mix, is inspected and scheduled instead of downloading only its current video");
                await WaitFor(() => model.IsIdle, "Playlist operation becomes idle");
            }
            const string mix = "https://www.youtube.com/watch?v=jNQXAC9IVRw&list=RDjNQXAC9IVRw&start_radio=1";
            var single = model.Queue.Add(new DownloadOptions { Url = mix, Folder = model.Folder, Height = 720 }, "This video only");
            int countBefore = model.Queue.Entries.Count;
            var separate = model.ReceiveBrowserRequest(new(2, "download", mix, Guid.NewGuid().ToString("N"), Output: new("video", "mp4", 720), WholePlaylist: true));
            Check(separate.Accepted && model.Queue.Entries.Count == countBefore + 1, "A whole Mix remains a separate queue job from its current video with the same URL and output profile");
            var whole = model.Queue.Entries[^1];
            await WaitFor(() => whole.State is QueueState.Completed or QueueState.Failed, "Separate Mix job finishes");
            Check(single.State == QueueState.Pending && whole.State == QueueState.Completed, "Starting the whole playlist never starts or replaces a separately queued single video");
            await WaitFor(() => model.IsIdle, "Separate Mix operation becomes idle");
            var open = model.ReceiveBrowserRequest(new(2, "open-link", "https://www.youtube.com/playlist?list=PLFixture", Guid.NewGuid().ToString("N")));
            Check(open.Accepted && model.WholePlaylist, "Playlist-only links from older extensions open the app with Whole playlist selected");
        }
        finally { provider.DownloadHandler = null; }
    }
}
