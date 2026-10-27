# Changelog

All notable user-facing changes are recorded here. Use the versioned release links below for published Windows packages.

## [Unreleased]

## [4.4.10] - 2026-10-27

### Improved
- Reuse successful non-live video previews for up to 45 seconds across at most eight sources, with isolated metadata copies and retained exact quality choices.
- Keep routine daily updater requests off the video/playlist preview path; actual downloads retain daily maintenance and compatible extractor-error recovery.
- Reuse the native host's already-verified tools without repeating discovery or validation between checks.
- Retain cancellation, cache expiry and eviction, immediate refresh after a download attempt, fresh download validation, automatic browser connection and complete-ZIP-only distribution.

## [4.4.9] - 2026-10-27

### Changed
- Distribute only the complete Windows ZIP, containing the app and Chromium/Firefox companion 1.5.6. Remove separate installer links and installer generation from the release workflow.
- Require exactly one verified ZIP in release publishing and website deployment. Retain automatic browser connection, visible first-use preparation and built-in updates for existing users.

## [4.4.8] - 2026-10-27

### Fixed
- Register and repair the current user's browser connection automatically on ordinary app startup, including portable first use and changed app paths. Preserve an explicit Disable connection choice.
- Move fresh-PC media-tool preparation into the visible desktop workflow instead of the browser host's short inspection deadline; retain the selected source context and require an explicit download choice.
- Show concise English/Arabic guidance for missing native connections, first-use preparation and older hosts, without restoring the failed-format fallback panel.
- Remove setup guidance pointing to the retired extension Test connection button.

### Updated
- Bundle Chromium and Firefox companion 1.5.6 in the complete ZIP and installer. Existing users can update the app and both companion folders together, then reload their extension.

## [4.4.7] - 2026-10-25

### Changed
- Simplify the extension popup to Open in Vydrop and Add to download queue; remove Test connection, Media-stream discovery and its optional observation permissions.
- Close a failed in-video format chooser and restore the original action without displaying the Retry/Open fallback panel or sending a download.
- Bundle Chromium and Firefox companion 1.5.5 with the app update.
- Keep the on-video download action visible before playback, while buffering or paused, and after the video ends; retain hidden-player, blocking-dialog and active-ad filtering.

## [4.4.5] - 2026-10-24

### Fixed
- Download the selected Reddit main video through its public master playlist when post metadata is blocked, preserving its canonical post and asset identity.
- Measure missing Reddit audio facts and select the verified audio rendition by ID so video downloads retain sound.
- Resolve LinkedIn feed cards without visible post links through their own post menu and public embed, matching the selected video's exact poster asset instead of a neighboring feed item.
- Keep toolbar and context-menu source identity, existing companion permissions, native-host identities and legacy single-video compatibility.

### Updated
- Bundle companion 1.5.3 for Chromium and Firefox. Install through the built-in updater to update the app and both companion folders while preserving settings, queue, history and media; reload the browser extension afterward.


## [4.4.4] - 2026-10-24

### Changed
- Update the app and both browser companions together with verified staging, rollback, preserved settings and downloads, and compatibility with the previous desktop-only updater.
- Remove browser selection from the installer, add branded artwork, and close a running app through its normal queue-saving shutdown path. Older versions receive a short retry message.
- Simplify Browser integration with shared animated browser tabs and collapsed connection help.
- Add companion 1.5.2's explicit single-video/whole-playlist choice, retaining existing native protocols and extension identities.
- Keep Dailymotion's containing watch-page handoff available when ads or navigation replace the clicked video element.
- Hide the download action on Dailymotion ad players and resolve content from its watch/embedded video ID instead of transient advertising streams.
- Resolve a selected JW Player's bounded public source configuration, including packed configuration strings, so supported video manifests can be inspected when the embed page itself is unsupported.
- Start the on-video action reliably in Chrome frames that expose a null custom-element registry.
- Name playlist folders after their real titles; migrate legacy ID folders and completion records without replacing media or merging different playlists.
- Center Download, place Cancel beside it, move Add to queue toward the sidebar, and add a localized Back button to Subtitles.
- Allow language switching during downloads without resetting progress or changing the active output request.
- Offer eight download connections for supported fragment and direct-range downloads while keeping existing defaults and queued jobs unchanged.
- Make the complete Windows ZIP the primary GitHub and website download, keeping the installer as an optional alternative and clarifying app/extension upgrades for existing users.

## [4.4.3] - 2026-10-23

### Updated
- Bundle browser companion 1.5.1 in both the Windows installer and portable ZIP, preserving extension IDs, existing permissions and the version-1 native-host fallback.
- Include the latest UI and queue recovery, resume and scheduling fixes while retaining compatible settings, history and saved jobs.

### Fixed
- Detect Reddit's open shadow-root players, keep the selected post's permalink, and avoid promoting comment GIFs or advertisements to the main video.
- Keep Twitch clip permalinks and measure missing stream metadata so video and audio formats remain available.
- Resolve Dailymotion player-only frames to the containing video page, including after navigation reuses the same frame URL.

