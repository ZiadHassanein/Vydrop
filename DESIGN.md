---
name: Vydrop
description: A media library with graphite navigation, clean neutral workspaces, and restrained Vydrop red actions.
colors:
  page-light: "#F7F8FA"
  page-dark: "#17191E"
  surface-light: "#FFFFFF"
  surface-dark: "#1F2228"
  field-light: "#FFFFFF"
  field-dark: "#262A31"
  ink-light: "#20242C"
  ink-dark: "#F1F3F5"
  muted-light: "#58616D"
  muted-dark: "#B7BFCA"
  accent-light: "#AD253B"
  accent-dark: "#F0838C"
  action: "#C72D49"
  on-accent: "#FFFFFF"
  download-action-fill: "#C13046"
  download-action-ink: "#FFFFFF"
  download-action-progress: "#651321"
  border-light: "#737E8C"
  border-dark: "#84909F"
  hover-light: "#E9EDF2"
  hover-dark: "#2B3038"
  selected-light: "#ECEFF3"
  selected-dark: "#30353D"
  error-light: "#A12232"
  error-dark: "#F4A3AB"
  divider-light: "#DDE2E8"
  divider-dark: "#363C45"
  success-light: "#24643E"
  success-dark: "#85D1A2"
  warning-light: "#81540F"
  warning-dark: "#E4BD78"
  media-video-light: "#58616D"
  media-video-dark: "#B7BFCA"
  media-audio-light: "#58616D"
  media-audio-dark: "#B7BFCA"
  navigation-icon: "#BCC4D0"
  navigation-light: "#1B1F27"
  navigation-dark: "#1B1F27"
  navigation-ink: "#F1F3F5"
  navigation-muted: "#BCC4D0"
  navigation-selected-light: "#343B46"
  navigation-selected-dark: "#343B46"
  navigation-selected-ink: "#FFFFFF"
  browser-border-light: "#C9B2C2"
  browser-border-dark: "#795C70"
  source-youtube: "#CB2535"
  source-vimeo: "#197AA6"
  source-soundcloud: "#B94F0A"
  source-linkedin: "#0A66C2"
  source-facebook: "#2164CE"
typography:
  headline: {"fontFamily":"Inter","fontSize":"34px","fontWeight":600,"lineHeight":"44px"}
  title: {"fontFamily":"Inter","fontSize":"23px","fontWeight":600,"lineHeight":"32px"}
  dialog-title: {"fontFamily":"Inter","fontSize":"22px","fontWeight":600}
  body: {"fontFamily":"Inter","fontSize":"14px","fontWeight":400,"lineHeight":"22px"}
  item-title: {"fontFamily":"Inter","fontSize":"16px","fontWeight":600}
  detail-source: {"fontFamily":"Inter","fontSize":"13px"}
  label: {"fontFamily":"Inter","fontSize":"12px"}
  caption: {"fontFamily":"Inter","fontSize":"11px"}
  duration: {"fontFamily":"Inter","fontSize":"10px"}
  arabic-body: {"fontFamily":"Noto Sans Arabic","fontSize":"14px","lineHeight":"22px"}
  browser-body: {"fontFamily":"system-ui, sans-serif","fontSize":"14px","fontWeight":400,"lineHeight":1.5}
  browser-title: {"fontFamily":"system-ui, sans-serif","fontSize":"16px","fontWeight":650,"lineHeight":1.35}
  browser-caption: {"fontFamily":"system-ui, sans-serif","fontSize":"11px"}
  browser-title-narrow: {"fontFamily":"system-ui, sans-serif","fontSize":"16px","fontWeight":650,"lineHeight":1.35}
rounded:
  row: "4px"
  browser-control: "5px"
  artwork: "6px"
  segment: "6px"
  segment-option: "3px"
  control: "8px"
  dialog: "10px"
  count: "20px"
spacing:
  micro: "3px"
  xs: "4px"
  sm: "8px"
  browser-grid: "9px"
  row: "10px"
  control: "12px"
  md: "16px"
  action: "18px"
  lg: "20px"
  section: "22px"
  workspace: "24px"
  dialog: "28px"
