using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;

namespace YouTubeDownloader;

// A copied single-file app can restore its matching companion without a network request.
internal sealed class BrowserCompanionFiles
{
    private static readonly string[] RequiredFiles =
    ["manifest.json", "connection.js", "video-detection.js", "quality-picker.js", "video-overlay.js", "background.js", "popup.js", "popup-discovery.js", "media-discovery.js", "popup.html", "popup.css", "brand-logo-dark.svg", "icon.png", "icon16.png", "icon48.png"];
    private readonly Dictionary<string, byte[]> files = new(StringComparer.Ordinal);
    private readonly string externalRoot, recoveryRoot, cacheRoot;
    internal bool CanPrepare => files.Count > 0;

    internal BrowserCompanionFiles(string appDirectory, string dataDirectory, byte[] archive = null)
    {
        externalRoot = Path.Combine(Path.GetFullPath(appDirectory), "Browser");
        if (archive == null)
        {
            using var resource = typeof(BrowserCompanionFiles).Assembly.GetManifestResourceStream("YouTubeDownloader.BrowserCompanion");
            using var buffer = new MemoryStream(); resource?.CopyTo(buffer); archive = buffer.ToArray();
        }
        cacheRoot = Path.Combine(Path.GetFullPath(dataDirectory), "BrowserCompanion");
        recoveryRoot = Path.Combine(cacheRoot, Convert.ToHexString(SHA256.HashData(archive))[..24]);
        try
        {
            using var zip = new ZipArchive(new MemoryStream(archive), ZipArchiveMode.Read);
            long total = 0;
            foreach (var entry in zip.Entries)
            {
                if (entry.FullName.EndsWith('/')) continue;
                string name = entry.FullName;
                if (zip.Entries.Count > 80 || entry.Length > 4 * 1024 * 1024 || (total += entry.Length) > 12 * 1024 * 1024 ||
                    name.Contains('\\') || name.Split('/').Any(part => part is "" or "." or "..") ||
                    !(name.StartsWith("chromium/", StringComparison.Ordinal) || name.StartsWith("firefox/", StringComparison.Ordinal) || name is "README.md" or "LICENSE.txt"))
                    throw new InvalidDataException("Invalid companion archive.");
                using var stream = entry.Open(); using var buffer = new MemoryStream(); stream.CopyTo(buffer);
                files.Add(name, buffer.ToArray());
            }
            foreach (string folder in new[] { "chromium", "firefox" })
            {
                if (RequiredFiles.Any(name => !files.ContainsKey(folder + "/" + name))) throw new InvalidDataException();
                using var manifest = JsonDocument.Parse(files[folder + "/manifest.json"]);
                if (manifest.RootElement.GetProperty("manifest_version").GetInt32() != 3 || manifest.RootElement.GetProperty("name").GetString() != (ReleaseChannel.IsPreview ? "Vydrop Preview" : "Vydrop")) throw new InvalidDataException();
            }
        }
        catch (Exception error) when (error is InvalidDataException or ArgumentException or JsonException or KeyNotFoundException or InvalidOperationException) { files.Clear(); }
    }

    internal string ExtensionPath(SetupBrowser browser)
    {
        string root = Complete(externalRoot, browser) ? externalRoot : recoveryRoot;
        string folder = Path.Combine(root, Folder(browser));
        return browser == SetupBrowser.Firefox ? Path.Combine(folder, "manifest.json") : folder;
    }
    internal bool HasExtension(SetupBrowser browser) => Complete(externalRoot, browser) || Complete(recoveryRoot, browser);

    internal void RecoverInterruptedUpdate()
    {
        CompanionFileTransaction.Recover(externalRoot);
        foreach (string root in RecoveryCopies()) CompanionFileTransaction.Recover(root);
    }

    internal void UpdateInstalledCopies()
    {
        if (!CanPrepare) throw new InvalidDataException("Companion files are unavailable.");
        // Keep the actual loaded path stable, including copies recovered by earlier releases.
        CompanionFileTransaction.Apply(externalRoot, files);
        foreach (string root in RecoveryCopies())
        {
            var present = files.Where(file => !file.Key.Contains('/') || Directory.Exists(Path.Combine(root, file.Key.Split('/')[0]))).ToDictionary();
            CompanionFileTransaction.Apply(root, present);
        }
    }