### Limitations
- Site access restrictions still apply. Some Reddit pages require authentication, and the tested Krmizy video hosts are not certified as working.

## [4.4.2] - 2026-10-23

### Fixed
- Protect the last valid queue backup when recovering a damaged or missing primary file, and retain damaged originals for recovery.
- Validate saved queue contents before restoring or replacing them, including malformed playlists, duplicate jobs and invalid states.
- Resume a matching paused or interrupted download from Add download and the browser companion, preserving its job identity, partial files, progress and original per-job settings.
- Keep scheduled times and paused job states intact when saving a scheduled start fails.
- Make compact Details and filter-animation checks wait for the actual UI state instead of relying on fixed timer delays.

### Compatibility
- Retain queue format 1, existing settings and history, legacy browser identities, and exact version-1 native messaging fallback.
- Browser-store listings remain unpublished; the installer prepares the native connection, and browser installation still requires approval.

## [4.4.1] - 2026-10-23

### Changed
- Separate completed video and audio in Library from waiting, active, paused and failed work in Queue, with independent searches and relevant filters and actions.
- Refine the app with neutral graphite surfaces, consistent native outline icons, clearer transfer colors and contact links in Settings.
- Let Add download fill the center panel, align its action buttons and remove the modal Close control from workspace tabs.
- Offer the detected browser during installation and prepare the selected browser connection; browser approval and local extension loading remain required while store listings are pending.
- Refresh the English and Arabic screenshots, setup guides and Windows download links.

### Fixed
- Make the full Resume, Download, navigation and filter button surfaces clickable, including their padding.
- Open the active job in Queue after starting from Add download, clearing stale queue filters and searches while keeping completed files in Library.
- Preserve existing settings, queue snapshots, media history, partial downloads and older browser companion protocols.

## [4.4.0] - 2026-10-22

### Added
- Make the download manager the main workspace, with search, media and status filters, transfer controls and a selected-media inspector in Vydrop's updated visual identity.
- Choose video or audio format and quality from the browser menu, then start the confirmed download in Vydrop, including when the app is closed.

### Changed
- Keep Add download, Settings and History in the main workspace, with compact layouts, English/Arabic support, theme-aware artwork and accessible navigation.
- Clarify download preferences while retaining existing settings, queued-job snapshots, saved history and older browser protocols.
- Remove superseded hidden header and form controls, unused presentation members and the old runtime logo bitmap.

### Fixed
- Keep virtualized download rows stable through native window resizes, language and theme changes.
- Recover missing browser companion files from the app's bundled copy and keep setup status actionable.
- Keep scheduled stops from restarting a due queue, and preserve paused or interrupted jobs for recovery after reopening.
- Announce new completions through the visible History navigation and send transfer announcements to the visible activity panel.

## [4.3.0] - 2026-10-21

### Added
- A per-user Windows installer with automatic native-host registration, upgrade/uninstall support and a separate portable ZIP. Browser-store listings remain pending; local companion loading is available.
- Durable queue recovery, pause/resume, scheduling, per-job retries, optional speed/connection limits and compatible direct-MP4 acceleration.
- Companion 1.4.1 with connection testing, explicit queue handoff, optional media discovery and retained version-1/legacy-host fallback.
- A narrated 1080p browser-setup video with English/Arabic captions, chapter markers and responsive players in both illustrated guides.
- Keyboard shortcuts for the video link (Ctrl+L), recent downloads (Ctrl+J), queue (Ctrl+Shift+Q) and settings (Ctrl+,), with accessible shortcut hints.

### Changed
- Refine the shared outline icons, dialog headings, settings hierarchy and ready/error feedback while preserving the approved branding, crimson Download button and English/Arabic layouts.
- Give queue titles two lines and the full row width, group recovery actions beneath them and remove the duplicate separator above the footer.
- Theme advanced-option headers, checkboxes and the schedule date input; align download limits and show localized numeric validation without replacing valid saved settings.
- Compact the media output summary and improve recent-file path readability.

### Fixed
- Acknowledge browser requests reliably during app startup and retain compatibility with older app/extension transports.
- Center recent-download badge numerals by their visible outline rather than font line height.
- Restore the English “Contact me” label and apply the current title-bar theme to recent downloads.
- Keep schedule dates readable in dark mode and avoid success-colored feedback for cancelled downloads.

## [4.2.5] - 2026-10-19

### Changed
- Color the recent-download, language and theme header icons with the theme-aware app accent.
- Simplify browser setup to one action that connects the app, copies the correct path and opens the browser page, with manual recovery controls kept available.
- Make subtitle languages searchable by name or language code without changing the selected subtitle until a result is chosen.
- Allow two-line recent-file titles, tighten metadata spacing, expand the list for multiple files and mark missing files with strikethrough and a clear status badge.
- Add English/Arabic install, troubleshooting and project-information guides, visible provenance and breadcrumbs, consistent page entities and stricter crawl-output validation.
- Guide browser setup through app connection, browser-specific loading and a first video, with exact path-copy actions and persistent recovery feedback.
- Report whether the native connection points to this app, while keeping browser extension installation a separate manual step.
- Add a compact setup checklist to the English/Arabic illustrated guides, with current paths and troubleshooting for missing files or an unavailable browser.
- Remove the download-count explanation from the website and repository overview while retaining the release-download badge.