components:
  button-primary: {"backgroundColor":"{colors.action}","textColor":"{colors.on-accent}","padding":"10px 18px","height":"44px"}
  button-secondary: {"backgroundColor":"{colors.field-light}","textColor":"{colors.ink-light}","padding":"8px 16px","height":"44px","rounded":"{rounded.control}"}
  button-quiet: {"backgroundColor":"transparent","textColor":"{colors.accent-light}","padding":"8px 16px","height":"44px","rounded":"{rounded.control}"}
  search-field: {"backgroundColor":"{colors.field-light}","textColor":"{colors.ink-light}","padding":"10px 12px 10px 38px","height":"44px","rounded":"{rounded.control}"}
  navigation-item: {"backgroundColor":"transparent","textColor":"{colors.navigation-ink}","padding":"10px 14px","height":"48px"}
  navigation-item-selected: {"backgroundColor":"{colors.navigation-selected-light}","textColor":"{colors.navigation-selected-ink}","padding":"10px 14px","height":"48px"}
  navigation-count: {"backgroundColor":"{colors.navigation-selected-light}","textColor":"{colors.navigation-selected-ink}","padding":"1px 6px","height":"23px","rounded":"{rounded.count}"}
  library-filter: {"backgroundColor":"transparent","textColor":"{colors.muted-light}","padding":"9px 8px","height":"46px"}
  library-count: {"backgroundColor":"{colors.surface-light}","textColor":"{colors.ink-light}","padding":"1px 6px","height":"23px","rounded":"{rounded.count}"}
  library-row-selected: {"backgroundColor":"{colors.selected-light}","textColor":"{colors.ink-light}","padding":"14px 12px","rounded":"{rounded.row}"}
  browser-segment-selected: {"backgroundColor":"{colors.selected-light}","textColor":"{colors.accent-light}","padding":"5px","height":"33px","rounded":"{rounded.segment-option}"}
  browser-format-selected: {"backgroundColor":"{colors.selected-light}","textColor":"{colors.accent-light}","padding":"6px","height":"35px","rounded":"{rounded.browser-control}"}
  browser-choice-selected: {"backgroundColor":"{colors.selected-light}","textColor":"{colors.accent-light}","padding":"6px","height":"35px","rounded":"{rounded.browser-control}"}
  browser-launch: {"backgroundColor":"{colors.action}","textColor":"{colors.on-accent}","padding":"9px 12px","height":"42px","rounded":"{rounded.browser-control}"}
---

# Design System: Vydrop

## Overview

**Creative North Star: "The Personal Media Library"**

Vydrop frames download management as a personal media library: graphite navigation, clean off-white or charcoal work surfaces, and restrained red actions. Media identity leads through thumbnails and readable titles; controls and measured transfer state stay close to the selected item. The approved Vydrop V/download-arrow and wordmark anchor the identity.

The native Windows interface uses tonal separation, fine borders, restrained corners and chamfered navigation and actions. The browser companion repeats the palette and action silhouette in a compact chooser with segmented media types and visible format and quality radio cards. Clarity, accessible states and honest runtime metadata define the visual character.

**Key Characteristics:**

- Graphite navigation with Vydrop red primary actions.
- Off-white light surfaces and layered charcoal dark surfaces.
- Inter and Noto Sans Arabic in the native app; browser-native system typography in the companion.
- Large media thumbnails, separated filters with nonzero counts, compact drawn status controls and a selected-media inspector.
- Logical RTL layout, visible focus and system high-contrast colors.

This record describes the current built source: ThemeResources.cs, ManagerStyles.xaml, Controls.xaml, MainWindow.xaml and its code-behind, DownloadManagerView.xaml and its code-behind, HistoryView.xaml, MediaPreviewView, QueueItemView.Media.cs, MainViewModel.Library.cs, MainViewModel.BrowserDownloads.cs, MainViewModel.QueueReliability.cs and browser/common/quality-picker.js, video-overlay.js, connection.js and background.js. The approved light, dark and browser v2 comps remain visual references; the source supplies the measurements below. Native measurements are WPF device-independent pixels (DIP), expressed as px in the portable frontmatter. Browser measurements are CSS pixels. Component heights describe minimum heights where source uses MinHeight or min-height. The browser values below reflect the later effective compact CSS rules.

