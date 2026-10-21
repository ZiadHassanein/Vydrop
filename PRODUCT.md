# Vydrop

<!-- impeccable:product-schema 1 -->

## Platform

Native Windows desktop (WPF), with an HTML/CSS/JavaScript browser companion for Chrome, Edge and Firefox. This is an existing Windows application, not a web or mobile app.

## Users

People saving public video and audio on Windows, including English and Arabic users who send links from their browser or paste them into the app.

## Product Purpose

Inspect public media, choose an available output format and quality, save downloads to a chosen local folder, and manage a durable queue. Users must understand what is downloading, what needs attention, and how to resume work after reopening the app.

## Operating Context

The download manager is the default main screen, with persistent sidebar navigation and a searchable download list. Add download, Settings and History replace the center workspace. Empty Add download and Settings use the available center width without reserving a blank inspector; an inspected Add download preview appears in the right inspector at wide sizes and inline in compact layouts. Settings has one workspace title and Close control, with flat section content. Sidebar and Settings show no version or release-channel labels. History remains accessible during transfers. Zero sidebar and filter counts are hidden, and the status filters have deliberate separation.

The browser action sits inside the visible video's top-right corner. Its compact menu shows inspected format and quality choices, a selected-output summary and Download action. Confirming a choice uses the saved folder, persists the job in the local queue and starts it through the existing download path when idle; confirmed jobs wait when the app is busy. Older hosts retain a visible review-and-start-in-app fallback. The user requested visual mockups before implementation and then these operating-flow refinements.

Browser setup presents a browser selector, one available next action and concise instructions. Both installed and portable apps offer local loading when no published store listing exists. A copied app can prepare its embedded, matching companion files in a local cache after the user starts setup; missing or unusable recovery content leads to the illustrated guide. Native app registration and a verified browser handshake are separate states.

Download preferences apply to new downloads; queued jobs retain their captured settings. Download parts offers 1 part by default, 2 parts or 4 parts of the same file when supported. An explicit speed-limit checkbox shows Unlimited when off and a positive KiB/s cap when on, with the explanation 1024 KiB/s = 1 MiB/s; invalid input preserves the last valid cap. Automatic retries describes zero through four extra tries after temporary network errors, with Retry once as the default. The direct-MP4 option explains its 2-or-4-parts prerequisite and normal-method fallback without promising faster transfers.

## Capabilities and Constraints

- Existing supported outputs, playlists, available subtitles, media inspection, queue pause/resume, scheduling and browser integration remain available.
- Preserve stable settings and queue storage compatibility, older browser/native-host protocol compatibility, and Windows 10/11 x64 support.
- Queue and history stay local. Downloading cannot continue after the app process exits.
- Display actual source metadata and measured transfer information. Do not invent file sizes, speed, time remaining, resume guarantees or connection counts.
- Do not extend this redesign into general document/program downloading, DRM access, account extraction or a new download engine.
- Keep existing English/Arabic language support, RTL behavior, light/dark themes, keyboard access and Windows high-contrast support.
- Queue-wide pause, cancel, scheduled stop, engine halt and app closure retain control over confirmed browser work. Reopening the app does not silently resume older pending or interrupted jobs.
- Repeated and concurrent browser format checks reuse bounded inspection work. Dismissing the chooser clears its busy presentation and ignores late inspection results.
- Clearing or replacing the editor link cancels stale inspection feedback and ignores late results. Actual active-queue feedback and Cancel remain available inline even without a media preview. A scheduled stop consumes the old start schedule as well, so a timer tick cannot restart that stopped work.
- Opening browser setup performs no connection registration, browser launch, clipboard write or recovery write. Explicit local setup verifies matching companion files before copying a loadable path; cache repair preserves unrelated files. Saved settings, queue formats and browser/native-host protocols retain their existing compatibility.

## Brand Commitments

Retain the Vydrop name, approved red V/download-arrow wordmark and recognizable red brand accents. The reference images establish a download-manager workflow and an on-video quality menu; they do not authorize copying another product's branding or visual assets.

The user selected the right-hand details-panel layout (concept A), but rejected the reference-like visual treatment. Keep that information layout while developing an original Vydrop style. Prioritize media identity, a recognizable brand-colored navigation area and clear, deliberately designed controls over a conventional dense file table. The old mockups are layout references, not approved visual designs.

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
- Keep simple downloads simple; reveal scheduling and advanced controls when needed.
- Preserve recoverable user work across navigation, shutdown and upgrades.
- Show media identity and useful status instead of asking users to recognize raw URLs.

## Accessibility & Inclusion

Readable English and Arabic typography, correct bidirectional text, visible keyboard focus, named controls, state indicators beyond color, reduced-motion behavior and Windows accessibility conventions remain requirements.
