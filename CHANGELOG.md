# Changelog

All notable user-facing changes are recorded here. Use the versioned release links below for published Windows packages.

## [Unreleased]

### Changed
- Label the English contact menu “Contact me” and the queue action “Start download” (بدء التنزيل).
- Load video titles above queued links without blocking downloads; keep links usable when a title cannot be fetched.
- Use one separator above the queue footer and keep separators between queued videos.
- Highlight moved or deleted recent files with a readable warning and disable their folder action, including files removed while the history window is open.

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
[4.2.4]: https://github.com/ZiadHassanein/Vydrop/releases/tag/v4.2.4
[4.2.3]: https://github.com/ZiadHassanein/Vydrop/releases/tag/v4.2.3
[4.2.1]: https://github.com/ZiadHassanein/Vydrop/releases/tag/v4.2.1

## ملخص بالعربية

يعالج الإصدار **4.2.4** مشاكل الإغلاق وحالات قائمة التنزيل وتحديث ألوان التباين، ويحسّن أداء اختيار الفيديوهات في القوائم الكبيرة. تتضمن الحزمة إضافة **1.3.6** برسائل استعادة عربية وإنجليزية أوضح. يبقى التصميم الحالي وتوافق الإعدادات والسجل والربط مع الإصدارات القديمة محفوظًا. تحتاج النسخ حتى 4.2.1 إلى تنزيل الحزمة الجديدة يدويًا مرة واحدة بعد تغيير اسم المستودع.
