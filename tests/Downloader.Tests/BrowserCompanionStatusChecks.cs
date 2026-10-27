using System.Text.Json;
using YouTubeDownloader;

internal static class BrowserCompanionStatusChecks
{
    internal static void Run()
    {
        BrowserRequest Parse(string json) { using var document = JsonDocument.Parse(json); return BrowserProtocol.ReadRequest(document.RootElement); }
        Check(Parse("""{"version":2,"type":"ping","requestId":"old"}""").CompanionVersion == null, "Legacy native health pings remain supported without a claimed extension version");
        var current = Parse("""{"version":2,"type":"ping","requestId":"current","companionVersion":"1.5.8","companionBrowser":"Chrome"}""");
        Check(current.CompanionVersion == "1.5.8" && current.CompanionBrowser == "Chrome", "A native health ping retains its loaded companion version and browser");
        foreach (string invalid in new[] {
            """{"version":2,"type":"ping","requestId":"bad","companionVersion":"1.5.8"}""",
            """{"version":2,"type":"ping","requestId":"bad","companionVersion":"garbage","companionBrowser":"Chrome"}""",
            """{"version":2,"type":"ping","requestId":"bad","companionVersion":"1.5.8","companionBrowser":"../Chrome"}""",
            """{"version":2,"type":"open-link","url":"https://example.com/video","requestId":"bad","companionVersion":"1.5.8","companionBrowser":"Chrome"}""",
            """{"version":1,"type":"open-link","url":"https://example.com/video","companionVersion":"1.5.8"}"""
        })
        {
            bool rejected = false; try { Parse(invalid); } catch (InvalidDataException) { rejected = true; }
            Check(rejected, "Malformed or misplaced companion status is rejected");
        }
        string root = Path.Combine(Path.GetTempPath(), "Vydrop.CompanionStatus." + Guid.NewGuid().ToString("N"));
        string previous = BrowserHealth.DirectoryOverride;
        try
        {
            BrowserHealth.DirectoryOverride = root;
            string executable = Environment.ProcessPath;
            Check(BrowserHealth.CompanionVersion(executable, "Chrome") == null, "Missing status cannot claim a current browser extension");
            BrowserHealth.Record(executable, "1.5.5", "Chrome");
            BrowserHealth.Record(executable, "1.5.8", "Edge");
            Check(BrowserHealth.CompanionVersion(executable, "Chrome") == "1.5.5" && BrowserHealth.CompanionVersion(executable, "Edge") == "1.5.8" && BrowserHealth.CompanionVersion(executable, "Firefox") == null, "Browser version reports do not cross Chrome, Edge or Firefox");
            Check(BrowserHealth.CompanionVersion(executable + ".other", "Chrome") == null, "A different app path cannot reuse the previous companion status");
            string report = Path.Combine(root, "verified-companion-Chrome.json");
            File.WriteAllText(report, JsonSerializer.Serialize(new { executable, build = File.GetLastWriteTimeUtc(executable), verifiedAt = DateTimeOffset.UtcNow.AddMinutes(-3), companionVersion = "1.5.8" }));
            Check(BrowserHealth.CompanionVersion(executable, "Chrome") == null, "Expired status cannot report a current extension version");
            File.WriteAllText(report, "broken");
            Check(BrowserHealth.CompanionVersion(executable, "Chrome") == null, "Damaged local extension status fails closed");
            BrowserHealth.Record(executable);
            Check(BrowserHealth.IsRecentlyVerified(executable) && BrowserHealth.CompanionVersion(executable, "Firefox") == null, "A legacy handshake verifies app connectivity without inventing an extension version");
        }
        finally { BrowserHealth.DirectoryOverride = previous; if (Directory.Exists(root)) Directory.Delete(root, true); }
        CleanupChecks();
    }
    private static void CleanupChecks()
    {
        string root = Path.Combine(Path.GetTempPath(), "Vydrop.Cleanup." + Guid.NewGuid().ToString("N"));
        string previous = UpdateTransaction.RootOverride;
        try
        {
            string updates = Path.Combine(root, "Updates"); Directory.CreateDirectory(updates);
            UpdateTransaction.RootOverride = updates;
            foreach (string name in new[] { "history.json", "settings.json", "queue.json", "saved-video.mp4" }) File.WriteAllText(Path.Combine(root, name), "keep " + name);
            string Create(string phase, bool active)
            {
                string id = Guid.NewGuid().ToString("N"), stage = Path.Combine(updates, id); Directory.CreateDirectory(stage);
                var journal = new UpdateJournal { Id = id, Target = Path.Combine(root, "Vydrop.exe"), Stage = stage, Backup = Path.Combine(stage, "previous"), Version = "4.4.12", Phase = phase };
                if (active) { using var process = System.Diagnostics.Process.GetCurrentProcess(); journal.HelperPid = process.Id; journal.HelperStart = process.StartTime.ToUniversalTime().Ticks; }
                File.WriteAllText(Path.Combine(stage, "transaction.json"), new JsonCodec().Serialize(journal));
                File.WriteAllText(Path.Combine(stage, "update.zip"), "old download");
                return stage;
            }
            string complete = Create("committed", false), busy = Create("committed", true), recovery = Create("recovery-required", false), prepared = Create("prepared", false);
            string extra = Create("committed", false);
            File.WriteAllText(Path.Combine(extra, "history.json"), "keep even in staging");
            File.WriteAllText(Path.Combine(extra, "saved-video.mp4"), "keep this video");
            UpdateTransaction.CleanupCompletedStages();
            Check(File.ReadAllText(Path.Combine(extra, "history.json")) == "keep even in staging" && File.ReadAllText(Path.Combine(extra, "saved-video.mp4")) == "keep this video" && !File.Exists(Path.Combine(extra, "update.zip")), "Untracked history and videos inside an update stage are also preserved while obsolete updater files are removed");
            Check(!Directory.Exists(complete), "Confirmed updates remove their old staging files after the helper exits");
            Check(Directory.Exists(busy) && Directory.Exists(recovery) && Directory.Exists(prepared), "Running updater helpers and unresolved recovery transactions are retained");
            Check(new[] { "history.json", "settings.json", "queue.json", "saved-video.mp4" }.All(name => File.ReadAllText(Path.Combine(root, name)) == "keep " + name), "Update cleanup preserves history, settings, queue and downloaded media byte for byte");
        }
        finally { UpdateTransaction.RootOverride = previous; if (Directory.Exists(root)) Directory.Delete(root, true); }
    }
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); Console.WriteLine("PASS: " + message); }
}
