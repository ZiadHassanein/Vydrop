# Vydrop

<!-- impeccable:product-schema 1 -->

## Platform

Native Windows desktop (WPF), with an HTML/CSS/JavaScript browser companion for Chrome, Edge and Firefox. This is an existing Windows application, not a web or mobile app.

## Users

People saving public video and audio on Windows, including English and Arabic users who send links from their browser or paste them into the app.

## Product Purpose

Inspect public media, choose an available output format and quality, save downloads to a chosen local folder, and manage a durable queue. Users must understand what is downloading, what needs attention, and how to resume work after reopening the app.

## Operating Context

The download manager is the default main screen, with a persistent top toolbar and a searchable download list. Add download, Settings and History replace the center workspace. Empty Add download and Settings use the available center width without reserving a blank inspector; an inspected Add download preview appears in the right inspector at wide sizes and inline in compact layouts. Settings has one workspace title and Close control, with flat section content. The toolbar and Settings show no version or release-channel labels. History remains accessible during transfers. Zero navigation and filter counts are hidden, and the status filters have deliberate separation.

The browser action sits inside the visible video's top-right corner. Its compact menu shows inspected format and quality choices, a selected-output summary and Download action. Confirming a choice uses the saved folder, persists the job in the local queue and starts it through the existing download path when idle; confirmed jobs wait when the app is busy. Older hosts retain a visible review-and-start-in-app fallback. The user requested visual mockups before implementation and then these operating-flow refinements.

The installer offers detected browsers, recommends the default browser on first install, and lets users defer setup. Its explicit browser choice hands off directly to the app and then the browser, reusing the installer-owned native connection. Upgrades default to deferring browser setup. Published listings open the official store; unpublished listings retain the honest local-loading flow. Opening Browser integration from Settings performs no setup actions. Browser setup presents a browser selector, one available next action and concise instructions. Both installed and portable apps offer local loading when no published store listing exists. A copied app can prepare its embedded, matching companion files in a local cache after the user starts setup; missing or unusable recovery content leads to the illustrated guide. Native app registration and a verified browser handshake are separate states.

Download speed in Settings shows the speed cap first. Advanced download options reveals 1, 2, 4 or 8 same-file connections, automatic retries and supported direct-MP4 acceleration. Preferences apply to new downloads; queued jobs retain their captured settings. Keep the existing KiB/s storage and unlimited sentinel. Add to queue gives each link independent Video/Audio, format and quality controls. New rows copy the preceding type, format and preferred resolution; each link discovers its own available choices automatically after URL editing settles. Pasted links remain separate drafts. Show a lightweight loading state while source discovery runs, then offer only the available formats, real resolutions and source-bounded audio bitrates. Reuse inspected metadata across output changes, with no Check formats button. Playlists use automatic per-video quality; lossless formats have no bitrate setting. Keep the saved folder and global transfer preferences without repeating them per row. Scheduling is removed; older saved start/stop times are cleared on opening while preserving jobs, completed files and their saved order. Download rows omit the manual reorder arrows.

## Capabilities and Constraints

- Existing supported outputs, playlists, available subtitles, media inspection, queue pause/resume and browser integration remain available.
- Preserve stable settings and queue storage compatibility, older browser/native-host protocol compatibility, and Windows 10/11 x64 support.
- Queue and history stay local. Downloading cannot continue after the app process exits.
- Display actual source metadata and measured transfer information. Do not invent file sizes, speed, time remaining, resume guarantees or connection counts.
- Do not extend this redesign into general document/program downloading, DRM access, account extraction or a new download engine.
- Keep existing English/Arabic language support, RTL behavior, light/dark themes, keyboard access and Windows high-contrast support.
- Queue-wide pause, cancel, engine halt and app closure retain control over confirmed browser work. Reopening the app does not silently resume older pending or interrupted jobs.
- Repeated and concurrent browser format checks reuse bounded inspection work. Dismissing the chooser clears its busy presentation and ignores late inspection results.
- Clearing or replacing the editor link cancels stale inspection feedback and ignores late results. Actual active-queue feedback and Cancel remain available inline even without a media preview.
- Ordinary opening of browser setup performs no connection registration, browser launch, clipboard write or recovery write. Only the explicit installer handoff begins the selected setup once. Explicit local setup verifies matching companion files before copying a loadable path; cache repair preserves unrelated files. Saved settings, queue formats and browser/native-host protocols retain their existing compatibility.

## Brand Commitments

Retain the Vydrop name, approved red V/download-arrow wordmark and recognizable red brand accents. The reference images establish a download-manager workflow and an on-video quality menu; they do not authorize copying another product's branding or visual assets.

The user selected the right-hand details-panel layout (concept A), but rejected the reference-like visual treatment. Keep that information layout while developing an original Vydrop style. Prioritize media identity, recognizable graphite navigation with restrained Vydrop red actions and clear, deliberately designed controls over a conventional dense file table. The old mockups are layout references, not approved visual designs.

## Evidence on Hand

