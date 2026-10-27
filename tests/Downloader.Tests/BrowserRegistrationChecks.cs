using System.Text.Json;
using YouTubeDownloader;

internal static class BrowserRegistrationChecks
{
    internal static void Run()
    {
        string app = @"C:\Apps\Vydrop\Vydrop.exe";
        string Chromium(string path, object origins, object name = null) => JsonSerializer.Serialize(new
        {
            name = name ?? BrowserIdentity.HostName, type = "stdio", path, allowed_origins = origins
        });
        var origin = new[] { "chrome-extension://" + BrowserIdentity.ChromiumId + "/" };
        Check(BrowserRegistration.MatchesManifest(Chromium(app, origin), app.ToUpperInvariant(), false), "Connection status recognises the existing Chromium identity and case-insensitive app location");
        Check(!BrowserRegistration.MatchesManifest(Chromium(@"C:\Old\Vydrop.exe", origin), app, false), "A connection to an old app folder requires reconnecting");
        Check(!BrowserRegistration.MatchesManifest(Chromium(app, new[] { "chrome-extension://unrelated/" }), app, false), "An unrelated extension identity cannot appear connected");
        Check(!BrowserRegistration.MatchesManifest(Chromium(app, origin, 42), app, false) && !BrowserRegistration.MatchesManifest("[]", app, false), "Malformed native-host properties are safely treated as disconnected");
        string firefox = JsonSerializer.Serialize(new { name = BrowserIdentity.HostName, type = "stdio", path = app, allowed_extensions = new[] { BrowserIdentity.FirefoxId } });
        Check(BrowserRegistration.MatchesManifest(firefox, app, true) && !BrowserRegistration.MatchesManifest(firefox, app, false), "Firefox status checks its own retained add-on identity");
        Check(!BrowserRegistration.IsEnabled(Path.Combine(AppContext.BaseDirectory, "not-installed-" + Guid.NewGuid().ToString("N") + ".exe")), "A missing executable is disconnected without creating a registration");
        string root = Path.Combine(AppContext.BaseDirectory, "automatic-browser-" + Guid.NewGuid().ToString("N"));
        string registered = null; int calls = 0;
        bool Connected(string path) => registered == path;
        void Enable(string path) { calls++; registered = path; }
        Check(BrowserRegistration.EnsureAutomatic(app, root, Connected, Enable) && calls == 1, "First normal launch registers the native connection automatically");
        Check(BrowserRegistration.EnsureAutomatic(app, root, Connected, Enable) && calls == 1, "An existing correct connection needs no registry rewrite");
        string moved = @"C:\Moved\Vydrop\Vydrop.exe";
        Check(BrowserRegistration.EnsureAutomatic(moved, root, Connected, Enable) && calls == 2 && registered == moved, "Moving the app automatically repairs the connection to its current path");
        BrowserRegistration.SetAutomaticDisabled(root, true);
        Check(!BrowserRegistration.EnsureAutomatic(app, root, Connected, Enable) && calls == 2, "An explicit Disable connection choice survives restarts");
        BrowserRegistration.SetAutomaticDisabled(root, false);
        Check(BrowserRegistration.EnsureAutomatic(app, root, Connected, Enable) && calls == 3, "Explicit reconnection restores automatic maintenance");
        Check(!BrowserRegistration.EnsureAutomatic(app, root, _ => false, _ => throw new UnauthorizedAccessException()), "Blocked registry writes cannot crash ordinary app startup");
        Console.WriteLine("12 browser registration checks passed.");
    }

    private static void Check(bool value, string message)
    {
        if (!value) throw new Exception(message);
        Console.WriteLine("PASS: " + message);
    }
}
