namespace YouTubeDownloader;

// A small, bounded, public metadata response. No extractor URLs or credentials cross into the page.
internal static class BrowserMediaFormats
{
    internal static object From(VideoInfo media, string url = null)
    {
        if (media.Live) throw new InvalidOperationException("This video is live right now. Try again once the stream has ended.");
        return new
        {
            title = Limit(media.Title, 600),
            source = Uri.TryCreate(url, UriKind.Absolute, out var origin) ? origin.Host : null,
            thumbnail = Thumbnail(media.ThumbnailUrl),
            videos = Enum.GetValues<VideoContainer>().Select(container => new
            {
                format = OutputProfiles.VideoExtension(container),
                heights = media.QualitiesFor(container).Select(q => q.Resolution).Where(h => h is > 0 and <= 8640).Distinct().OrderDescending().Take(24).ToArray()
            }).Where(v => v.heights.Length > 0).ToArray(),
            audio = media.HasAudio ? new[] { "m4a", "mp3", "opus", "wav", "flac" } : Array.Empty<string>()
        };
    }
    private static string Thumbnail(string value)
    {
        try { return value?.StartsWith("https://", StringComparison.OrdinalIgnoreCase) == true ? MediaLink.PublicUrl(value) : null; }
        catch (ArgumentException) { return null; }
    }
    private static string Limit(string text, int length) => string.IsNullOrEmpty(text) ? "Video" : text[..Math.Min(length, text.Length)];
}