- Current application views and tokens: `src/Downloader.Desktop/Views/` and `src/Downloader.Desktop/Themes/`.
- Existing companion: `browser/common/`.
- Queue restart tests demonstrate recovery after normal closure and process termination, byte resume and exact output integrity.
- User supplied two reference images and confirmed the manager-first structure, browser quality menu and mockup-first workflow.
- The final scoped review in `.impeccable/review/workspace-flow-v3.json` marks WF1–WF14 and the resolved source findings satisfied. Fresh native and browser fixture captures and named validation logs support that correction scope; `.impeccable/review/workspace-validation-v3.json` records the final stage results and limits.
- The later empty-editor, Settings, version-label and browser-setup review in `.impeccable/review/setup-workspace-ui.json` marks SW1–SW8 satisfied with a scoped ship disposition. `.impeccable/review/setup-ui-validation.json` records current native captures, targeted checks, restart/resume evidence and package status for that scope.
- The Download preferences clarification scope and its current disposition are recorded in `.impeccable/review/download-settings-clarity.json`; `.impeccable/review/download-settings-validation.json` records the focused native checks, correctly sized English/Arabic fixtures and preview status.
- The original comp-led mechanical workflow remains open at spec, with later gates pending. The scoped correction review does not certify original-comp fidelity or a complete mechanical build pass.

## Product Principles

- Make the current download and its next useful action immediately readable.
- Keep simple downloads simple; reveal advanced download controls when needed.
- Preserve recoverable user work across navigation, shutdown and upgrades.
- Show media identity and useful status instead of asking users to recognize raw URLs.

## Accessibility & Inclusion

Readable English and Arabic typography, correct bidirectional text, visible keyboard focus, named controls, state indicators beyond color, reduced-motion behavior and Windows accessibility conventions remain requirements.

## Current navigation layout

Put Add download, Library, Queue, History and Settings beside the Vydrop logo in a single toolbar, alongside language and theme controls. Show labels whenever their measured width fits and use localized icon tooltips only when needed. Browser integration lives in Settings, with Settings active while it is open. Keep the full-width workspace, existing Library filters and download behavior.

## Current color direction

The user rejected the pink/purple palette as insufficiently professional. Use pale neutral navigation with dark text in light mode, graphite navigation in dark mode, clean neutral workspaces and neutral utility icons, retaining the approved red logo and red primary actions. Keep semantic status colors and accessibility behavior.

Contact links live directly in Settings as a Contact me section. Keep GitHub, LinkedIn and Email available without a popup; the top bar contains navigation only.

Library and Queue have separate purposes: Library contains completed video/audio downloads, including explicitly marked missing files; Queue contains pending, active, paused, interrupted, failed, cancelled and source-refresh work. Completion removes a job from Queue and makes the same durable job visible in Library. Each scope keeps its own search. Confirmed browser downloads open Queue. Existing settings and queue schema remain unchanged.

Download rows place the status beside the title and expose their available actions together beside Pause/Resume, without an overflow menu. The queue primary action explicitly resumes paused/interrupted jobs before processing pending work. Add to queue, Browser integration, subtitle options and playlist selection occupy the center workspace with navigation available. Navigating away preserves queue drafts, cancels unfinished playlist selection and subtitle lookups, and stops browser verification polling. Native folder selection remains an operating-system dialog.

Retry/resume retains the saved percentage through metadata checks and connection setup. A resumed job with known progress keeps a determinate bar and percentage while preparing. Measured transfer reports remain authoritative, including a real restart at a lower offset; replacing a signed source clears progress because its old partial data is not reused. The existing saved-queue schema remains version one.

Language switching is a direct English/Arabic button showing the destination language, with keyboard activation and the existing busy-operation guard. Native tab handoffs, selected-navigation feedback, button hover and press responses use short, interruptible motion with stationary hit areas. Respect Windows reduced-motion and high-contrast settings; transfer updates do not replay page entrances. Keep the approved graphite/red identity, bundled fonts and pinned Lucide icon family.

The desktop opens in a compact 1000 × 740 frame, constrained to the monitor work area. In compact mode, each download row has a small Details icon alongside its file actions; it opens that specific job in the center workspace, with Back and keyboard focus restoration. Wide windows retain the side inspector. Keep toolbar labels on one line in both languages, protect the title and status from overlap, and match the theme glyph to the language control ink. The logo is an animation-only control: hovering or clicking plays a short downward white-arrow gesture while the V and lettering remain still. It never opens Add download, shows a tooltip, or triggers a transfer.

Use compact 44-pixel navigation buttons with 18-pixel icons and 13-pixel labels. Keep all five actions beside the logo in one row; show labels whenever the measured central space fits, and localized icon tooltips otherwise. Navigation buttons have no background or outline. Hover colors the icon red; selection retains the red icon and label. Keep the keyboard focus underline and system high-contrast colors. Mirror the page order in Arabic, and keep the inspector breakpoint independent from navigation sizing.

Library and Queue filter tabs use only a selected underline. Size selection and keyboard-focus underlines to the label and any visible count badge, excluding the tab's click padding. Keep label weight, padding, counters and click areas fixed on hover, press, focus and selection. Animate the underline's horizontal scale with a short smooth expansion; keyboard focus also uses an underline. Changing a media filter updates the existing Library view without sliding or fading the workspace. Reduced motion settles the underline immediately.

Button clicks keep their ordinary appearance after release. Header, action and navigation buttons use a separate underline for keyboard focus, without leaving a focus outline behind after mouse activation or changing their border thickness and content position.

Add download uses a centered workspace capped at 840 pixels, with an open link field, aligned muted captions, leading-aligned format choices and a centered 200 × 48 primary action. Add to queue stays on the leading side. Smaller windows stack captions and actions while preserving fluid fields and inline media preview; Settings keeps its own layout.
