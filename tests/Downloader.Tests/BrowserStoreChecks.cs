using System.Text.Json;
using YouTubeDownloader;

internal static class BrowserStoreChecks
{
    internal static void Run()
    {
        const string chrome = "abcdefghijklmnopabcdefghijklmnop", edge = "pppppppppppppppppppppppppppppppp";
        string Catalog(string c = chrome, string e = edge, string f = "https://addons.mozilla.org/firefox/addon/vydrop/") => JsonSerializer.Serialize(new { format = 1, chromeId = c, edgeId = e, firefoxListingUrl = f });
        var valid = BrowserStoreCatalog.Parse(Catalog());
        Check(valid.Address("Chrome") == "https://chromewebstore.google.com/detail/" + chrome && valid.Address("Edge") == "https://microsoftedge.microsoft.com/addons/detail/" + edge, "Assigned store IDs produce official store addresses");
        Check(valid.Address("Firefox") == "https://addons.mozilla.org/firefox/addon/vydrop/" && valid.Address("Other") == "", "Firefox uses the validated listing and unknown browsers fail closed");
        var empty = BrowserStoreCatalog.Parse(Catalog("", "", ""));
        Check(new[] { "Chrome", "Edge", "Firefox" }.All(b => empty.Address(b) == ""), "Unpublished listings never produce fabricated download links");
        foreach (string json in new[] { Catalog("BAD"), Catalog(e: "../bad"), Catalog(f: "https://addons.mozilla.org.evil.test/firefox/addon/vydrop/"), Catalog(f: "http://addons.mozilla.org/firefox/addon/vydrop/"), Catalog(f: "https://addons.mozilla.org/firefox/addon/vydrop/?next=bad"), Catalog(f: "https://evil@addons.mozilla.org/firefox/addon/vydrop/"), "[]", "{}", Catalog().Replace("\"format\":1", "\"format\":2") })
        {
            bool rejected = false;
            try { BrowserStoreCatalog.Parse(json); }
            catch (Exception ex) when (ex is InvalidDataException or JsonException or InvalidOperationException or KeyNotFoundException) { rejected = true; }
            Check(rejected, "Invalid or untrusted store configuration is rejected");
        }
        Check(BrowserIdentity.ChromiumIds.Contains(BrowserIdentity.ChromiumId) && BrowserIdentity.FirefoxId.Contains("vifree", StringComparison.Ordinal), "Legacy extension identities remain allowed");
        string folder = Path.Combine(Path.GetTempPath(), "Vydrop-install-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        string marker = Path.Combine(folder, "installation.json"), exe = Path.Combine(folder, "Vydrop.exe");
        try
        {
            Check(!BrowserStores.IsInstalled(exe), "Portable copies do not claim an installer registration");
            File.WriteAllText(marker, JsonSerializer.Serialize(new { format = 1, kind = "installer", channel = ReleaseChannel.IsPreview ? "preview" : "stable" }));
            Check(BrowserStores.IsInstalled(exe), "An installed copy uses its channel-specific marker");
            File.WriteAllText(marker, "{\"format\":1,\"kind\":\"installer\",\"channel\":\"other\"}");
            Check(!BrowserStores.IsInstalled(exe), "A marker from another channel cannot activate store onboarding");
            foreach (string invalid in new[] { "[]", "{}", "broken", new string('x', 4097) })
            {
                File.WriteAllText(marker, invalid);
                Check(!BrowserStores.IsInstalled(exe), "Invalid installation markers fail closed");
            }
        }
        finally { File.Delete(marker); Directory.Delete(folder); }
        Console.WriteLine("Browser store and installation marker checks passed.");
    }
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); Console.WriteLine("PASS: " + message); }
}
