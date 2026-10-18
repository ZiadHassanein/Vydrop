#nullable enable
namespace YouTubeDownloader;

public interface IMediaTitleLookup
{
    Task<string?> FindAsync(string url, CancellationToken token);
}
