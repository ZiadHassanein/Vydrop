using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using YouTubeDownloader;

internal static partial class Program
{
    // Actual native controls with isolated demonstration jobs, never the user's queue or files.
    private static async Task CaptureReleaseDocumentation(MainWindow window, MainViewModel model, FakeProvider provider, FixtureThumbnailLoader loader, string captures)
    {
        captures = Path.GetFullPath(captures);
        provider.VideoTitle = "Vydrop browser extension setup";
        provider.VideoUploader = "Ziad Hassanein · Vydrop";
        provider.VideoDuration = 155.4;
        string repo = Environment.GetEnvironmentVariable("VYDROP_SOURCE_ROOT") ?? throw new InvalidOperationException("Set VYDROP_SOURCE_ROOT to the source checkout.");
        string poster = Path.Combine(repo, "docs/images/extension/video/poster.jpg");
        var artwork = new BitmapImage(); artwork.BeginInit(); artwork.CacheOption = BitmapCacheOption.OnLoad; artwork.UriSource = new Uri(poster); artwork.EndInit(); artwork.Freeze();
        loader.Handler = (_, _) => Task.FromResult<ImageSource>(artwork);
        string folder = Path.Combine(captures, "Downloads"); Directory.CreateDirectory(folder);
        model.Folder = folder;
        window.Width = 1296; window.Height = 800;
        DownloadOptions Example(string id, bool audio = false) => new()
        {
            Url = "https://ziadhassanein.github.io/Vydrop/" + id,
            Folder = folder, Audio = audio, Height = audio ? 0 : 1080,
            ThumbnailUrl = "https://example.test/vydrop-setup.jpg", Duration = 155.4
        };
        var complete = model.Queue.Add(Example("browser-extension-setup.html"), "Get started with Vydrop");
        await model.Queue.RunJobAsync(complete.Id, (_, _, _) => Task.FromResult(new DownloadResult { FilePath = Path.Combine(repo, "docs/media/vydrop-extension-setup.mp4") }));
        var paused = model.Queue.Add(Example("audio-notes", true), "Audio notes · A little inspiration");
        await model.Queue.RunJobAsync(paused.Id, (_, _, token) => { model.Queue.Pause(); return Task.FromCanceled<DownloadResult>(token); });
        var waiting = model.Queue.Add(Example("next-video"), "Your next favorite video");
        model.SelectedLibraryItem = model.QueueItems.First(item => item.Id == complete.Id);
        foreach (string language in new[] { "en", "ar" }) foreach (bool dark in new[] { true, false })
        {
            model.Url = ""; model.LibraryFilterCommand.Execute("all");
            model.SelectLanguageCommand.Execute(language); if (model.IsDark != dark) model.ThemeCommand.Execute(null);
            await Task.Delay(400); window.UpdateLayout();
            Check(window.ManagerContent.DetailsBorder.IsVisible && window.ManagerContent.HistoryButton.IsVisible, "Release screenshots show the real manager and inspector");
            Capture(window, Path.Combine(captures, $"vydrop-{(dark ? "dark" : "light")}-{language}-manager.png"));
            model.NewDownloadCommand.Execute(null); model.Url = "https://youtu.be/jNQXAC9IVRw";
            for (int attempt = 0; attempt < 100 && !model.CanDownload; attempt++) await Task.Delay(25);
            Check(model.CanDownload, "The release Add download capture has an inspected output choice");
            await Task.Delay(200); window.UpdateLayout();
            Capture(window, Path.Combine(captures, $"vydrop-{(dark ? "dark" : "light")}-{language}-download.png"));
        }
        model.Url = "";
        foreach (string language in new[] { "en", "ar" })
        {
            model.SelectLanguageCommand.Execute(language);
            var setupActions = new SetupFixture();
            var setup = new BrowserSetupViewModel(key => Texts.For(language, key), setupActions, false);
            setup.StartSetupCommand.Execute(null);
            var dialog = new BrowserSetupWindow { DataContext = setup, Owner = window, ShowActivated = false };
            try { dialog.Show(); await Task.Delay(180); Capture(dialog, Path.Combine(captures, $"guided-setup-{language}.png")); }
            finally { dialog.Close(); }
        }
        Check(model.Queue.Entries.Count == 3 && model.Queue.Entries.Any(entry => entry.Id == waiting.Id && entry.State == QueueState.Pending), "Documentation fixtures stay isolated and do not start pending jobs");
    }
}
