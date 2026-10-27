---
title: "Download Vydrop for Windows: Install and Update"
description: "Download the complete Vydrop Windows ZIP with the app and browser extensions, Check setup and safe updates."
lang: en
dir: ltr
alternate: true
english: /download.html
arabic: /ar/download.html
last_reviewed: 2026-10-24
---

# Download and install Vydrop for Windows

**Vydrop is a free, open-source video and audio downloader for Windows 10/11 x64.** Download the packaged app from the project's GitHub Releases page. The desktop interface supports English and Arabic.

[Download Vydrop 4.4.10 — Complete ZIP](https://github.com/ZiadHassanein/Vydrop/releases/download/v4.4.10/Vydrop-v4.4.10-win-x64.zip) · [Release notes](https://github.com/ZiadHassanein/Vydrop/releases/tag/v4.4.10)

The ZIP contains **Vydrop.exe**, **Browser/chromium** and **Browser/firefox**. Extract the entire archive; opening only the EXE inside the ZIP does not unpack the companion folders.

The complete ZIP includes companion **1.5.6** and the latest Reddit player, Twitch clip-format and Dailymotion link fixes. When updating an existing unpacked companion, reload it in your browser's Extensions page and refresh video tabs.

## Run the extracted app

1. Extract the complete ZIP to a permanent writable folder, such as **Documents\Vydrop**.
2. Open **Vydrop.exe** from the extracted folder. Keep **Browser** beside it if you want browser integration.
3. Paste a public video's own link. Wait for its title and available quality, choose Video or Audio, then choose **Download**.
4. For browser handoff, follow the [illustrated extension guide](browser-extension-setup.md).

The companion opens a format and quality menu. Confirm Download to start the chosen output in your saved folder; older app versions open a review screen. Vydrop does not import browser cookies or unlock private or DRM-protected media. See [supported sites and limitations](supported-sites.md).

## Choose the application package

| Item | What you need |
| --- | --- |
| Operating system | Windows 10 or Windows 11, x64 |
| Portable app | `Vydrop-v4.4.10-win-x64.zip`; extract the complete folder |
| .NET | Included in the package; no separate runtime installation |
| Media tools | Missing tools are prepared when needed; first use needs an internet connection |
| Browser companion | Optional Chrome, Edge or Firefox companion, inside the app's **Browser** folder |
| Account or payment | No Vydrop account, API key or subscription |

GitHub's **Code → Download ZIP** downloads the source code. Choose the named Windows ZIP under **Releases → Assets** for the ready-to-run app. There is no native macOS or Linux package in this release.

## Check the publisher and package

The official repository is **[ZiadHassanein/Vydrop](https://github.com/ZiadHassanein/Vydrop)**, maintained by Ziad Hassanein. Review its release notes and asset details before running a package. The current Windows executable is **not Authenticode-signed**. A checksum or GitHub asset digest can detect a changed download relative to that publisher; it is not independent proof that a package is safe. Read the [security policy](https://github.com/ZiadHassanein/Vydrop/blob/main/SECURITY.md) for the full trust boundary.

## Update an existing copy

Downloading a new ZIP does not change a running older copy. Close Vydrop, extract the complete new ZIP into the existing app folder and replace the app files, including the **Browser** folder. Keep your downloaded media and local app-data files. Compatible settings, queue and history remain in their existing storage location. Reload the companion and refresh video tabs. If you use a new folder, open Vydrop there once; its browser connection repairs automatically. Previously installed copies can use the built-in updater.

In **Settings**, choose **Check for updates**, then **Install**. The verified update replaces the app and both bundled browser companions together, restarts Vydrop, and preserves compatible settings, queue, history and downloaded media. Reload the extension in the browser and refresh video tabs. If an earlier copy still points to another companion folder, use **Browser integration → Set up** to reconnect the matching files.

Versions through **4.2.1** need one manual download after the repository was renamed from ViFree to Vydrop. Open the new **Vydrop.exe** and update any shortcut that still points to an older copy. Application updates and media-tool updates are separate: **Settings → Update downloader** checks the media tools, not the desktop application package.

Settings and recent-file history use `%LocalAppData%\YouTubeDownloader` for compatibility. Your media files remain in your chosen save folder. See [troubleshooting](troubleshooting.md) if the app cannot check a video or the companion shows Try again.

## Continue

[Save YouTube audio as MP3](youtube-to-mp3-windows.md) · [Download playlist entries](download-youtube-playlist-windows.md) · [Save subtitles](youtube-subtitles-srt.md) · [About Vydrop](about.md)


<figure><img src="images/screenshots/vydrop-light-en-download.png" width="1280" height="761" loading="lazy" alt="Add download opens in the main workspace. These are native 4.4.4 controls with example media."><figcaption>Add download opens in the main workspace. These are native 4.4.4 controls with example media.</figcaption></figure>
