using System.IO.Compression;
using System.Security.Cryptography;
using YouTubeDownloader;

internal static class BundledUpdateChecks
{
    internal static void Run(string fixtures)
    {
        string root = Path.Combine(Path.GetTempPath(), "Vydrop-bundle-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            string stage = Path.Combine(root, "stage"); Directory.CreateDirectory(stage);
            File.Copy(Path.Combine(fixtures, "FakeDownloader.exe"), Path.Combine(stage, "Vydrop.exe"));
            File.WriteAllText(Path.Combine(stage, "README.txt"), "new readme");
            File.WriteAllText(Path.Combine(stage, "THIRD-PARTY-NOTICES.txt"), "new notices");
            var manifest = new PackageManifest { Version = "2.0.0", Architecture = "win-x64", BrowserFiles = new() };
            foreach (string browser in new[] { "chromium", "firefox" })
                foreach (string name in new[] { "manifest.json", "connection.js", "quality-picker.js" })
                {
                    string relative = $"Browser/{browser}/{name}", path = Path.Combine(stage, relative);
                    Directory.CreateDirectory(Path.GetDirectoryName(path)); File.WriteAllText(path, "new " + relative);
                    manifest.BrowserFiles[relative] = Hash(path);
                }
            foreach (string name in AppUpdates.PackageFiles.Where(name => name != "package.json")) manifest.Files[name] = Hash(Path.Combine(stage, name));
            void Save() => File.WriteAllText(Path.Combine(stage, "package.json"), new JsonCodec().Serialize(manifest));
            Save();
            AppUpdates.ValidatePackage(stage, new Version(2, 0, 0));
            string archive = Path.Combine(root, "complete.zip"), extracted = Path.Combine(root, "extracted");
            ZipFile.CreateFromDirectory(stage, archive);
            AppUpdates.ExtractPackage(archive, extracted, CancellationToken.None);
            AppUpdates.ValidatePackage(extracted, new Version(2, 0, 0));
            Check(File.Exists(Path.Combine(extracted, "Browser/firefox/quality-picker.js")), "Bundled preparation extracts and verifies both companion folders");
            foreach (string failure in new[] { "replace-Browser/chromium/quality-picker.js", "replace-Browser/firefox/connection.js", "before-start", "startup", "success" })
            {
                string target = Path.Combine(root, Guid.NewGuid().ToString("N")); Directory.CreateDirectory(target);
                File.WriteAllText(Path.Combine(target, "Vydrop.exe"), "old app");
                Directory.CreateDirectory(Path.Combine(target, "Browser/chromium"));
                File.WriteAllText(Path.Combine(target, "Browser/chromium/connection.js"), "old extension");
                File.WriteAllText(Path.Combine(target, "Browser/chromium/user-file.txt"), "keep custom file");
                File.WriteAllText(Path.Combine(target, "settings.json"), "keep settings");
                File.WriteAllText(Path.Combine(target, "queue.json"), "keep jobs");
                File.WriteAllText(Path.Combine(target, "video.mp4"), "keep download");
                var journal = new UpdateJournal { Target = Path.Combine(target, "Vydrop.exe"), Stage = stage, Backup = Path.Combine(target, "previous"), Version = "2.0.0" };
                UpdateTransaction.Fault = step => { if (step == failure) throw new IOException("Injected update failure"); };
                bool failed = false;
                try { UpdateTransaction.Apply(Path.Combine(target, "journal.json"), journal, () => { if (failure == "startup") throw new IOException("Startup failed"); }); }
                catch (IOException) { failed = true; }
                Check(failure == "success" ? !failed && Hash(journal.Target) == manifest.Files["Vydrop.exe"] && File.ReadAllText(Path.Combine(target, "Browser/firefox/connection.js")) == "new Browser/firefox/connection.js" :
                    failed && journal.Phase == "rolled-back" && File.ReadAllText(journal.Target) == "old app" && File.ReadAllText(Path.Combine(target, "Browser/chromium/connection.js")) == "old extension" && !File.Exists(Path.Combine(target, "Browser/firefox/connection.js")), "App and both extensions commit or roll back together: " + failure);
                Check(File.ReadAllText(Path.Combine(target, "settings.json")) == "keep settings" && File.ReadAllText(Path.Combine(target, "queue.json")) == "keep jobs" && File.ReadAllText(Path.Combine(target, "video.mp4")) == "keep download" && File.ReadAllText(Path.Combine(target, "Browser/chromium/user-file.txt")) == "keep custom file", "Updates preserve preferences, queue, media and unrelated files: " + failure);
            }
            manifest.BrowserFiles["Browser/../settings.json"] = new string('a', 64); Save();
            Reject(() => AppUpdates.ValidatePackage(stage, new Version(2, 0, 0)), "Browser manifest traversal is rejected");
            manifest.BrowserFiles.Remove("Browser/../settings.json"); Save();
            File.WriteAllText(Path.Combine(stage, "Browser/firefox/connection.js"), "damaged");
            Reject(() => AppUpdates.ValidatePackage(stage, new Version(2, 0, 0)), "Damaged companion files cannot be installed");
            AppUpdates.ValidatePackage(stage, new Version(2, 0, 0), false);
            Check(true, "The first upgrade can validate desktop-only output from an older helper");
            manifest.BrowserFiles = null; Save(); AppUpdates.ValidatePackage(stage, new Version(2, 0, 0));
            Check(true, "Legacy packages without companion metadata remain compatible");
        }
        finally { UpdateTransaction.Fault = null; Directory.Delete(root, true); }
    }
    private static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
    private static void Reject(Action action, string message) { bool rejected = false; try { action(); } catch (InvalidDataException) { rejected = true; } Check(rejected, message); }
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); Console.WriteLine("PASS: " + message); }
}
