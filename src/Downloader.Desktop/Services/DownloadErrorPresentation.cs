#nullable enable
using System.Net.Http;

namespace YouTubeDownloader;

internal static class DownloadErrorPresentation
{
    internal static string Describe(Exception exception, string networkRecovery = "Check your internet connection, then paste the link again to retry.")
    {
        string text = exception.Message.ToLowerInvariant();
        if (exception is MediaToolRecoveryException) return exception.Message;
        if (exception is ArgumentException)
            return exception.Message;
        if (text.Contains("selected subtitles")) return "The selected subtitles are no longer available. Refresh output options or choose no subtitles.";
        if (text.Contains("429") || text.Contains("too many requests"))
            return "This site is limiting downloads right now. Wait a few minutes, then try again.";
        if (text.Contains("[linkedin]") && (text.Contains("unable to extract video") || text.Contains("sign in") || text.Contains("login")))
            return "LinkedIn did not expose a public video at this link. Open the video in your browser and use the Vydrop button.";
        if (text.Contains("no longer available") || text.Contains("requested format"))
            return "That quality is no longer available. Paste the link again to refresh the options.";
        if (text.Contains("unsupported url") || text.Contains("no suitable extractor")) return "This site or link is not supported. Try a public video link.";
        if (text.Contains("private") || text.Contains("sign in") || text.Contains("logged-in") || text.Contains("login") || text.Contains("age-restricted"))
            return "This video requires sign-in. Try a publicly available video.";
        if (text.Contains("403"))
            return "This site refused this download. Wait a little, then paste the link again.";
        if (DownloadErrors.IsDiskFull(text))
            return "The drive is full. Free some space or choose another save folder.";
        if (exception is UnauthorizedAccessException || text.Contains("permission") || text.Contains("access is denied"))
            return "We can't save in that folder. Use Change to choose another save folder.";
        if (exception is HttpRequestException or TimeoutException || text.Contains("network") || text.Contains("connection"))
            return networkRecovery;
        if (text.Contains("integrity") || text.Contains("compatible") || text.Contains("media tool"))
            return "Media tools could not be prepared. Paste the link again to retry.";
        if (text.Contains("unavailable") || text.Contains("removed"))
            return "This video is unavailable. Try another video link.";
        return "We couldn't finish this action. Check the link and save folder, then try again.";
    }
}
