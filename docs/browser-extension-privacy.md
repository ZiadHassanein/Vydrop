---
title: Vydrop browser extension privacy
description: What the Vydrop browser companion reads, transfers to the local app, and stores.
---

# Browser extension privacy

Updated October 23, 2026. Maintainer: [Ziad Hassanein](https://github.com/ZiadHassanein). [Support and privacy questions](https://github.com/ZiadHassanein/Vydrop/issues).

The optional companion connects your browser to the Vydrop desktop app on your Windows computer. You choose the video, format and quality, then explicitly press Download to start that output in the app's saved folder. Sending only a link opens the app for review. Older app versions retain that review workflow.

## Information used

- On HTTP(S) pages, local scripts identify visible playing video elements and nearby video links or public video identifiers to place the Vydrop button. They do not read password fields, cookies, clipboard contents, account APIs, or media response bodies.
- Clicking the video button, a popup action, or the context menu can transfer the selected page/video URL and source context to Vydrop through the browser's native messaging API. URLs can contain private or temporary access tokens; send only links you intend to save locally. On LinkedIn, a user click may open the selected post in a temporary background tab to find that video's media URL; the tab is closed after lookup unless you navigate it elsewhere.
- Optional media discovery requests separate browser permissions. It observes media response URLs and response types only after you enable it. Results remain in browser memory, are bounded, expire after 90 seconds, and are cleared on navigation or background-worker restart. Choose a result to send it to the desktop app. You can revoke those permissions in your browser.
- The copy action writes the selected link to the system clipboard. The extension does not read existing clipboard contents.

## Destination and retention

The alarms permission checks the extension’s own local update descriptor approximately once a minute. An updated bundle is hashed locally before the extension reloads itself. These checks do not contact websites or servers, launch Vydrop, read video links or refresh browser tabs.

Vydrop has no analytics, advertising, sale of data, or developer-operated collection service. The extension sends selected URLs to the local desktop app, not to a Vydrop server. Mozilla's consent categories for this local transfer are **browsing activity** and **website content**.

The desktop app contacts the selected media service to inspect or download your requested content, and the usual network information is visible to that service. Desktop tool and app updates contact their documented download providers. The companion does not import your browser account or sign-in cookies.

Desktop queue jobs, their source URLs, preferences, and a recovery backup are saved in your Windows user profile until removed or replaced. Stable data uses `%LocalAppData%\YouTubeDownloader`; previews use `%LocalAppData%\ViFreePreview`. Downloaded files stay in the folder you select. Uninstalling the app preserves these files and preferences. If you want to erase queue history completely, close Vydrop and remove its `queue.json` and `queue.json.backup` files; do not delete unrelated files.

## Your choices

Ordinary Vydrop downloads work without a browser extension. Remove the extension or use **Disable connection** in Vydrop to disconnect it. Denying optional discovery access leaves normal page/player actions available. Firefox 140 and later show the required local-transfer categories during installation, which you can decline by cancelling installation.

Vydrop cannot access DRM-protected content, and signing into a website does not grant the desktop app access to private content. Only save content you have permission to download.


Companion 1.5.9 adds its own loaded extension version and browser family (Chrome, Edge or Firefox) to an explicit local health request. The desktop stores a small local version report bound to its executable and build so setup can identify old or unconfirmed companions. No browser profile, browsing history, cookies or account data is read. These reports remain on this PC and are not sent to a developer-operated service.
