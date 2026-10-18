#nullable enable
namespace YouTubeDownloader;

// Optional previews use a separate process and existing verified tools only.
// They never install/update tools or own the main download operation.
public sealed class MediaTitleLookup : IMediaTitleLookup
{
    private readonly Func<CancellationToken, ToolSet?> discover;
    public MediaTitleLookup() : this(Discover) { }
    internal MediaTitleLookup(Func<CancellationToken, ToolSet?> discover) => this.discover = discover;

    private static ToolSet? Discover(CancellationToken token)
    {
        using var dependencies = new DependencyManager();
        return dependencies.Discover(token);
    }

    public Task<string?> FindAsync(string url, CancellationToken token) => Task.Run(() =>
    {
        var targets = MediaLink.Resolve(url);
        var tools = discover(token);
        if (tools == null) return null;
        using var provider = new YtDlpProvider(tools);
        return targets.VideoUrl == null
            ? provider.InspectPlaylist(targets.PlaylistUrl, token).Title
            : provider.Inspect(targets.VideoUrl, token).Title;
    }, token);
}
