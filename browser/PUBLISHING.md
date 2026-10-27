# Browser store publication handoff

Prepared October 27, 2026 for companion 1.5.12. Packages are upload candidates, not approved or signed extensions. Public release links remain unchanged.

## Automatic setup prerequisite

The app installer already registers the native connection. The store catalog is empty, and publisher accounts have not been created. First-time extension delivery therefore still needs store registration, real assigned IDs, approval and Firefox signing. Chrome's supported Windows external installation also includes a one-time enable confirmation; restarting alone cannot enable an unpublished extension. See [Chrome installation rules](https://developer.chrome.com/docs/extensions/how-to/distribute/install-extensions) and [Firefox signing requirements](https://extensionworkshop.com/documentation/publish/signing-and-distribution-overview/).

Once approved listings are configured, the existing installer handoff opens the selected store directly. The user approves the extension without Developer mode, folder selection or loading an unpacked package. `scripts/build-installer.ps1 -RequireStoreListings` prevents accidentally building that distribution with missing listings. It does not replace store approval or browser-installed package testing.

## What the publisher must provide

| Store | Publisher action | Return to the maintainer |
|---|---|---|
| Chrome Web Store | [Register a developer account](https://developer.chrome.com/docs/webstore/register/), pay its one-time registration fee, complete account/identity/contact verification | Assigned 32-character extension ID and draft/listing URL |
| Microsoft Edge Add-ons | [Enroll in Partner Center](https://learn.microsoft.com/en-us/microsoft-edge/extensions-chromium/publish/create-dev-account) with a Microsoft account; Edge extension registration has no fee | Assigned 32-character extension ID and draft/listing URL |
| Firefox Add-ons | Sign in to [AMO Developer Hub](https://addons.mozilla.org/developers/), create a public listing and submit for Mozilla signing/review | Official listing URL and Mozilla-signed add-on |

Choose a public publisher name and verified support email; confirm whether publishing as an individual or an organization. Complete any store-requested identity/trader verification, payments, 2FA and agreements yourself. No password, recovery code or exported private signing key is needed by this project.

For Windows Authenticode signing, optionally provide access to your code-signing certificate by thumbprint and its timestamp service. Browser store signing and Windows executable signing are separate. The installer currently remains unsigned.

## Upload candidates and listing copy

```powershell
./scripts/package-browser.ps1 -StorePackages
```

Upload the corresponding `dist/Vydrop-browser/Vydrop-{chrome,edge,firefox}-unsigned.zip`. Each archive has its manifest at the root. The Chromium development public key is omitted in store archives so the assigned store identity can be recorded explicitly. The legacy unpacked key is retained in local packages. Keep Firefox's existing stable ID `vifree@ziadhassanein.github.io`; do not publish the separate preview identity as stable.

Select desktop Firefox as the AMO target. Do not enable Android distribution: Vydrop requires the Windows native app. The manifest's Android minimum 142 only expresses the data-consent API minimum; it does not add mobile native messaging support.

**Name:** Vydrop

**Short description:** Choose a video's format and quality, then download with Vydrop for Windows.

**Long description:** Vydrop connects your browser to the free, open-source Vydrop desktop app for Windows 10/11 x64. Choose the selected playing video using its Vydrop button or toolbar popup, pick a format and quality, and press Download to start that output in the app's saved folder. Link-only sending opens the app for review; older app versions retain that review workflow. Test connection checks the browser-to-app link. The companion can open the installed app when it is closed. An optional media-discovery action can find publicly accessible media streams after you grant its separate permissions. Vydrop does not bypass DRM or website access restrictions, and support depends on the video service. The desktop app is required. Available in English and Arabic. No advertising or analytics.

**Homepage/support:** https://ziadhassanein.github.io/Vydrop/ and https://github.com/ZiadHassanein/Vydrop/issues

**Privacy:** publish and confirm `https://ziadhassanein.github.io/Vydrop/browser-extension-privacy.html` before submitting that URL. Source policy is [browser-extension-privacy.md](../docs/browser-extension-privacy.md). Supply the chosen public support email wherever a store requires email; the issue tracker does not replace email verification.

Supply current screenshots showing the real player button, popup, and desktop review in the dimensions each dashboard requests. Include the square icon from the upload package. Do not submit screenshots of another browser's store or advertise unverified universal download support.

## Permission/privacy declarations

- `nativeMessaging`: transfers explicitly selected URL/source context to the local Vydrop app and checks connectivity.
- HTTP(S) content scripts: locate visible playing videos and nearby public links in frames, position the user-invoked button. Website access is broad because users choose videos on arbitrary supported sites. No password-field or cookie reading.
- `activeTab`/`scripting`: inspect the user-selected tab/player or selected LinkedIn post on explicit action when a content script cannot provide the link.
- `contextMenus`: user-invoked link sending. `clipboardWrite`: copy the selected video link; there is no clipboard reading.
- Optional `webRequest` and optional HTTP(S) hosts: off until granted for bounded media response URL/type discovery. No response bodies, account import or cookie API permission.
- No remotely hosted executable code, ads, analytics or Vydrop collection endpoint. Selected URLs can contain personal tokens and move outside the browser into the local app. Disclose local browsing activity/website content processing rather than claiming that no data is handled. Queue/source URLs persist locally, including in a recovery backup.
- Firefox declares required `browsingActivity` and `websiteContent` with minimum Firefox 140, using its built-in installation consent. [Mozilla explicitly includes native messaging in disclosure requirements](https://extensionworkshop.com/documentation/develop/best-practices-for-collecting-user-data-consents/).

## Review and release order

1. Prepare draft listings/uploads to obtain assigned Chrome and Edge IDs. Do not publish yet.
2. Fill `browser/store-listings.json` with real IDs/listing URL. Rebuild the desktop app and installer. Verify both new IDs and retained legacy IDs are accepted by the native host; do not substitute a guessed ID.
3. Publish the privacy page, upload accurate listing assets/declarations, and submit packages for review/signing. Provide a test Windows installer and simple test steps to reviewers.
4. Test each approved/store-installed extension: health check, app open/closed, link review, queue, browser restart, app upgrade and uninstall. Check optional discovery refusal/revocation. Chrome/Edge external prompts are not proven until these actual identities can be installed.
5. Only then release the installer and switch public download/setup links to the store flow. Retain local loading under troubleshooting and keep older app protocol behavior.

Reviewers: install the candidate Windows app, add the corresponding companion, open a public playable video, choose its format and quality, and press Download. Verify the selected output starts in the saved app folder, including when the app was initially closed. Link-only sending should open a review; older hosts should keep their version-1 review fallback. No account or paid media service is required for this test. Do not use private or DRM-protected media as an acceptance fixture.
