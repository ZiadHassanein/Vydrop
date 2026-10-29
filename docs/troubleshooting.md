---
title: "Vydrop Troubleshooting: Video, Browser and Downloads"
description: "Fix Vydrop video checks, browser Try again errors, unavailable formats, subtitle issues and missing downloads with practical Windows recovery steps."
lang: en
dir: ltr
alternate: true
english: /troubleshooting.html
arabic: /ar/troubleshooting.html
last_reviewed: 2026-10-19
---

# Troubleshoot Vydrop on Windows

Start with the latest [official Windows package](download.md) and the exact video's own public link. A working browser page does not always expose a stream Vydrop can download. These steps distinguish the app connection, video detection, media extraction and saved file location.

## The extension says Try again, or the app does not open

1. Open the Vydrop copy you intend to use. Choose **Settings → Browser integration**, select your browser, then choose **Set up**.
2. Keep that executable in its permanent folder. Moving or deleting it leaves the browser pointing to an old app location.
3. Confirm the companion is enabled and loaded from the matching package's **Browser\chromium** folder, or **Browser\firefox\manifest.json** in Firefox.
4. After reloading an extension, refresh the video page and play the video again. Try its Vydrop button or the extension's toolbar action.
5. Close the setup dialog before sending a video. The app also needs to finish or cancel any active download before accepting another link.

Once the native connection is registered, it is designed to start Vydrop when closed. A browser button appearing is not proof that this connection works. Follow the [browser-specific illustrated setup](browser-extension-setup.md), including the real send-to-app test.

## The video button is missing

The overlay follows a visible playing video. Play the video, keep it in view, and refresh after an extension update. Check that the companion has access to that site. An iframe, custom player or multiple visible videos can prevent unambiguous detection. Use the toolbar action on the video's own page, or paste its share link into Vydrop.

## Could not check video

Use the video's own link instead of a profile, feed or website homepage. Check that it is public and reachable without signing in. Check your connection, then use **Settings → Update downloader** and retry once. A recent site change may require an upstream yt-dlp fix; repeated retries cannot remove site restrictions.

For LinkedIn, use the companion on the playing video. Some LinkedIn post URLs cannot be extracted directly even though the browser can play the video. Detection may still fail on private or unsupported players. Vydrop does not import your login session.

## A format or quality is unavailable

Vydrop lists choices from the detected source. A low-resolution source does not become HD by selecting a different container. MP4/MKV/WebM describe the file container. Audio conversion targets such as MP3 bitrate do not restore missing source detail. See the [MP3 guide](youtube-to-mp3-windows.md) and [site limitations](supported-sites.md).

## No subtitles are listed

Only captions exposed by the source can be saved. For a playlist, choose **Get subtitle languages**; those results come from the first available video and may differ for other entries. **SRT** and **VTT** choose the sidecar file format. Vydrop does not generate new captions by speech recognition. See [subtitle instructions](youtube-subtitles-srt.md).

## A completed file cannot be opened

Recent downloads records the saved path. If the file was renamed, moved or deleted outside Vydrop, that old path is unavailable. Check your selected download folder or Windows Recycle Bin. The app cannot recover deleted media; download again only if the source is available and you have permission.

## Report a reproducible problem

[Open a download problem report](https://github.com/ZiadHassanein/Vydrop/issues/new?template=download-problem.yml) with the app version, Windows version, public video URL and steps. For browser issues, include the browser and companion versions. Redact personal paths and never post private URLs, cookies, passwords or tokens. Report security vulnerabilities through the [private security channel](https://github.com/ZiadHassanein/Vydrop/security/advisories/new).

[About the app and its limitations](about.md) · [Download and update](download.md) · [Documentation home](index.md)
