---
title: "Vydrop: Free Windows Video Downloader for the US"
description: "Save public videos or audio on Windows with Vydrop. Free and open source, with playlists, subtitles and Chrome, Edge or Firefox integration."
lang: en
language_code: en-US
locale: en_US
dir: ltr
alternate: true
english: /
arabic: /ar/
home: true
local_guide: true
---

# Vydrop: a free video downloader for Windows

[Download Vydrop for Windows](https://github.com/ZiadHassanein/Vydrop/releases/download/v4.4.17/Vydrop-v4.4.17-win-x64.zip) · Windows 10/11 x64 · v4.4.17

Vydrop 4.4.17 is a free, open-source video and audio downloader by Ziad Hassanein. This guide uses US English. The desktop app runs on Windows 10 or 11, x64, and its interface supports English and Arabic. The same official download is used worldwide.

## Get the portable Windows app

Download the ZIP from the official GitHub release, extract the entire folder, and open **Vydrop.exe** from the extracted folder. Keep its **Browser** folder if you want the optional extension. The .NET runtime is bundled. On first use, missing media tools are downloaded and verified automatically; allow time for preparation and enough disk space for the tools and your media.

To check Windows compatibility, open **Settings → System → About** and look for an x64-based processor. Vydrop does not provide a native macOS, Linux, Android or iPhone app.

## Save a video for offline viewing

1. Copy the video's own public URL, rather than a social network's homepage.
2. Paste it into Vydrop. The Download button shows the checking state while the app inspects available streams.
3. Choose **Video** or **Audio**, the format and an available quality. Choose the folder where you want to save the file.
4. Click **Download**. Use **Queue** for several links or choose specific entries from a playlist.
5. After completion, open the output folder from the completed action, queue or **Recent downloads**.

<h2 id="formats">Choose a format that fits your source</h2>

| Task | Available choices | What determines the result |
| --- | --- | --- |
| Keep the video | MP4, MKV or WebM | The inspected video and audio streams must fit the container. |
| Save only audio | MP3, M4A, Opus, WAV or FLAC | The original audio quality and the selected encoding. |
| Save captions | SRT or VTT | Subtitle languages exposed by the source video. |

A larger bitrate does not recover detail missing from the source. WAV and FLAC do not turn compressed audio into a higher-quality original. Vydrop offers measured or inspected choices; an unknown audio source may offer Automatic.

<h2 id="extension">Use Chrome, Edge or Firefox</h2>

In Vydrop, open **Settings → Browser integration → choose your browser → Set up**. Then install the included companion 1.5.14:

- **Chrome:** open chrome://extensions, enable Developer mode, then load the extracted **Browser/chromium** folder.
- **Edge:** open edge://extensions, enable Developer mode, then load **Browser/chromium**.
- **Firefox:** open about:debugging#/runtime/this-firefox, choose Load Temporary Add-on and select **Browser/firefox/manifest.json**. This temporary installation must be loaded again after Firefox restarts.

Allow access to the websites you use and refresh existing video tabs. **Download by Vydrop** appears inside the playing video’s top-right corner. Choose Video or Audio, format and quality, then confirm Download to start the output in the saved folder, including when the app is closed. Older app versions open the review workflow.

See the [illustrated browser guide](../browser-extension-setup.md), [supported sites](../supported-sites.md) and [security details](https://github.com/ZiadHassanein/Vydrop/blob/main/SECURITY.md). Only save media you have permission to download. Private, sign-in-only and DRM-protected media are unsupported.

## Frequently asked questions

### Can Vydrop download videos from sites other than YouTube?

Yes. Vydrop uses yt-dlp for public pages from 1,000+ supported sites, including TikTok, Facebook, Vimeo, X/Twitter, Reddit, Dailymotion and Twitch clips. Direct MP4/MP3 links also work. Site changes and access restrictions can prevent a particular download.

### Is there a free open-source Windows downloader with Arabic?

Yes. Vydrop is free under the MIT license, runs on Windows 10/11 x64, and includes English and Arabic interfaces with light and dark themes. The .NET runtime is bundled; missing media tools are prepared automatically.

### Does Vydrop download TikTok, Facebook, Vimeo or X videos?

Yes, when the public video is supported by yt-dlp and available without signing in. Paste the video’s own link, not the homepage. The extension can resolve a playing video, but custom players and site restrictions can limit detection.

### Can I download a video playing in my browser?

Use the optional Chrome, Edge or Firefox companion. Click Download by Vydrop inside the playing video, choose Video or Audio, format and quality, then confirm Download. Vydrop starts that output in your saved folder, even if the app was closed. Older app versions open a review screen. Detection varies by website.

### How do I save a YouTube playlist as MP3 on Windows?

Paste a public playlist link, choose Whole playlist, select the entries, choose Audio → MP3 and an available audio quality, then choose Download. The app processes the selected entries in order. Available quality depends on the original audio.

### Does Vydrop need an account, API key or subscription?

No. Vydrop needs no account, API key or paid subscription. It contacts media sites and official update servers when needed. It does not import browser cookies, bypass sign-in or decrypt DRM. Only download media you have permission to save.

### What happens when a website changes?

Vydrop checks for verified yt-dlp and Deno updates once daily when starting a download and can try one compatible update-and-retry. A site may still require an upstream fix. Use Settings → Update downloader for a manual check; application updates are separate.
