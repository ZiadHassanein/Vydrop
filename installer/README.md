# Windows installer and browser-store handoff

Build from the repository root with PowerShell 7 and .NET 10:

```powershell
./scripts/build-installer.ps1 -Version 4.4.3-preview.4 -Preview
# For a stable release candidate, omit -Preview and use a three-part version.
```

The script verifies or downloads the pinned, publisher-signed portable Inno Setup 6.7.3 compiler. `-CompilerPath` accepts an existing compiler. `-UseExistingPackage` validates package hashes and the compiled store-catalog hash before reusing a desktop package. Output is `dist/installers/`, including SHA256 and signing-status metadata. Builds are unsigned unless a publisher certificate is provided separately; never describe an unsigned build as trusted or signed.

The setup is per-user and needs no administrator rights. Default app locations are `%LocalAppData%\Programs\Vydrop` and `%LocalAppData%\Programs\Vydrop Preview`. It installs Start menu and uninstall entries, optionally a desktop shortcut, and generates native manifests for the installed executable. Native messaging is registered for Chrome, Edge and Firefox in both registry views. The final wizard action opens **Browser extension setup**; silent installs do not launch the app.

Preview and stable have separate app IDs, mutexes, native-host names, Firefox IDs, settings and queues. Existing unpacked Chromium identity and v1 native-message compatibility are retained. Upgrades reuse the same app ID/path and require the running channel to close. There are no startup tasks, browser force-install policies, or administrator-wide modifications.

Uninstall removes only native registration values still pointing to this installation. A connection redirected to another app copy is preserved. Generated integration files are explicitly removed; unrelated files in the app folder, settings, queues and downloaded media remain. Users can reconnect a portable copy from its integration screen.

## Store configuration

`browser/store-listings.json` is embedded into the desktop app. Fill it only with the assigned official Chrome/Edge IDs and the published Firefox URL. IDs must be 32 lowercase characters in `a`–`p`; Firefox URLs must be on `https://addons.mozilla.org/.../firefox/addon/<slug>/`. Blank means unpublished. Rebuild the app and installer after editing this file.

Configured IDs are added alongside the legacy ID to both native-host allowlists and native caller validation. Stable setup opens the selected official listing so the user can approve installation without Developer mode or choosing an extension folder. This per-user installer registers native messaging; it does not register a machine-wide external extension. Firefox uses its official signed listing. Preview builds deliberately do not open stable store listings.

For a release that must use store listings, build with `-RequireStoreListings`. This fails before packaging if any listing is missing or the build is a preview. The flag validates configuration; it does not prove store approval or install an extension. Chrome on Windows requires a Chrome Web Store package and an enable confirmation even for external installation. See [Chrome's supported installation methods](https://developer.chrome.com/docs/extensions/how-to/distribute/install-extensions). A restart alone cannot install the current unpublished companion.

The current catalog is empty. Store buttons explain the pending release instead of opening invented links. **Manual setup and troubleshooting → Load the local companion** works until the listings are available. See [publication checklist](../browser/PUBLISHING.md).

## Verification

```powershell
dotnet run --project tests/Downloader.Tests -c Release -- --browser-stores
dotnet run --project tests/Downloader.WpfTests -c Release -- --store-setup
dotnet run --project tests/Downloader.WpfTests -c Release -- --browser-setup
node --test tests/browser.test.cjs tests/browser-video.test.cjs tests/browser-errors.test.cjs tests/browser-improvements.test.cjs
```

`scripts/test-installer.ps1` exercises a preview installer in a fresh test directory: real install, in-place upgrade, automatic native registration, cold start of the installed app, ownership-aware uninstall, unrelated-file/data preservation and restoration of pre-existing preview registration. It refuses to run over an existing installer or queue. Run it only when Vydrop is closed. This does not substitute for clean Windows 10/11 VM and approved-store tests.

Pass `-PlaywrightPath <absolute Playwright module directory>` to include a real native-messaging round trip from an isolated, headless Edge companion. The companion uses the retained unpacked identity, not a simulated native API. The browser profile stays separate from signed-in user profiles; no OS pointer input is used. Store installation/approval is a later test with actual published IDs.