    private IEnumerable<string> RecoveryCopies()
    {
        if (!Directory.Exists(cacheRoot)) yield break;
        CompanionFileTransaction.Regular(cacheRoot);
        foreach (string root in Directory.EnumerateDirectories(cacheRoot).Take(64))
        {
            string name = Path.GetFileName(root);
            if (!System.Text.RegularExpressions.Regex.IsMatch(name, "^[A-Fa-f0-9]{24}$")) continue;
            CompanionFileTransaction.Regular(root);
            bool owned = false;
            foreach (string folder in new[] { "chromium", "firefox" })
            {
                string path = Path.Combine(root, folder, "manifest.json");
                CompanionFileTransaction.Regular(path);
                if (!File.Exists(path) || new FileInfo(path).Length > 64 * 1024) continue;
                try
                {
                    using var manifest = JsonDocument.Parse(File.ReadAllBytes(path));
                    using var expected = JsonDocument.Parse(files[folder + "/manifest.json"]);
                    owned |= manifest.RootElement.GetProperty("name").GetString() == expected.RootElement.GetProperty("name").GetString() &&
                        (folder == "chromium" ? manifest.RootElement.GetProperty("key").GetString() == expected.RootElement.GetProperty("key").GetString() :
                        manifest.RootElement.GetProperty("browser_specific_settings").GetProperty("gecko").GetProperty("id").GetString() == expected.RootElement.GetProperty("browser_specific_settings").GetProperty("gecko").GetProperty("id").GetString());
                }
                catch (Exception error) when (error is JsonException or KeyNotFoundException or InvalidOperationException) { }
            }
            if (owned || File.Exists(Path.Combine(root, ".vydrop-companion-update", "journal.json"))) yield return root;
        }
    }
    private static string Folder(SetupBrowser browser) => browser == SetupBrowser.Firefox ? "firefox" : "chromium";
    private bool Complete(string root, SetupBrowser browser)
    {
        if (!CanPrepare) return false;
        string prefix = Folder(browser) + "/";
        try
        {
            return files.Where(file => file.Key.StartsWith(prefix, StringComparison.Ordinal)).All(file => Matches(Path.Combine(root, file.Key.Replace('/', Path.DirectorySeparatorChar)), file.Value));
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException) { return false; }
    }
    private static bool Matches(string path, byte[] expected)
    {
        if (!File.Exists(path) || new FileInfo(path).Length != expected.Length || (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0) return false;
        using var input = File.OpenRead(path);
        return SHA256.HashData(input).AsSpan().SequenceEqual(SHA256.HashData(expected));
    }
    internal void Prepare(SetupBrowser browser)
    {
        if (HasExtension(browser)) return;
        if (!CanPrepare) throw new InvalidDataException("Companion files are unavailable.");
        string prefix = Folder(browser) + "/";
        foreach (var file in files.Where(file => !file.Key.Contains('/') || file.Key.StartsWith(prefix, StringComparison.Ordinal)))
        {
            string path = Path.GetFullPath(Path.Combine(recoveryRoot, file.Key.Replace('/', Path.DirectorySeparatorChar)));
            if (!path.StartsWith(recoveryRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException();
            string parent = Path.GetDirectoryName(path);
            for (var directory = new DirectoryInfo(parent); directory != null && directory.FullName.StartsWith(recoveryRoot, StringComparison.OrdinalIgnoreCase); directory = directory.Parent)
                if (directory.Exists && (directory.Attributes & FileAttributes.ReparsePoint) != 0) throw new IOException("The companion folder must be a regular local folder.");
            Directory.CreateDirectory(parent);
            if (Matches(path, file.Value)) continue;
            string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try { File.WriteAllBytes(temporary, file.Value); File.Move(temporary, path, true); }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }
        if (!HasExtension(browser)) throw new IOException("Companion preparation could not be verified.");
    }
}
