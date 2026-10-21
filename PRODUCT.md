# Vydrop

<!-- impeccable:product-schema 1 -->

## Platform

Native Windows desktop (WPF), with an HTML/CSS/JavaScript browser companion for Chrome, Edge and Firefox. This is an existing Windows application, not a web or mobile app.

## Users

People saving public video and audio on Windows, including English and Arabic users who send links from their browser or paste them into the app.

## Product Purpose

Inspect public media, choose an available output format and quality, save downloads to a chosen local folder, and manage a durable queue. Users must understand what is downloading, what needs attention, and how to resume work after reopening the app.

## Operating Context

The download manager is the default main screen, with persistent sidebar navigation and a searchable download list. Add download and History replace the center workspace; the inspected Add download preview appears in the right inspector at wide sizes and inline in compact layouts. History remains accessible during transfers. Zero sidebar and filter counts are hidden, and the status filters have deliberate separation.

The browser action sits inside the visible video's top-right corner. Its compact menu shows inspected format and quality choices, a selected-output summary and Download action. Confirming a choice uses the saved folder, persists the job in the local queue and starts it through the existing download path when idle; confirmed jobs wait when the app is busy. Older hosts retain a visible review-and-start-in-app fallback. The user requested visual mockups before implementation and then these operating-flow refinements.

## Capabilities and Constraints

- Existing supported outputs, playlists, available subtitles, media inspection, queue pause/resume, scheduling and browser integration remain available.
- Preserve stable settings and queue storage compatibility, older browser/native-host protocol compatibility, and Windows 10/11 x64 support.
- Queue and history stay local. Downloading cannot continue after the app process exits.
- Display actual source metadata and measured transfer information. Do not invent file sizes, speed, time remaining, resume guarantees or connection counts.
- Do not extend this redesign into general document/program downloading, DRM access, account extraction or a new download engine.
- Keep existing English/Arabic language support, RTL behavior, light/dark themes, keyboard access and Windows high-contrast support.
- Queue-wide pause, cancel, scheduled stop, engine halt and app closure retain control over confirmed browser work. Reopening the app does not silently resume older pending or interrupted jobs.
- Repeated and concurrent browser format checks reuse bounded inspection work. Dismissing the chooser clears its busy presentation and ignores late inspection results.

## Brand Commitments

Retain the Vydrop name, approved red V/download-arrow wordmark and recognizable red brand accents. The reference images establish a download-manager workflow and an on-video quality menu; they do not authorize copying another product's branding or visual assets.

The user selected the right-hand details-panel layout (concept A), but rejected the reference-like visual treatment. Keep that information layout while developing an original Vydrop style. Prioritize media identity, a recognizable brand-colored navigation area and clear, deliberately designed controls over a conventional dense file table. The old mockups are layout references, not approved visual designs.

## Evidence on Hand

- Current application views and tokens: `src/Downloader.Desktop/Views/` and `src/Downloader.Desktop/Themes/`.
- Existing companion: `browser/common/`.
- Queue restart tests demonstrate recovery after normal closure and process termination, byte resume and exact output integrity.
- User supplied two reference images and confirmed the manager-first structure, browser quality menu and mockup-first workflow.
- The final scoped review in `.impeccable/review/workspace-flow-v3.json` marks WF1–WF14 and the resolved source findings satisfied. Fresh native and browser fixture captures and named validation logs support that correction scope; `.impeccable/review/workspace-validation-v3.json` records the final stage results and limits.
- The original comp-led mechanical workflow remains open at spec, with later gates pending. The scoped correction review does not certify original-comp fidelity or a complete mechanical build pass.

## Product Principles

- Make the current download and its next useful action immediately readable.
- Keep simple downloads simple; reveal scheduling and advanced controls when needed.
- Preserve recoverable user work across navigation, shutdown and upgrades.
- Show media identity and useful status instead of asking users to recognize raw URLs.

## Accessibility & Inclusion

Readable English and Arabic typography, correct bidirectional text, visible keyboard focus, named controls, state indicators beyond color, reduced-motion behavior and Windows accessibility conventions remain requirements.
