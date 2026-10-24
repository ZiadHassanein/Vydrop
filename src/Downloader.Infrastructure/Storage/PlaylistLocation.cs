using System.Security.Cryptography;
using System.Text;

namespace YouTubeDownloader;

internal static class PlaylistLocation
{
    private sealed class Location { public string Name, PreviousName; }
    internal static (string Folder, string PreviousFolder) Resolve(DownloadOptions options, string suggested, string state)
    {
        string root = Path.GetFullPath(options.Folder);
        string id = new string(options.Playlist.Id.Where(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_').Take(96).ToArray());
        string legacy = "Playlist [" + id + "]";
        string key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(root.TrimEnd('\\').ToUpperInvariant() + "\n" + options.Playlist.Id)));
        string directory = Path.Combine(state, "Locations"), record = Path.Combine(directory, key + ".json");
        Regular(record, state);
        var json = new JsonCodec { MaxJsonLength = 4096 };
        Location location = null;
        if (File.Exists(record) && new FileInfo(record).Length <= 4096)
            try { location = json.Deserialize<Location>(File.ReadAllText(record)); }
            catch (System.Text.Json.JsonException) { }
        if (location == null || !Valid(location.Name) || (location.PreviousName != null && location.PreviousName != legacy))
        {
            string name = Path.GetFileName(suggested);
            for (int suffix = 0; Directory.Exists(Path.Combine(root, name)) || File.Exists(Path.Combine(root, name)); suffix++)
            {
                if (suffix > 100) throw new IOException("Choose another playlist download folder.");
                name = Path.GetFileName(suggested) + " [" + id + "]" + (suffix == 0 ? "" : " (" + suffix.ToString(System.Globalization.CultureInfo.InvariantCulture) + ")");
            }
            location = new Location { Name = name, PreviousName = Directory.Exists(Path.Combine(root, legacy)) ? legacy : null };
            Directory.CreateDirectory(directory);
            string temp = record + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var file = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                { file.Write(Encoding.UTF8.GetBytes(json.Serialize(location))); file.Flush(true); }
                File.Move(temp, record, true);
            }
            finally { if (File.Exists(temp)) File.Delete(temp); }
        }
        string folder = Path.Combine(root, location.Name), previous = location.PreviousName == null ? null : Path.Combine(root, location.PreviousName);
        Regular(folder, root);
        if (previous != null)
        {
            Regular(previous, root);
            // Persist the location before moving, so an interrupted rename can finish on resume.
            if (Directory.Exists(previous) && !Directory.Exists(folder)) Directory.Move(previous, folder);
            else if (Directory.Exists(previous) && Directory.Exists(folder))
                throw new IOException("The playlist folder already exists. Choose another save folder.");
        }
        return (folder, previous);
    }
    private static bool Valid(string name) => !string.IsNullOrWhiteSpace(name) && name.Length <= 220 && name == name.Trim().TrimEnd('.') && name == Path.GetFileName(name) && !name.Any(c => char.IsControl(c) || Path.GetInvalidFileNameChars().Contains(c)) && name is not "." and not "..";
    private static void Regular(string path, string boundary)
    {
        for (string current = Path.GetFullPath(path); current != null; current = Path.GetDirectoryName(current))
        {
            // The user may deliberately select a redirected Downloads folder. Protect its
            // children without rejecting the chosen root or Windows profile redirection.
            if (string.Equals(current, Path.GetFullPath(boundary), StringComparison.OrdinalIgnoreCase)) break;
            if ((File.Exists(current) || Directory.Exists(current)) && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Choose a regular local playlist folder.");
        }
    }
}
