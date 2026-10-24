using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace YouTubeDownloader;
public sealed class AppRelease
{
    public Version Version;
    public string PackageUrl, ChecksumUrl, PackageName, PackageHash;
}

public sealed class PackageManifest
{
    public string Version;
    public string Architecture;
    public Dictionary<string, string> Files = new();
    // Separate from Files so installed v4 updaters can still validate the desktop payload.
    public Dictionary<string, string> BrowserFiles;
}

public static class AppUpdates
{
    // Canonical endpoint after the owner-approved rename. Installed versions through 4.2.1
    // still pin ViFree and need a manual upgrade; keep redirect validation strict.
    public const string Repository = "https://github.com/ZiadHassanein/Vydrop";
    private const string Api = "https://api.github.com/repos/ZiadHassanein/Vydrop/releases/latest";
    internal static readonly string[] PackageFiles =
    {
        "Vydrop.exe",
        "README.txt",
        "THIRD-PARTY-NOTICES.txt",
        "package.json"
    };
    private static readonly JsonCodec Json = new()
    {
        MaxJsonLength = 256 * 1024
    };
    public static Version Current => Assembly.GetExecutingAssembly().GetName().Version;

    public static async Task<AppRelease> CheckAsync(CancellationToken token)
    {
        using var client = new VerifiedDownloads();
        string text = await client.TextAsync(Api, Api, 2 * 1024 * 1024, token).ConfigureAwait(false);
        return Parse(text, Current);
    }

    internal static AppRelease Parse(string text, Version current)
    {
        using var document = JsonDocument.Parse(text);
        var root = document.RootElement;
        if (root.GetProperty("draft").GetBoolean() || root.GetProperty("prerelease").GetBoolean())
            return null;
        if (!Version.TryParse(root.GetProperty("tag_name").GetString()?.TrimStart('v', 'V'), out var version) || version.Build < 0 || Compare(version) <= Compare(current))
            return null;
        string name = "Vydrop-v" + version.ToString(3) + "-win-x64.zip", package = null, checksum = null, packageHash = null;
        foreach (var asset in root.GetProperty("assets").EnumerateArray())
        {
            string url = asset.GetProperty("browser_download_url").GetString(), file = asset.GetProperty("name").GetString();
            if (!Trusted(url))
                continue;
            if (file == name)
            {
                package = url;
                if (asset.TryGetProperty("digest", out var digest) && digest.ValueKind != JsonValueKind.Null)
                {
                    string value = digest.ValueKind == JsonValueKind.String ? digest.GetString() : null;
                    if (!Regex.IsMatch(value ?? "", "^sha256:[A-Fa-f0-9]{64}$"))
                        throw new InvalidDataException("The release package digest is invalid.");
                    packageHash = value[7..];
                }
            }
            if (file == "checksums-desktop.json")
                checksum = url;
        }

        if (package == null || (packageHash == null && checksum == null))
            throw new InvalidDataException("This release has no verified desktop package yet.");
        return new AppRelease
        {
            Version = version,
            PackageName = name,
            PackageUrl = package,
            PackageHash = packageHash,
            ChecksumUrl = checksum
        };
    }

