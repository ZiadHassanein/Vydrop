using System.Text.Json;
using System.Security.Cryptography;

namespace YouTubeDownloader;

// Used for the first upgrade from a desktop-only updater and for already-loaded recovery copies.
// The regular updater journals the complete app/Browser payload in UpdateTransaction.
internal static class CompanionFileTransaction
{
    private sealed record Entry(string Name, bool Existed, string Hash);
    private sealed record Journal(bool Committed, Entry[] Entries);
    internal static Action<string> Fault { get; set; }
    private const string Work = ".vydrop-companion-update";

    internal static void Recover(string root)
    {
        string work = Path.Combine(root, Work), record = Path.Combine(work, "journal.json");
        if (!File.Exists(record)) return;
        Regular(record);
        if (new FileInfo(record).Length > 32 * 1024) throw new InvalidDataException("Invalid companion recovery record.");
        var journal = JsonSerializer.Deserialize<Journal>(File.ReadAllText(record));
        if (journal?.Entries == null || journal.Entries.Length > 80 || journal.Entries.Select(e => e.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() != journal.Entries.Length ||
            journal.Entries.Any(e => !ValidName(e.Name))) throw new InvalidDataException("Invalid companion recovery record.");
        if (!journal.Committed)
        {
            foreach (var entry in journal.Entries.Where(entry => entry.Existed))
            {
                string backup = Path.Combine(work, "previous", entry.Name);
                Regular(backup);
                if (new FileInfo(backup).Length > 4 * 1024 * 1024 || Hash(backup) != entry.Hash) throw new InvalidDataException("Companion recovery files are damaged.");
            }
            foreach (var entry in journal.Entries)
            {
                string target = Path.Combine(root, entry.Name);
                Regular(target);
                if (entry.Existed) Copy(Path.Combine(work, "previous", entry.Name), target);
                else if (File.Exists(target)) File.Delete(target);
            }
        }
        Cleanup(work, journal.Entries);
    }

    internal static void Apply(string root, IReadOnlyDictionary<string, byte[]> files, IReadOnlyCollection<string> retired = null)
    {
        retired ??= Array.Empty<string>();
        if (files.Count + retired.Count > 80 || files.Keys.Concat(retired).Any(name => !ValidName(name)) || retired.Any(files.ContainsKey)) throw new InvalidDataException("Invalid companion file list.");
        Recover(root);
        var changed = files.Where(file => !Matches(Path.Combine(root, file.Key), file.Value)).ToArray();
        var removed = retired.Where(name => File.Exists(Path.Combine(root, name))).Distinct(StringComparer.Ordinal).ToArray();
        if (changed.Length == 0 && removed.Length == 0) return;
        foreach (string name in removed) Regular(Path.Combine(root, name));
        var entries = changed.Select(file => new Entry(file.Key, File.Exists(Path.Combine(root, file.Key)), File.Exists(Path.Combine(root, file.Key)) ? Hash(Path.Combine(root, file.Key)) : null))
            .Concat(removed.Select(name => new Entry(name, true, Hash(Path.Combine(root, name))))).ToArray();
        string work = Path.Combine(root, Work), record = Path.Combine(work, "journal.json");
        Regular(record);
        Directory.CreateDirectory(work);
        foreach (var entry in entries)
        {
            Regular(Path.Combine(root, entry.Name));
            if (entry.Existed) Copy(Path.Combine(root, entry.Name), Path.Combine(work, "previous", entry.Name));
        }
        Write(record, JsonSerializer.SerializeToUtf8Bytes(new Journal(false, entries)));
        try
        {
            foreach (var file in changed)
            {
                Fault?.Invoke(file.Key);
                Write(Path.Combine(root, file.Key), file.Value);
            }
            foreach (string name in removed)
            {
                Fault?.Invoke("remove-" + name);
                Regular(Path.Combine(root, name));
                File.Delete(Path.Combine(root, name));
            }
            Write(record, JsonSerializer.SerializeToUtf8Bytes(new Journal(true, entries)));
        }
        catch { Recover(root); throw; }
        Cleanup(work, entries);
    }

    private static bool ValidName(string name) => System.Text.RegularExpressions.Regex.IsMatch(name ?? "", "^(?:(?:chromium|firefox)/[A-Za-z0-9_-][A-Za-z0-9._-]{0,100}|README\\.md|LICENSE\\.txt)$");

    private static bool Matches(string path, byte[] bytes)
    {
        Regular(path);
        return File.Exists(path) && new FileInfo(path).Length == bytes.Length && File.ReadAllBytes(path).AsSpan().SequenceEqual(bytes);
    }
    private static void Copy(string source, string target)
    {
        Regular(source);
        if (new FileInfo(source).Length > 4 * 1024 * 1024) throw new InvalidDataException("Invalid companion backup.");
        Write(target, File.ReadAllBytes(source));
    }
    private static void Write(string path, byte[] bytes)
    {
        Regular(path);
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            { stream.Write(bytes); stream.Flush(true); }
            File.Move(temporary, path, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
    private static string Hash(string path)
    {
        Regular(path);
        using var stream = File.OpenRead(path);
        if (stream.Length > 4 * 1024 * 1024) throw new InvalidDataException("Invalid companion backup.");
        return Convert.ToHexString(SHA256.HashData(stream));
    }
    internal static void Regular(string path)
    {
        for (string current = Path.GetFullPath(path); current != null; current = Path.GetDirectoryName(current))
            if ((File.Exists(current) || Directory.Exists(current)) && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Companion updates require regular local folders.");
    }
    private static void Cleanup(string work, Entry[] entries)
    {
        foreach (var entry in entries)
        {
            string backup = Path.Combine(work, "previous", entry.Name);
            Regular(backup);
            if (File.Exists(backup)) File.Delete(backup);
        }
        foreach (string folder in new[] { "previous/chromium", "previous/firefox", "previous" })
        {
            string directory = Path.Combine(work, folder);
            if (Directory.Exists(directory) && !Directory.EnumerateFileSystemEntries(directory).Any()) Directory.Delete(directory);
        }
        File.Delete(Path.Combine(work, "journal.json"));
        if (!Directory.EnumerateFileSystemEntries(work).Any()) Directory.Delete(work);
    }
}
