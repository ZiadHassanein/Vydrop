using YouTubeDownloader;

internal static class PlaylistFolderChecks
{
    internal static void Run()
    {
        string root = Path.Combine(Path.GetTempPath(), "Vydrop-playlist-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            string state = Path.Combine(root, "state"), output = Path.Combine(root, "downloads"), id = "PL1234567890";
            Directory.CreateDirectory(output);
            var request = new DownloadOptions { Url = "https://www.youtube.com/playlist?list=" + id, Folder = output,
                Playlist = new PlaylistInfo { Id = id, Url = "https://www.youtube.com/playlist?list=" + id, Title = "Learn TypeScript in Arabic 2022" } };
            string legacy = Path.Combine(output, "Playlist [" + id + "]"); Directory.CreateDirectory(legacy);
            string video = Path.Combine(legacy, "01 Intro.mp4"); File.WriteAllText(video, "keep downloaded media");
            var old = PlaylistRecords.Open(legacy, OutputProfiles.Key(request), state); old.Completed["jNQXAC9IVRw"] = video; old.Save();
            var storage = new LocalPlaylistStorage(state); var session = storage.Open(request);
            Check(Path.GetFileName(session.Folder) == request.Playlist.Title && !Directory.Exists(legacy), "Playlist folder uses its real title and renames the legacy ID folder");
            Check(File.ReadAllText(Path.Combine(session.Folder, "01 Intro.mp4")) == "keep downloaded media" && session.Completed["jNQXAC9IVRw"] == Path.Combine(session.Folder, "01 Intro.mp4"), "Renaming retains downloads and rebases completion records for resume");
            request.Playlist.Title = "Renamed online";
            Check(storage.Open(request).Folder == session.Folder, "Metadata title changes never split a resumed playlist across folders");
            var another = request.Snapshot(); another.Url = another.Playlist.Url = "https://www.youtube.com/playlist?list=PL0987654321"; another.Playlist.Id = "PL0987654321"; another.Playlist.Title = "Learn TypeScript in Arabic 2022";
            Check(storage.Open(another).Folder != session.Folder && File.Exists(Path.Combine(session.Folder, "01 Intro.mp4")), "Different playlists with the same title never merge or overwrite each other");
            foreach (string title in new[] { "دورة TypeScript: البداية / الاختبار?", "CON", "..", new string('x', 500) })
            {
                another.Playlist.Id = "PL" + Guid.NewGuid().ToString("N"); another.Url = another.Playlist.Url = "https://www.youtube.com/playlist?list=" + another.Playlist.Id; another.Playlist.Title = title;
                string folder = storage.Open(another).Folder;
                Check(Directory.GetParent(folder).FullName == output && Path.GetFileName(folder).Length <= 220, "Playlist titles stay valid and inside the chosen download folder: " + title[..Math.Min(title.Length, 30)]);
            }
        }
        finally { Directory.Delete(root, true); }
    }
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); Console.WriteLine("PASS: " + message); }
}