    private static Version Compare(Version value) => new(value.Major, value.Minor, Math.Max(0, value.Build), Math.Max(0, value.Revision));
    private static bool Trusted(string url)
    {
        try
        {
            VerifiedDownloads.ValidateOrigin(url, Repository + "/releases/download/");
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static async Task<string> PrepareAsync(AppRelease release, CancellationToken token)
    {
        if (!Trusted(release.PackageUrl) || (release.ChecksumUrl != null && !Trusted(release.ChecksumUrl)))
            throw new InvalidDataException("The update address is invalid.");
        using var client = new VerifiedDownloads();
        string hash = release.PackageHash;
        if (release.ChecksumUrl != null)
        {
            var sum = Json.Deserialize<Dictionary<string, string>>(await client.TextAsync(release.ChecksumUrl, Repository + "/releases/download/", 4096, token).ConfigureAwait(false));
            if (!sum.TryGetValue("Hash", out var legacyHash) || !sum.TryGetValue("File", out var filename) || filename != release.PackageName || (hash != null && !string.Equals(hash, legacyHash, StringComparison.OrdinalIgnoreCase)))
                throw new InvalidDataException("The update checksum is invalid.");
            hash = legacyHash;
        }
        if (!Regex.IsMatch(hash ?? "", "^[A-Fa-f0-9]{64}$"))
            throw new InvalidDataException("The update checksum is invalid.");
        string stage = Path.Combine(AppPaths.UserData, "Updates", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(stage);
        string archive = Path.Combine(stage, "update.zip");
        await client.DownloadAsync(release.PackageUrl, Repository + "/releases/download/", archive, 256L * 1024 * 1024, hash, null, null, token).ConfigureAwait(false);
        ExtractPackage(archive, stage, token);
        ValidatePackage(stage, release.Version);
        return stage;
    }

    internal static void ExtractPackage(string archive, string stage, CancellationToken token)
    {
        SafeArchives.Extract(archive, stage, PackageFiles.ToDictionary(f => f, f => f), 400L * 1024 * 1024, token);
        var manifest = Json.Deserialize<PackageManifest>(File.ReadAllText(Path.Combine(stage, "package.json")));
        ValidateBrowserFiles(manifest?.BrowserFiles);
        if (manifest?.BrowserFiles == null) return; // Older desktop-only package.
        foreach (var group in manifest.BrowserFiles.Keys.GroupBy(Path.GetDirectoryName))
            SafeArchives.Extract(archive, Path.Combine(stage, group.Key), group.ToDictionary(f => f, Path.GetFileName), 400L * 1024 * 1024, token);
    }

    internal static bool IsBrowserFile(string name) => Regex.IsMatch(name ?? "", "^Browser/(?:chromium/|firefox/)?[A-Za-z0-9_-][A-Za-z0-9._-]{0,100}$") &&
        (name.StartsWith("Browser/chromium/", StringComparison.Ordinal) || name.StartsWith("Browser/firefox/", StringComparison.Ordinal) || name is "Browser/README.md" or "Browser/LICENSE.txt");

    private static void ValidateBrowserFiles(Dictionary<string, string> files)
    {
        if (files == null) return;
        if (files.Count is < 2 or > 80 || !files.ContainsKey("Browser/chromium/manifest.json") || !files.ContainsKey("Browser/firefox/manifest.json") ||
            files.Keys.Any(name => !IsBrowserFile(name)) || files.Keys.Distinct(StringComparer.OrdinalIgnoreCase).Count() != files.Count)
            throw new InvalidDataException("The browser update manifest is invalid.");
    }

    internal static string[] PayloadFiles(PackageManifest manifest) => PackageFiles.Concat(manifest.BrowserFiles?.Keys ?? Enumerable.Empty<string>()).ToArray();

    internal static PackageManifest ValidatePackage(string directory, Version version, bool validateBrowser = true)
    {
        var manifest = Json.Deserialize<PackageManifest>(File.ReadAllText(Path.Combine(directory, "package.json")));
        if (manifest == null || manifest.Architecture != "win-x64" || !Version.TryParse(manifest.Version, out var declared) || Compare(declared) != Compare(version) || manifest.Files == null || !manifest.Files.Keys.Order().SequenceEqual(PackageFiles.Where(f => f != "package.json").Order()))
            throw new InvalidDataException("The update manifest is invalid.");
        ValidateBrowserFiles(manifest.BrowserFiles);
        long browserBytes = 0;
        foreach (var entry in manifest.Files.Concat(validateBrowser && manifest.BrowserFiles != null ? manifest.BrowserFiles : Enumerable.Empty<KeyValuePair<string, string>>()))
        {
            if (!Regex.IsMatch(entry.Value ?? "", "^[A-Fa-f0-9]{64}$"))
                throw new InvalidDataException("The update file checksum is invalid.");
            string path = Path.Combine(directory, entry.Key);
            UpdateTransaction.CheckRegularPath(path);
            using var file = File.OpenRead(path);
            if (IsBrowserFile(entry.Key) && ((browserBytes += file.Length) > 12 * 1024 * 1024 || file.Length > 4 * 1024 * 1024))
                throw new InvalidDataException("The browser update is too large.");
            if (!string.Equals(Convert.ToHexString(SHA256.HashData(file)), entry.Value, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("The update files failed their integrity check.");
        }

        string executable = Path.Combine(directory, "Vydrop.exe");
        if (!ToolProbe.IsX64(executable) || !Version.TryParse(FileVersionInfo.GetVersionInfo(executable).FileVersion, out var actual) || Compare(actual) != Compare(version))
            throw new InvalidDataException("The update executable version is invalid.");
        return manifest;
    }

    public static void InstallAndRestart(string prepared) => UpdateTransaction.Launch(prepared, Environment.ProcessPath, Environment.ProcessId);
    public static bool RunHelper(string[] args)
    {
        if (args.Length == 2 && args[0] == "--apply-update")
        {
            UpdateTransaction.Run(args[1]);
            return true;
        }

        if (args.Length == 2 && args[0] == "--recover-update")
        {
            UpdateTransaction.Run(args[1], true);
            return true;
        }

        UpdateTransaction.SetAcknowledgement(args);
        return UpdateTransaction.RecoverFor(Environment.ProcessPath);
    }

    public static string RecoveryMessage => UpdateTransaction.RecoveryMessage;
    public static bool IsUpdateStartup => UpdateTransaction.IsUpdateStartup;

    public static void AcknowledgeStartup() => UpdateTransaction.Acknowledge();
}
