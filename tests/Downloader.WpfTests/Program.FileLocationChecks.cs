using System.IO;
using System.Windows;
using YouTubeDownloader;

internal static partial class Program
{
    private static void CheckFileLocation()
    {
        var app = new Application();
        app.Resources.MergedDictionaries.Add(new ResourceDictionary
        { Source = new Uri("pack://application:,,,/Vydrop;component/Themes/Controls.xaml") });
        string root = Path.Combine(AppContext.BaseDirectory, "file-location-fixture-" + Guid.NewGuid().ToString("N"));
        string save = Path.Combine(root, "downloads"), output = Path.Combine(save, "Playlist — فيديو");
        Directory.CreateDirectory(output);
        string video = Path.Combine(output, "01 — فيديو, [test].mp4"), audio = Path.Combine(output, "02 — audio.mp3");
        File.WriteAllBytes(video, [1]); File.WriteAllBytes(audio, [1]);
        var jobs = new[]
        {
            new StoredQueueJob { Id = Guid.NewGuid(), Title = "Video", State = QueueState.Completed, Files = [video], Options = new() { Url = "https://example.com/video", Folder = save } },
            new StoredQueueJob { Id = Guid.NewGuid(), Title = "Audio", State = QueueState.Completed, Files = [audio], Options = new() { Url = "https://example.com/audio", Folder = save, Audio = true } },
            new StoredQueueJob { Id = Guid.NewGuid(), Title = "Pending", State = QueueState.Pending, Options = new() { Url = "https://example.com/pending", Folder = save } }
        };
        new LocalQueueStorage(root).Save(new QueueSnapshot { Jobs = jobs.ToList() });
        var folders = new List<string>(); var media = new List<(string File, bool Reveal)>(); bool shellFailure = false;
        // Capture the requested shell actions without opening Explorer or touching desktop focus.
        var actions = new DesktopActions(new Window(), folders.Add, (file, reveal) =>
        {
            if (shellFailure) throw new System.Runtime.InteropServices.COMException("Fixture shell failure.");
            media.Add((file, reveal));
        });
        using var service = new DownloadService(new FakeProvider(), new EmptyStorage());
        var model = new MainViewModel(service, new SettingsRepository(root), actions, new FixtureThumbnailLoader());
        model.LibraryFilterCommand.Execute("all");
        var selected = model.QueueItems.Single(row => row.Id == jobs[0].Id);
        model.SelectedLibraryItem = selected;
        model.OpenLibraryFolderCommand.Execute(null);
        Check(media.Single() == (video, true) && folders.Count == 0,
            "Details folder action reveals the exact completed video, including Unicode, spaces and punctuation");
        model.OpenQueueFolderCommand.Execute(jobs[1].Id);
        Check(media.Last() == (audio, true) && model.SelectedLibraryItem == selected,
            "Row folder action reveals its own audio file independently of the selected inspector item");
        Check(selected.FolderToOpen == output && selected.FolderActionLabel == "Show in folder",
            "Completed details show the actual playlist output directory and describe file selection");
        File.Delete(video);
        selected.Refresh(key => key, false);
        Check(model.OpenLibraryFolderCommand.CanExecute(null), "A missing file still allows its existing output folder to open");
        model.OpenLibraryFolderCommand.Execute(null);
        Check(folders.Single() == output && media.Count == 2 && selected.FolderActionLabel == "Open save folder",
            "A moved/deleted file falls back to its actual output directory without selecting a different file");
        model.OpenQueueFolderCommand.Execute(jobs[2].Id);
        Check(folders.Last() == save && media.Count == 2, "Pending downloads keep the open-save-folder action");
        model.SelectedLibraryItem = model.QueueItems.Single(row => row.Id == jobs[1].Id);
        shellFailure = true; model.OpenLibraryFolderCommand.Execute(null);
        Check(model.NoticeVisible && model.NoticeHeading == "Folder unavailable", "A shell selection failure stays inside the app as a recoverable notice");
        app.Shutdown();
    }
}