The scoped correction review (.impeccable/review/workspace-flow-v3.json) records WF1–WF14 and the resolved source findings satisfied. Current native captures are under build/workspace-regression-captures/workspace-flow; browser captures are under build/manager-browser-captures. They use test media and isolated browser fixtures. Named final validation stages and their limits are recorded in .impeccable/review/workspace-validation-v3.json. The original mechanical build state remains open at spec with later gates pending; no original-comp fidelity, numeric comp-diff, font-ranking, bitmap-plate or complete mechanical pass is claimed.

The later scoped Add download, Settings and browser-setup corrections use native captures under build/setup-ui-regression/setup-ui, browser-setup and workspace-flow. The independent review in .impeccable/review/setup-workspace-ui.json marks SW1–SW8 satisfied with a scoped ship disposition; .impeccable/review/setup-ui-validation.json records checks and package status. This correction scope preserves the existing identity, tokens and original mechanical gate limits.

## Colors

Graphite navigation and cool neutral surfaces define the native palette. Vydrop red anchors primary actions and selection; green is reserved for success. Frontmatter owns exact primitive values. Paired -light and -dark entries map to native semantic brushes. Sidecar tonal strips are synthesized OKLCH previews for documentation, not additional shipping palette tokens.

### Primary

- **Cherry Action** (action, on-accent): solid primary actions with white content in both themes.
- **Cherry Accent** (accent-light, accent-dark): quiet actions, progress, focus and selected-row outlines; dark mode brightens small accent details.
- **Graphite Navigation** (navigation-*): persistent navigation, cool white ink, muted gray icons and copy, and a slate selected fill.
- **Editor Download Action** (download-action-fill, download-action-ink, download-action-progress): retained dedicated download-control brushes, separate from manager Action and Accent.

### Secondary

- **Source Marks** (source-youtube, source-vimeo, source-soundcloud, source-linkedin, source-facebook): small drawn identifiers beside actual source domains in rows and details. The general web mark uses Accent. These source colors do not become action colors.

### Neutral

- **Neutral Page** (page-*): off-white light workspace and charcoal dark workspace.
- **Neutral Surface** (surface-*): details, supporting containers and count badges. The native workspace uses the refined surface values above; the companion retains its existing blush/plum CSS palette.
- **Field**, **Ink** and **Muted**: input and radio-card faces, primary copy and supporting metadata.
- **Border**, **Divider**, **Hover** and **Selected**: distinct boundary, separator, pointer and selection roles.
- **Success** and **Error**: semantic feedback accompanied by explicit status text and a drawn state symbol. Missing files also use a struck title.
- **Browser Border** (browser-border-*): companion strokes, including the brighter dark-mode boundary. The chooser's CSS variables map its surfaces, text, borders, divider, accent and selected state to these palette roles.

**The Semantic Brush Rule.** Use native dynamic resources for theme-aware color roles. In Windows high contrast, system colors replace the authored palette; never pin a status or selection to a brand color.

High contrast maps Ink, Muted, Error, Success, Warning, MediaVideo, MediaAudio, NavigationIcon and navigation text to WindowText; Accent, Action, Border, DownloadActionFill and selected navigation to Highlight; OnAccent, DownloadActionInk, DownloadActionProgress and selected-navigation ink to HighlightText; other surfaces to Window. Source marks override their brand fills with Accent and their symbol stroke/fill with OnAccent. Selected/focused row border thickness increases from one to three. Browser forced colors use Canvas, CanvasText, ButtonText, Highlight and HighlightText. The launch button alone sets forced-color-adjust:none within the forced-colors query to suppress Chromium text backplates while explicitly retaining system Highlight and HighlightText.

The existing browser detector record (.impeccable/review/workspace-browser-detector.json) reports five advisory literal-color findings in video-overlay.js, covering #282b32, #FF8393 and #E63D4D. These pre-existing overlay values remain documented drift; this flow refinement neither promotes them to system tokens nor changes their styling.

