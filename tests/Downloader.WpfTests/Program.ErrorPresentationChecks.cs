using System.Net.Http;
using YouTubeDownloader;

internal static partial class Program
{
    private static void CheckErrorPresentation()
    {
        foreach (var (error, phrase) in new (Exception, string)[]
        {
            (new MediaToolRecoveryException(true, new Exception("fixture")), "latest downloader"),
            (new MediaToolRecoveryException(false, new Exception("fixture")), "couldn't be updated"),
            (new ArgumentException("Use Change to choose a valid folder on this computer."), "Use Change"),
            (new Exception("selected subtitles"), "subtitles"),
            (new Exception("429 too many requests"), "limiting downloads"),
            (new Exception("[linkedin] unable to extract video"), "Vydrop button"),
            (new Exception("requested format"), "quality"),
            (new Exception("unsupported url"), "not supported"),
            (new Exception("private login"), "sign-in"),
            (new Exception("403"), "refused"),
            (new Exception("no space left on device"), "drive is full"),
            (new UnauthorizedAccessException(), "Use Change"),
            (new HttpRequestException(), "internet connection"),
            (new TimeoutException(), "internet connection"),
            (new Exception("integrity"), "Media tools"),
            (new Exception("removed"), "unavailable"),
            (new Exception("unknown fixture"), "couldn't finish")
        })
        {
            string message = MainViewModel.DescribeError(error);
            Check(message.Contains(phrase, StringComparison.OrdinalIgnoreCase), "Extracted error presentation preserves recovery for " + error.Message);
            Check(Texts.For("ar", message) != message, "The recovery instruction has an Arabic translation");
        }
        Check(DownloadErrorPresentation.Describe(new TimeoutException(), "fixture recovery") == "fixture recovery", "Callers retain their specific network recovery instruction");
    }
}
