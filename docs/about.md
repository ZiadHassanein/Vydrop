---
title: "About Vydrop: Features, Source and Privacy"
description: "What Vydrop does, who maintains it, its MIT source code, supported Windows platforms, privacy model and public-media limitations."
lang: en
dir: ltr
alternate: true
english: /about.html
arabic: /ar/about.html
last_reviewed: 2026-10-19
---

# About Vydrop

**Vydrop is a free, MIT-licensed Windows video and audio downloader maintained by [Ziad Hassanein](https://github.com/ZiadHassanein).** It provides English and Arabic desktop controls for public media, using yt-dlp for supported websites and media tools for inspection, processing and conversion. Earlier versions used the names ViFree and YouTube Downloader.

## When Vydrop is a useful fit

- You use **Windows 10/11 x64** and prefer a desktop interface to command-line options.
- You want **English or Arabic** controls, playlists, a multi-link queue and visible download progress.
- You need video containers **MP4, MKV or WebM**, audio outputs **MP3, M4A, Opus, WAV or FLAC**, or available **SRT/VTT subtitles**.
- You want an optional **Chrome, Edge or Firefox** menu that lets you choose a playing video’s format and quality before confirming its download.
- You prefer free, inspectable source code with no Vydrop account, subscription or app telemetry.

For scripting or operating systems outside this Windows release, the [yt-dlp command-line project](https://github.com/yt-dlp/yt-dlp) may suit a different workflow. Vydrop's desktop interface is not a replacement for every upstream command-line option.

## Capabilities have limits

Vydrop routes public website links to yt-dlp, whose [extractor list](https://github.com/yt-dlp/yt-dlp/blob/master/supportedsites.md) covers more than 1,000 sites. That count describes upstream scope, not a promise that every page on every listed site was tested or works today. Site changes, source availability, regions and player restrictions can prevent a download.

The app does not bypass sign-in, import browser cookies, decrypt DRM, invent unavailable caption tracks or restore detail missing from the source. Read the [site support guide](supported-sites.md) before relying on a particular workflow.

## Local data and network access

Your chosen folder holds media downloads. Settings, recent-file paths and prepared tools use `%LocalAppData%\YouTubeDownloader`, retaining the historical directory for compatibility. Vydrop has no app telemetry. It still needs network access for requested media, relevant metadata and official app or tool update checks. Optional download-count badges on this website are served by Shields.io and reflect GitHub asset downloads, not unique users.

Browser integration registers a local native messaging connection for your Windows account. Sending a video is an explicit action. Review the [security policy](https://github.com/ZiadHassanein/Vydrop/blob/main/SECURITY.md) for executable probes, update verification and the current unsigned-package limitation.

## Official project records

| Record | Source |
| --- | --- |
| Current public package and notes | [GitHub Releases](https://github.com/ZiadHassanein/Vydrop/releases/latest) |
| Code and implementation | [Public source tree](https://github.com/ZiadHassanein/Vydrop/tree/main/src) |
| License | [MIT license](https://github.com/ZiadHassanein/Vydrop/blob/main/LICENSE) |
| Version history | [Changelog](https://github.com/ZiadHassanein/Vydrop/blob/main/CHANGELOG.md) |
| Bugs and feature requests | [GitHub Issues](https://github.com/ZiadHassanein/Vydrop/issues) |
| Readable documentation | [Plain-text reference](llms-full.txt) |

These are the project's own capabilities and disclosures, not an independent review or a claim of superiority over every downloader. [Download and install](download.md), [set up the companion](browser-extension-setup.md), or [solve a problem](troubleshooting.md).