## Typography

The native app bundles **Inter** and **Noto Sans Arabic** through LatinFont and ArabicFont. ContentFont selects by interface language, and Arabic media titles also select the Arabic face inside English UI. The title bar remains native; the approved vector wordmark supplies the brand lettering.

The library headline is semibold (34 DIP, 44 line height). Row titles are semibold (16 DIP), source domains regular (13 DIP), output metadata (12 DIP), and compact row status (11 DIP). Inspector and empty-state titles use the title role (23 DIP, 32 line height). Body text is regular (14 DIP, 22 line height); dialog titles use the dialog-title role. Filter count copy uses caption size; navigation counts use label size. Duration overlays use the duration role. Row titles are single-line ellipsized with a tooltip; inspector titles wrap to two lines. There is no single mathematical scale ratio in source.

The browser uses the system UI sans-serif stack and tabular numerals. Panel body copy is 14px with 1.5 line height. The effective media title is 16px, weight 650, line height 1.35 and capped at two lines at all supported widths. Media-type segments and the selected summary are 13px; labels and status are 12px; source, header status and supporting text are 11px. Media-type segments and launch action use weight 650; 13px format and quality cards use 600. Compact rules hide the HD/4K/8K badge elements; height labels remain visible and named.

**The Language Shape Rule.** Mirror the logical interface for Arabic while keeping technical strings such as source domains, file paths, versions and applicable output details readable in their explicit LTR containers.

## Layout

The manager uses graphite navigation, a flexible neutral library and a selected-media inspector. The native window opens at 1280 × 820 DIP, minimum 760 × 500. Library and inspector scroll independently; list containers are recycled and horizontal scrolling is suppressed.

- Navigation is 224 DIP wide, reducing to 176 below manager width 960. Its brand row is 92 high.
- The library inspector and inspected editor preview use width 320 from manager width 1080 through 1360, then 350 above 1360. Empty Add download, Settings and History collapse both the inspector and its reserved column/divider.
- Below 1080, details move into a 340-wide overlay drawer with 20 outer margin. Details or a row double-click opens it; Escape or Close dismisses it. Background sections are disabled, keyboard navigation cycles within, and focus returns to the list.
- Library margins settle at 24 leading, 4 top, 18 trailing, 0 bottom; below 960 the leading margin is 14. The heading/action group has 18 bottom space, search 8, and filters 10. Inspector content has 20 side insets and 20 bottom space. Section separators use 22 vertical space.
- Row artwork is 144 × 84 in a 158-wide leading column, with rounded artwork corners and a bottom-trailing duration overlay. The identity, trailing actions and 4-high progress track share an 84-high content grid; the track occupies its final four units rather than adding a second row below the artwork. Row padding remains 12 horizontally and 14 vertically. Inspector artwork is 184 high. Missing images retain a drawn media-symbol fallback.
- Row content separates identity, output metadata and a narrow trailing status/action group. This trailing group has minimum width 78 and maximum width 126; status text can ellipsize with a tooltip.
- Add download, Settings and History occupy the center workspace while the sidebar and header remain available. The editor surface has zero outer margin and no maximum-width cap; its heading margin is 24 leading, 4 top, 20 trailing and 12 bottom, and its form remains capped at 620 with internal 30 horizontal and 24 vertical margins. An actual inspected media preview moves to the existing 320/350-wide right inspector when that column is available (manager width at least 1080); below it the preview appears inline above the form controls. Compact preview artwork is 114 wide, with 12 trailing space and a two-line 16/22 title. Active queue state alone does not create a preview or reserve the inspector. Settings section content is capped at 840 with 24 horizontal, 12 top and 24 bottom margins. History uses 24 horizontal and 20 vertical margins and hides the right inspector.

The native browser-setup dialog opens at 680 × 700 DIP. Its content uses 24 horizontal and 20 vertical insets; the browser selector reserves 180 DIP beside its label. One primary next action and current app-connection status sit immediately below the selector. Optional recovery controls remain in a collapsed expander; the scrollable instruction area keeps a separate guide/Close footer accessible at shorter heights.