## [4.2.4] - 2026-10-18

### Added
- Publish US English, French, German and Spanish website guides, plus an Arabic terminology guide covering 22 countries. The app interface remains English and Arabic.
- Connect localized pages with reciprocal language links and include them in the sitemap and readable documentation reference.

### Fixed
- Skip the browser video overlay on XML, feeds and plain-text viewers.
- Make thumbnail teardown safe during shutdown without losing successful completion or saved-file history.
- Serialize queue progress, retries and terminal transitions; invoke cancellation callbacks outside the queue lock.
- Refresh system contrast colours in the main window and open dialogs, and keep selected Video/Audio controls distinguishable without changing their text styling.
- Include companion 1.3.6 with shared English/Arabic recovery messages for video detection, native connection and busy errors. Retain compatibility with earlier companions and native hosts.
- Use the current Change button name in save-folder recovery guidance.

### Changed
- Render only visible playlist rows while preserving checked choices, unavailable entries and keyboard navigation for large playlists.
- Remove unused UI members, provider events, translations and retired overview images; derive the download user-agent from the app version.
- Keep the current interface design, native connection identifiers and stored preferences/history format.
- Run checking feedback, shutdown, system contrast, playlist and error-presentation checks through the standard test command; wait for settled themes before static screenshots.

## [4.2.3] - 2026-10-18

### Changed
- Coordinate the Download button with the app's crimson palette and white lettering in both themes.
- Play a smooth two-second shine across the logo and Download button on hover or click.
- Show video/playlist checking inside the Download button with a subtle scanning video icon and an available Cancel action.
- Animate the completion check only after a successful download; respect reduced motion and hidden views.
- Keep Video/Audio label color and weight fixed when selected.
- Include companion 1.3.5 with improved YouTube placement, a compact V for crowded headers, and LinkedIn group/feed video selection and handoff.
- Probe compatible direct media for measured video and audio details before offering output choices.
- Use the current Vydrop app-update endpoint. Versions through 4.2.1 need one manual ZIP download after the repository rename.
- Refresh English/Arabic screenshots, release links and the readable documentation reference.

## [4.2.1] - 2026-10-17

### Fixed
- Clean completed app-update stages on a later startup while preserving recovery and active-helper stages.
- Retain active and previous media-tool selections and prune older owned versions and old quarantine folders.
- Reuse metadata inspected during a queued download; keep revalidation for older main-window choices.
- Report a busy browser connection without launching another app; retry listener construction failures.
- Offer manual release download for an incompatible or unverifiable update instead of blaming the connection.
- Make theme-button test dimensions tolerate physical-pixel rounding at fractional display scales.
- Use explicit file inputs for media validation and contain expected process-stop errors during cancellation.
- Translate the link, queue and update error messages that still appeared in English in the Arabic interface, and drop stale translations left over from earlier versions.
- Remove abandoned update preparations older than a week once no helper or parent process remains.
- Show the browser companion's popup and context-menu entries in Arabic when the browser interface language is Arabic.

### Added
- Bilingual multi-site guides, product facts, crawler hints, structured data, issue templates and a published documentation site.
- A localization test that keeps every interface string and user-facing error message translated.
- A 1200 × 630 social-sharing card using the approved logo.
- GitHub release download badges and a free per-version ZIP counter, without app telemetry. Counts include repeat and automated downloads.

### Changed
- Builds run the recommended .NET analyzers and code-style rules with warnings treated as errors; sources use file-scoped namespaces and an explicit line-ending policy.

[Unreleased]: https://github.com/ZiadHassanein/Vydrop/tree/main
[4.3.0]: https://github.com/ZiadHassanein/Vydrop/releases/tag/v4.3.0
[4.2.5]: https://github.com/ZiadHassanein/Vydrop/releases/tag/v4.2.5
[4.2.4]: https://github.com/ZiadHassanein/Vydrop/releases/tag/v4.2.4
[4.2.3]: https://github.com/ZiadHassanein/Vydrop/releases/tag/v4.2.3
[4.2.1]: https://github.com/ZiadHassanein/Vydrop/releases/tag/v4.2.1

## ملخص بالعربية

يتضمن الإصدار **4.3.0** مُثبّت ويندوز يجهّز ربط التطبيق تلقائيًا، ونسخة ZIP محمولة، وتحسينات لإرسال الفيديو من المتصفح وقائمة تنزيلات محفوظة قابلة للاستئناف والجدولة. أصبحت الأيقونات والعدادات وخيارات القائمة ورسائل التحقق أوضح في المظهرين وبالعربية والإنجليزية. تتضمن الحزمة إضافة **1.4.1** مع الحفاظ على الإعدادات والسجل وتوافق الربط السابق. تظل متاجر المتصفحات قيد التجهيز، وتتوفر خطوات تحميل الإضافة المحلية من داخل التطبيق.
