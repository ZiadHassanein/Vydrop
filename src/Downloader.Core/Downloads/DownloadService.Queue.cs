namespace YouTubeDownloader;
public sealed partial class DownloadService
{
    public Task<DownloadResult> DownloadQueueItemAsync(DownloadOptions options, IProgress<DownloadProgress> progress, CancellationToken token)
        => DownloadQueueItemAsync(options, progress, null, token);
    public async Task<DownloadResult> DownloadQueueItemAsync(DownloadOptions options, IProgress<DownloadProgress> progress, Action<DownloadOptions> prepared, CancellationToken token)
    {
        options = options.Snapshot();
        // Only an inspection from this execution may bypass the provider's stale-choice check.
        options.FreshMetadata = null;
        Action<DownloadProgress> report = p => progress?.Report(p);
        Progress += report;
        try
        {
            if (options.Playlist == null && options.Selection == null)
            {
                var targets = ResolveTargets(options.Url);
                if (targets.VideoUrl == null)
                    options.Playlist = await InspectPlaylistAsync(options.Url, progress, token).ConfigureAwait(false);
                else
                {
                    var metadata = await InspectAsync(options.Url, progress, token).ConfigureAwait(false);
                    options.ThumbnailUrl = metadata.ThumbnailUrl;
                    options.Uploader = metadata.Uploader;
                    options.Duration = metadata.Duration;
                    options.FreshMetadata = metadata.Collection == null ? metadata : null;
                    if (string.IsNullOrWhiteSpace(options.StableOutputName)) options.StableOutputName = metadata.Title;
                    if (metadata.Live) throw new InvalidOperationException("This video is live right now. Try again once the stream has ended.");
                    if (metadata.Collection != null) options.Playlist = metadata.Collection;
                    else if (options.Audio && !metadata.HasAudio) throw new InvalidOperationException("This video has no downloadable audio stream.");
                    else if (!options.Audio)
                    {
                        var quality = metadata.QualitiesFor(options.VideoContainer).FirstOrDefault(q => options.Height == 0 || q.Resolution <= options.Height) ?? throw new InvalidOperationException("The selected quality is no longer available.");
                        options.Selection = quality.Selection;
                        options.QualityKey = quality.SelectionKey;
                    }
                    progress?.Report(new DownloadProgress { Stage = "Checking video", ItemTitle = metadata.Title, Indeterminate = true });
                }
            }
            if (options.JobId != Guid.Empty) prepared?.Invoke(options.Snapshot());
            return await DownloadAsync(options, progress, token).ConfigureAwait(false);
        }
        finally { Progress -= report; }
    }
}