The browser action is positioned inside the visible video's top-right corner with a 10px inset. Its host is at most 190px wide, bounded by video width minus 20px and viewport width minus 24px. Below 160px available width, the label hides and the button becomes a 36px icon target; its minimum height remains 34px. Scroll, playback and fullscreen-container changes reposition it against the visible video bounds. A native video fullscreen element, an unrelated modal or insufficient space suppresses the action.

The browser chooser is at most 416 CSS pixels wide and leaves 12px viewport clearance. Width and maximum height are bounded by viewport size minus 24px, with overflow scrolling retained for smaller viewports. The anchor gap is 8px. The effective header is at least 50px high with 9px vertical and 16px horizontal padding; body padding is 14px vertical and 16px horizontal. The wordmark is 112 × 30px. Identity uses an 88 × 54px image beside text with a 12px gap and 12px bottom space. Missing artwork removes the image column. Media segments use 2px frame padding, 3px gaps and 33px minimum targets. Format cards use three columns, adapting to one or two when only those counts exist. Quality cards use auto-fit columns with a 60px minimum; audio uses one column. Both groups have 6px gaps and 13px bottom space, and their cards have 35px minimum height and 6px padding. The summary has 10px top padding; the 42px minimum Download action has 12px top space, followed by a hint with 7px top space. The ordinary three-format/five-height fixture fits with summary and Download visible without internal scrolling at 600/640px viewport heights. Embedded popups reserve 520px body minimum height while open and restore the previous value on dismissal.

## Elevation & Depth

The native manager uses tonal layering and dividers rather than drop shadows. Selection adds a neutral slate fill and a Vydrop red outline. The inspector is a continuous surface; the selected-library compact drawer uses a half-black scrim. Add download, Settings and History are ordinary center surfaces with no modal scrim. Settings sections sit directly on the workspace without a nested panel shell. Keep the library flat.

The floating browser chooser has one structural shadow (0 18px 48px #170d2040). Its burgundy header and tonal card boundaries carry hierarchy within the panel.

**The Surface Separation Rule.** Use palette layers and fine boundaries for native hierarchy; reserve the companion shadow for the floating browser chooser.

Native hover layers fade in over 130ms and out over 100ms with cubic ease-out. View reveals use 180ms, opacity .82 to 1 and a 5 DIP vertical settle; theme change uses a 260ms circular reveal from the theme control. Windows client-area animation settings and high contrast disable these effects. The manager chamfered action has no added hover animation.

## Shapes

Native fields and secondary buttons use the control radius; artwork uses the artwork radius and rows use the row radius. Dialogs and the browser panel use the dialog radius. Count badges use the count radius with 23 minimum width and 23 height. Boundaries are normally one unit thick, with explicit thicker focus/high-contrast states.

The signature primary action clips the leading upper corner using WPF geometry M12,0 H240 V48 H0 V12 Z, stretched to the control and logically mirrored for RTL. Navigation uses a trailing lower chamfer, M0,0 H240 V35 L227,48 H0 Z, instead of a rounded rectangle. Progress uses a slanted trailing edge, M0,0 H100 L94,6 H0 Z, stretched within its track.

The browser launch echoes the action with polygon(12px 0,100% 0,100% 100%,0 100%,0 12px); focus and forced colors remove the clip to preserve a complete outline. Segmented-control frames and artwork have restrained corners; radio cards have browser-control corners and explicit circular selection marks.

Use existing vector brand assets and the drawn geometry icon system. Runtime thumbnails are media data. Approved comp rasters remain design references; the implementation adds no new shipping raster controls or generated images. Existing asset provenance metadata does not change the asset's visual role.

## Components

### Buttons

Manager primary actions use Cherry Action, white semibold content, minimum height 44 DIP and padding 18 horizontally by 10 vertically. Add download pairs a drawn plus with its label; inspector actions pair a drawn pause or play symbol with their label. Focus adds a two-unit Ink stroke; press lowers opacity to .85, disabled state to .45. The inherited rounded PrimaryButton remains in existing editor controls: minimum height 52, subtle white hover layer and two-unit Ink focus.

Secondary buttons use Field, Ink and Border, control radius, 16 × 8 padding and minimum height 44. Hover uses Hover, press uses Selected, and focus adds a two-unit Accent border. Quiet actions inherit the geometry with transparent face/stroke and Accent text. Compact row/toolbar actions use their local smaller minimum sizes.

### Inputs / Fields

Search uses Field and Ink, one-unit Border, control radius and height 44 DIP. A drawn 17 × 17 magnifier sits at leading inset 13; input padding is 38 leading, 12 trailing and 10 vertically. Its visible prompt hides once text exists. Text entry uses Accent caret, Selected text selection and an Accent border on focus. Disabled fields use .55 opacity. Native combo boxes elsewhere in the editor share the surface/radius and add a separate two-unit Accent focus border; their popup is a scrolling selection surface.

### Navigation

Navigation items use outlined 19 × 19 vector icons, minimum height 48 DIP, padding 14 × 10 and margin 10 × 3. Unselected icons use NavigationIcon, a cool gray; selected icons and labels use NavigationSelectedInk on the chamfered NavigationSelected face. Hover adds a NavigationMuted stroke; focus uses a two-unit NavigationInk stroke. Queue, Video and Audio display live 23-high count badges only when nonzero; collapse removes the badge and its margin. Queue's badge counts unfinished items. Arabic reverses logical placement; the wordmark retains its LTR orientation. Add download, Settings and History select their center workspace without disabling sidebar navigation. Sidebar and Settings contain no visible version or release-channel labels.

### Counted filter tabs

Library filters are text tabs with a two-unit underline, 14 DIP trailing separation, minimum height 46 and 8 horizontal by 9 vertical padding. They wrap as needed. Their 23-high count badges collapse completely at zero. Selection uses Ink, semibold weight and Action underline; hover uses Accent text. Focus adds an Accent boundary while retaining the thicker bottom edge. All, Downloading, Paused and Completed show nonzero queue-derived counts. Paused includes interrupted work; these counts come from the full queue rather than the search-filtered result set. A drawn compact control beside the tabs opens queue management.

### Library rows and selected-media details

Flat rows carry large thumbnail, title, a drawn source mark and domain, output metadata, explicit status and contextual controls. Source marks are 18 × 16 DIP. Selection uses Selected fill and Accent border; focus stays visible and high contrast thickens the border. Hover changes the boundary to Divider. Missing files strike their title and show a text status with an alert symbol.

The trailing group combines a drawn 12 × 12 status symbol with 11 DIP status text. Active non-preparing, paused and interrupted rows append the real current percentage. A drawn 18 × 18 action sits in a minimum 34 × 34 target; completed rows open their save folder, active rows pause and other rows expose their available primary action. The 28-minimum More target uses three drawn dots and opens a row-specific themed context menu. Its Surface face has a one-unit Border stroke, 8 radius and 6 padding, with width bounded from 220 to 320. Menu items have 38 minimum height, 10 horizontal by 8 vertical padding, 16 × 16 drawn icons in a 20-wide column and a 10-unit label gap. Selected/highlighted items use Selected and Accent; disabled actions use .45 opacity and destructive removal uses Error. Closing restores trigger focus.

Row progress is 4 DIP high within the 84-high content area; inspector progress retains the shared style's 5 DIP height. Both use a slanted fill edge and their real Percent binding. Preparing uses the IsIndeterminate state. The inspector repeats media identity at larger scale, then actual output, state and active percentage. Transfer amount, speed and remaining time are compact inline text; active values appear only when available and otherwise use explicit unavailable values. A single-line ellipsized LTR save path has a full-path tooltip and a separate drawn folder action. The inspector ends with subtitle options and saved-locally feedback when applicable.

### Center workspaces

Add download uses the existing form and download engine inside the center panel. An empty form uses the available center width without a blank inspector; wide inspected previews use the right inspector and compact previews move inline. Clearing or replacing a link removes obsolete inspection status and preview. Actual active-queue status and Cancel remain in the form's scrolling content even without a preview. Settings uses one workspace-owned title and Close control above flat browser, app-update and download-support sections; its update actions remain available. Escape or Close returns to the library and restores prior focus. History uses a scrolling local recent-file list, an honest empty state, struck missing titles and availability-dependent reveal actions. It preserves the existing five-file limit and uses normal Tab continuation into workspace navigation.

### Download preferences

The native Download preferences section retains the flat Page surface and existing checkbox, combo-box and text-field vocabulary. Each row pairs a semibold label and wrapping 13 DIP Muted helper with a 180 DIP control column across a 16 DIP gap; subsequent rows have 20 DIP top separation. Download parts uses localized 1 part (default), 2 parts and 4 parts choices with same-file help. Limit download speed is an explicit checkbox: off shows Unlimited, while on reveals the positive whole-number editor, LTR KiB/s unit and exact 1024 KiB/s = 1 MiB/s explanation. Invalid enabled values retain the last valid cap and show a wrapping Error message with polite live feedback; re-enabling restores the last positive cap within the session. Automatic retries uses localized No retries through Retry 4 times choices, with Retry once as the default. The direct-MP4 checkbox keeps its 2-or-4-parts prerequisite and normal-method fallback visible below it. Native choices and helpers follow RTL layout; only the numeric speed/unit group stays LTR. Accessible names and help match the visible copy. These controls describe new-download defaults and make no speed guarantee; the scoped receipt is .impeccable/review/download-settings-validation.json.

### Native browser setup

The setup dialog reuses Page, Ink, Muted, Accent and the existing native controls. Its title is 23 DIP with 30 line height; the primary action uses the rounded PrimaryButton treatment with a local 44 DIP minimum height and 12 × 6 padding. The first view pairs the browser selector with concise action copy and current app-registration status. Successful registration uses Success, while separate text reports whether the browser handshake has been tested. Registration alone never presents a verified browser connection.

A published listing selects the store action; otherwise installed and portable apps both select local setup. The explicit local action prepares verified matching companion files if needed, connects the app, copies the real extension path and opens the selected browser's setup page. Missing or malformed embedded recovery content instead presents an active illustrated-guide action and hides file-loading instructions. Optional manual controls stay collapsed initially. Firefox's temporary-add-on limitation is stated alongside the loading instructions. Paths and browser addresses retain LTR containers within Arabic layout; messages and verification status use polite live announcements.

### Browser chooser

The isolated dialog contains a burgundy wordmark header with readiness copy and a drawn Close control, a thumbnail/title/source identity block, segmented Video/Audio controls, compact format and quality radio-card groups, a selection summary and a full-width Download action with a drawn glyph. Media-type segments use an inset selected fill. Formats remain visible choices. Each format and quality card has a 14px circular radio mark; selection uses a 4px Accent ring. Compact rules hide the visual HD/4K/8K badges. Effective card and action dimensions are recorded in Layout and the frontmatter.

Available formats and heights come from inspection. The summary repeats the selected format and height, or automatic source quality for audio, and states that file size is unavailable before downloading. Radio groups expose role, checked state and a roving Tab stop. Home and End move to the first and last choices; vertical arrows wrap, and horizontal arrows follow interface direction for Arabic. Video/Audio unavailable states disable the corresponding segment.

Browser focus uses a two-pixel Accent outline offset by three pixels. Escape and outside pointer dismiss the chooser, Tab stays among available controls, and closing restores anchor focus. Dismissal restores the overlay label and clears its pending/finding presentation; disposed picker callbacks ignore late inspection or send results. Loading, retry, unavailable, successful handoff and legacy-app handoff states use visible text. CSS variables carry light, dark and forced-color themes; selection, scrollbars and numeric alignment use the same palette and typography.

A trusted Download confirmation carries the selected media kind, container and height through the start-download capability. The native app saves that choice with its existing saved folder in the durable queue before accepting it, starts when idle and queues confirmed work while busy. Queue-wide pause, cancel, scheduled stop, engine halt and close clear automatic continuation intent; old persisted work stays stopped after restart. Hosts without confirmed-start support show the explicit older-app review/start fallback.

Inspection reuse is bounded to eight source URLs: pending requests are shared and successful format results last 60 seconds; failures are evicted for retry. Background source resolution separately holds up to eight tab/frame/source-context entries for 30 seconds. Native port requests serialize and begin their deadlines when sent, with 80 seconds for inspection and 18 seconds for other requests. The host reuses its inspector and avoids automatic tool maintenance on each format check. These are current implementation bounds, not download-speed promises.

## Do's and Don'ts

### Do:

- **Do** use the semantic theme roles and Windows system colors for high contrast.
- **Do** preserve the approved vector Vydrop identity and use runtime media thumbnails with an honest fallback.
- **Do** keep selected, focused, unavailable and transfer states identifiable through text or shape as well as color.
- **Do** use the bundled native language fonts and mirror logical layout for Arabic.
- **Do** retain the chamfered navigation and primary action, counted filters, flat media rows and distinct selected-media surface.
- **Do** show inspected format and quality options as radio cards with a visible selection summary in the browser chooser.

### Don't:

- **Don't** introduce competitor branding or turn the approved concept images into shipping raster UI.
- **Don't** invent thumbnails, file sizes, transfer rates, remaining times or resume guarantees.
- **Don't** use green as a general brand accent; reserve it for successful states with a label or symbol.
- **Don't** add dashboard analytics, decorative graphs or an overcrowded legacy toolbar to the operating surface.
- **Don't** replace native high-contrast or reduced-motion behavior with fixed authored styling.


## Add download workspace refinement

Keep the manager shell, neutral workspace and restrained Vydrop red actions. The inline editor groups the URL, Paste action and brief guidance in one shallow surface, followed by flat output rows with aligned captions. Rows stack at narrow widths; URL entry stays left-to-right in either language. One folder picker includes Change, with a separate Open folder action. A divider separates the quiet queue action from the primary Download action. Header and form share their maximum content width. Media identity remains in the selected right inspector at wide widths and inline when compact. All controls continue to use real available media, saved options and existing queue commands.


## Native icon and color refinement

Use the selected Lucide 0.577.0 system icons as native WPF geometry, with the shared LineIcon rounded 1.7-DIP stroke. The approved Vydrop wordmark and brand arrow remain separate and unchanged. Navigation, form, History, menus and the theme switch share these geometries; keep the existing resource keys to preserve older view bindings. The pinned upstream commit and ISC / Feather MIT licenses are in THIRD-PARTY-NOTICES.txt. No icon package or network lookup runs in the app.

Reserve cherry for actions, active downloads and selection. Paused or interrupted work uses Warning amber; completion uses Success green; unavailable files and failures use Error. Row and inspector progress inherit the same state brush as their status label and symbol. History's unavailable icon also uses Error alongside its existing struck title and explicit missing-file label. MediaAudio and MediaVideo use the same neutral icon color; their distinct shapes and labels identify the media type. Each state retains visible copy and a meaningful icon.

The native workspace uses off-white or charcoal layers with graphite navigation. Neutral utility icons recede so Vydrop red actions, focus outlines and status feedback retain emphasis. English and Arabic use the same roles. Text and semantic colors meet 4.5:1 on normal, field, hover and selected surfaces; control boundaries meet 3:1. Windows high contrast overrides every new semantic role with system foreground colors.


### Restrained native palette

The user rejected the pink/purple surface treatment. Native navigation is now graphite in both themes, with off-white light workspaces and charcoal dark workspaces. Selection surfaces and utility icons are neutral; the red logo and primary actions carry the brand. Remove lavender audio accents and rose navigation icons. Preserve status semantics, typography, layout, keyboard focus and all Windows high-contrast overrides. The browser companion retains its existing palette in this desktop-only scope.


## Contact placement

Contact me is a section in Settings below Download preferences. Its GitHub, LinkedIn and localized Email buttons are visible directly, wrap within the workspace, and use the existing SocialCommand destinations. The sidebar ends with Browser integration and Settings; no contact footer or popup remains. All links retain normal button focus, accessible names and English/Arabic layout.
