---
name: Vydrop
description: A media library with theme-matched neutral navigation, clean workspaces, and restrained Vydrop red actions.
colors:
  page-light: "#F7F8FA"
  page-dark: "#17191E"
  surface-light: "#FFFFFF"
  surface-dark: "#1F2228"
  setup-feedback-light: "#E8EDF3"
  setup-feedback-dark: "#1F2228"
  setup-feedback-border-light: "#C4CDD9"
  setup-feedback-border-dark: "#1F2228"
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
  navigation-icon-light: "#58616D"
  navigation-icon-dark: "#BCC4D0"
  navigation-light: "#EEF1F5"
  navigation-dark: "#1B1F27"
  navigation-ink-light: "#20242C"
  navigation-ink-dark: "#F1F3F5"
  navigation-muted-light: "#58616D"
  navigation-muted-dark: "#BCC4D0"
  navigation-selected-light: "#DDE4ED"
  navigation-selected-dark: "#343B46"
  navigation-selected-ink-light: "#20242C"
  navigation-selected-ink-dark: "#FFFFFF"
  navigation-active-ink-light: "#AD253B"
  navigation-active-ink-dark: "#F58A93"
  browser-border-light: "#C9B2C2"
  browser-border-dark: "#795C70"
  source-youtube: "#CB2535"
  source-vimeo: "#197AA6"
  source-soundcloud: "#B94F0A"
  source-linkedin: "#0A66C2"
  source-facebook: "#2164CE"
typography:
  headline: {"fontFamily":"Inter","fontSize":"26px","fontWeight":600,"lineHeight":"34px"}
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
  navigation-item: {"backgroundColor":"transparent","textColor":"{colors.navigation-ink}","padding":"8px 10px","height":"44px"}
  navigation-item-selected: {"backgroundColor":"transparent","textColor":"{colors.navigation-active-ink}","padding":"8px 10px","height":"44px"}
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

- Pale neutral navigation in light mode and graphite navigation in dark mode, with Vydrop red primary actions.
- Off-white light surfaces and layered charcoal dark surfaces.
- Inter and Noto Sans Arabic in the native app; browser-native system typography in the companion.
- Large media thumbnails, separated filters with nonzero counts, compact drawn status controls and a selected-media inspector.
- Logical RTL layout, visible focus and system high-contrast colors.

This record describes the current built source: ThemeResources.cs, ManagerStyles.xaml, Controls.xaml, MainWindow.xaml and its code-behind, DownloadManagerView.xaml and its code-behind, HistoryView.xaml, MediaPreviewView, QueueItemView.Media.cs, MainViewModel.Library.cs, MainViewModel.BrowserDownloads.cs, MainViewModel.QueueReliability.cs and browser/common/quality-picker.js, video-overlay.js, connection.js and background.js. The approved light, dark and browser v2 comps remain visual references; the source supplies the measurements below. Native measurements are WPF device-independent pixels (DIP), expressed as px in the portable frontmatter. Browser measurements are CSS pixels. Component heights describe minimum heights where source uses MinHeight or min-height. The browser values below reflect the later effective compact CSS rules.

The scoped correction review (.impeccable/review/workspace-flow-v3.json) records WF1–WF14 and the resolved source findings satisfied. Current native captures are under build/workspace-regression-captures/workspace-flow; browser captures are under build/manager-browser-captures. They use test media and isolated browser fixtures. Named final validation stages and their limits are recorded in .impeccable/review/workspace-validation-v3.json. The original mechanical build state remains open at spec with later gates pending; no original-comp fidelity, numeric comp-diff, font-ranking, bitmap-plate or complete mechanical pass is claimed.

The later scoped Add download, Settings and browser-setup corrections use native captures under build/setup-ui-regression/setup-ui, browser-setup and workspace-flow. The independent review in .impeccable/review/setup-workspace-ui.json marks SW1–SW8 satisfied with a scoped ship disposition; .impeccable/review/setup-ui-validation.json records checks and package status. This correction scope preserves the existing identity, tokens and original mechanical gate limits.

## Colors

Theme-matched navigation and cool neutral surfaces define the native palette. Vydrop red anchors primary actions and selection; green is reserved for success. Frontmatter owns exact primitive values. Paired -light and -dark entries map to native semantic brushes. Sidecar tonal strips are synthesized OKLCH previews for documentation, not additional shipping palette tokens.

### Primary

- **Cherry Action** (action, on-accent): solid primary actions with white content in both themes.
- **Cherry Accent** (accent-light, accent-dark): quiet actions, progress, focus and selected-row outlines; dark mode brightens small accent details.
- **Navigation** (navigation-*): pale neutral surfaces, dark ink and a light slate selected fill in light mode; graphite surfaces, cool white ink and a slate selected fill in dark mode. Muted icons and copy follow the active theme.
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

Page headings are semibold (26 DIP, 34 line height). Row titles are semibold (16 DIP), source domains regular (13 DIP), output metadata (12 DIP), and compact row status (11 DIP). Inspector titles use the title role (23 DIP, 32 line height); empty-state titles use 20 DIP with a 28-DIP line height. Body text is regular (14 DIP, 22 line height); dialog titles use the dialog-title role. Filter count copy uses caption size; navigation counts use label size. Duration overlays use the duration role. Row titles are single-line ellipsized with a tooltip; inspector titles wrap to two lines. There is no single mathematical scale ratio in source.

The browser uses the system UI sans-serif stack and tabular numerals. Panel body copy is 14px with 1.5 line height. The effective media title is 16px, weight 650, line height 1.35 and capped at two lines at all supported widths. Media-type segments and the selected summary are 13px; labels and status are 12px; source, header status and supporting text are 11px. Media-type segments and launch action use weight 650; 13px format and quality cards use 600. Compact rules hide the HD/4K/8K badge elements; height labels remain visible and named.

**The Language Shape Rule.** Mirror the logical interface for Arabic while keeping technical strings such as source domains, file paths, versions and applicable output details readable in their explicit LTR containers.

## Layout

The manager uses theme-matched horizontal navigation above a full-width workspace and a selected-media inspector. The native window opens at 1000 × 740 DIP within the monitor work area, minimum 760 × 500. Library and inspector scroll independently; list containers are recycled and horizontal scrolling is suppressed.

- Five page actions share a single 59-DIP toolbar with the logo and language/theme controls. Names appear whenever their measured width fits the available central space; otherwise, use 44-DIP icons with localized tooltips and accessible names. This decision is independent of the inspector breakpoint. The workspace reserves no sidebar column.
- The library inspector and inspected editor preview use width 320 from manager width 1080 through 1360, then 350 above 1360. Empty Add download, Settings and History collapse both the inspector and its reserved column/divider.
- Below 1080, details move into a 340-wide overlay drawer with 20 outer margin. Details or a row double-click opens it; Escape or Close dismisses it. Background sections are disabled, keyboard navigation cycles within, and focus returns to the list.
- Library margins settle at 24 leading, 4 top, 18 trailing, 0 bottom; below 960 the leading margin is 14. The heading/action group has 12 bottom space, search 8, and filters 10. Inspector content has 20 side insets and 20 bottom space. Section separators use 22 vertical space.
- Row artwork is 144 × 84 in a 158-wide leading column, with rounded artwork corners and a bottom-trailing duration overlay. The identity, trailing actions and 4-high progress track share an 84-high content grid; the track occupies its final four units rather than adding a second row below the artwork. Row padding remains 12 horizontally and 14 vertically. Inspector artwork is 184 high. Missing images retain a drawn media-symbol fallback.
- Row content separates identity, output metadata and a narrow trailing status/action group. This trailing group has minimum width 78 and maximum width 126; status text can ellipsize with a tooltip.
- Add download, Settings and History occupy the center workspace while the top navigation and header remain available. The editor surface has zero outer margin and no maximum-width cap; its heading margin is 24 leading, 4 top, 20 trailing and 12 bottom, and its form shares the 840-DIP editor cap with internal 24 horizontal, 8 top and 24 bottom margins. The link field is open on the page surface, 52 DIP high, with Paste alongside. Form captions use 12-DIP muted type and an 88-DIP gutter; selected media-format icons use Accent. Download stays centered at 200 × 48 DIP, with Add to queue on the leading side; narrow forms stack the captions and actions. An actual inspected media preview moves to the existing 320/350-wide right inspector when that column is available (manager width at least 1080); below it the preview appears inline above the form controls. Compact preview artwork is 114 wide, with 12 trailing space and a two-line 16/22 title. Active queue state alone does not create a preview or reserve the inspector. Settings section content is capped at 840 with 24 horizontal, 12 top and 24 bottom margins. History uses 24 horizontal and 20 vertical margins and hides the right inspector.

The native browser-setup dialog opens at 680 × 700 DIP. Its content uses 24 horizontal and 20 vertical insets; the browser selector reserves 180 DIP beside its label. One primary next action and current app-connection status sit immediately below the selector. Optional recovery controls remain in a collapsed expander; the scrollable instruction area keeps a separate guide/Close footer accessible at shorter heights.

The browser action is positioned inside the visible video's top-right corner with a 10px inset. Its host is at most 190px wide, bounded by video width minus 20px and viewport width minus 24px. Below 160px available width, the label hides and the button becomes a 36px icon target; its minimum height remains 34px. Scroll, playback and fullscreen-container changes reposition it against the visible video bounds. A native video fullscreen element, an unrelated modal or insufficient space suppresses the action.

The browser chooser is at most 416 CSS pixels wide and leaves 12px viewport clearance. Width and maximum height are bounded by viewport size minus 24px, with overflow scrolling retained for smaller viewports. The anchor gap is 8px. The effective header is at least 50px high with 9px vertical and 16px horizontal padding; body padding is 14px vertical and 16px horizontal. The wordmark is 112 × 30px. Identity uses an 88 × 54px image beside text with a 12px gap and 12px bottom space. Missing artwork removes the image column. Media segments use 2px frame padding, 3px gaps and 33px minimum targets. Format cards use three columns, adapting to one or two when only those counts exist. Quality cards use auto-fit columns with a 60px minimum; audio uses one column. Both groups have 6px gaps and 13px bottom space, and their cards have 35px minimum height and 6px padding. The summary has 10px top padding; the 42px minimum Download action has 12px top space, followed by a hint with 7px top space. The ordinary three-format/five-height fixture fits with summary and Download visible without internal scrolling at 600/640px viewport heights. Embedded popups reserve 520px body minimum height while open and restore the previous value on dismissal.

## Elevation & Depth

The native manager uses tonal layering and dividers rather than drop shadows. Selection adds a neutral slate fill and a Vydrop red outline. The inspector is a continuous surface; compact details uses the center workspace with a Back action. Add download, Settings and History are ordinary center surfaces with no modal scrim. Settings sections sit directly on the workspace without a nested panel shell. Keep the library flat.

The floating browser chooser has one structural shadow (0 18px 48px #170d2040). Its burgundy header and tonal card boundaries carry hierarchy within the panel.

**The Surface Separation Rule.** Use palette layers and fine boundaries for native hierarchy; reserve the companion shadow for the floating browser chooser.

Native hover layers fade in over 130ms and out over 100ms with cubic ease-out. Navigation selection and filter underlines fade in over 170ms and out over 110ms. Workspace navigation uses a 180ms, 12-DIP lateral handoff with opacity .94 to 1; direction follows navigation order and mirrors for Arabic. Coalesce rapid navigation and animate route changes only, never ordinary transfer updates. Keep the viewport clipped and the toolbar, inspector and all button hit areas stationary.

Button visuals compress to .975 over 80ms on press and return over 140–150ms on release or invocation. The chamfered primary action has a restrained white hover layer. Media-detail reveals use 180ms and a 4-DIP vertical settle; theme change retains its 260ms circular reveal. Completed, interrupted and hidden animations release their clocks and transforms. Windows client-area animation settings and high contrast disable spatial/decorative motion and retain immediate, legible state feedback.

The header emblem plays a single coordinated 540ms horizontal-axis flip of the V and download arrow on hover or activation, with a 4-degree perspective hint and restrained back-face shading. The letters, layout and hit area stay still. A neutral, narrow 420ms highlight follows only after the emblem settles; its overlay opacity is capped at .32. Repeated hover/clicks do not stack or restart a running sequence. Pointer exit lets an in-flight turn finish; hidden, unloaded, disabled or reduced-motion states immediately restore the exact resting logo and cancel late highlights. The existing Download button shine keeps its own timing and palette.

The header uses a fixed-width language button with the destination language in its own bundled font, the pinned Lucide Languages icon, and a localized action name. Click, Enter or Space switches directly between English and Arabic through the existing saved-language command; preserve its busy-operation guard. No language popup or chevron remains. Library uses the stacked-gallery symbol and video uses the clapperboard from the same pinned Lucide source; the theme symbol shares the 1.7-DIP outline weight.

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

The top bar contains Add download, Library, Queue, History and Settings. Browser integration is a single action within Settings, and keeps Settings active while open. Videos and Audio are filters within Library; selecting either keeps Library active in the top bar.

Navigation items use outlined 18 × 18 vector icons, minimum height 44 DIP, 13-DIP labels, padding 10 × 8 and margin 2 × 2. Unselected icons use NavigationIcon, a cool gray; selected icons and labels use NavigationActiveInk on the transparent toolbar, without a selected fill or outline. Hover changes only the glyph to NavigationActiveInk. High contrast maps that glyph to system Highlight on Window; keyboard focus retains its separate underline. Library and Queue display live 23-high navigation count badges only when nonzero; collapse removes the badge and its margin. Queue's badge counts unfinished items. Arabic reverses logical placement; the wordmark retains its LTR orientation. Add download, Settings and History select their center workspace without disabling top navigation. Navigation and Settings contain no visible version or release-channel labels.

### Counted filter tabs

Library filters are text tabs with a two-unit underline, 14 DIP trailing separation, minimum height 46 and 8 horizontal by 9 vertical padding. They wrap as needed. Their 23-high count badges collapse completely at zero. Selection uses Ink, semibold weight and Action underline; hover uses Accent text. Focus adds an Accent boundary while retaining the thicker bottom edge. Library uses All, Videos and Audio filters over completed media. Queue uses All, Downloading, Waiting and Paused filters over unfinished work. Paused includes interrupted work. Failed, cancelled and source-refresh jobs remain in All with their existing recovery actions. Counts come from the full matching scope rather than the search-filtered result set. Queue has visible Start download / Resume downloads, Pause queue and Add to queue controls; Library has no transfer toolbar.

### Library rows and selected-media details

Flat rows carry large thumbnail, title, a drawn source mark and domain, output metadata, explicit status and contextual controls. Source marks are 18 × 16 DIP. Selection uses Selected fill and Accent border; focus stays visible and high contrast thickens the border. Hover changes the boundary to Divider. Missing files strike their title and show a text status with an alert symbol.

The trailing group combines a drawn 12 × 12 status symbol with 11 DIP status text. Active non-preparing, paused and interrupted rows append the real current percentage. A drawn 18 × 18 action sits in a minimum 34 × 34 target; completed rows open their save folder, active rows pause and other rows expose their available primary action. The 28-minimum More target uses three drawn dots and opens a row-specific themed context menu. Its Surface face has a one-unit Border stroke, 8 radius and 6 padding, with width bounded from 220 to 320. Menu items have 38 minimum height, 10 horizontal by 8 vertical padding, 16 × 16 drawn icons in a 20-wide column and a 10-unit label gap. Selected/highlighted items use Selected and Accent; disabled actions use .45 opacity and destructive removal uses Error. Closing restores trigger focus.

Row progress is 4 DIP high within the 84-high content area; inspector progress retains the shared style's 5 DIP height. Both use a slanted fill edge and their real Percent binding. Preparing uses the IsIndeterminate state. The inspector repeats media identity at larger scale, then actual output, state and active percentage. Transfer amount, speed and remaining time are compact inline text; active values appear only when available and otherwise use explicit unavailable values. A single-line ellipsized LTR save path has a full-path tooltip and a separate drawn folder action. The inspector ends with subtitle options and saved-locally feedback when applicable.

### Center workspaces

Add download uses the existing form and download engine inside the center panel. An empty form uses the available center width without a blank inspector; wide inspected previews use the right inspector and compact previews move inline. Clearing or replacing a link removes obsolete inspection status and preview. Actual active-queue status and Cancel remain in the form's scrolling content even without a preview. Settings uses one workspace-owned title above flat browser, app-update and download-support sections; its update actions remain available. These workspace tabs keep toolbar navigation available without a modal Close control. Escape retains the existing return-to-library shortcut and restores prior focus. History uses a scrolling local recent-file list, an honest empty state, struck missing titles and availability-dependent reveal actions. It preserves the existing five-file limit and uses normal Tab continuation into workspace navigation.

### Download preferences

The Download speed section keeps the flat Page surface and existing native controls. The primary view shows the speed-limit checkbox and, when enabled, the preset selector; exact KiB/s editing appears only for Custom. A collapsed Advanced download options expander contains same-file connections (1 default, 2, 4 or 8), automatic retries and the optional direct-MP4 connections path. Retain original stored units, validation and per-job snapshots without a speed guarantee. The queue composer omits these global preferences. Each queue link is one quiet Surface group with a one-unit Divider outline, 12-DIP radius and 12-DIP insets. Its URL has a localized hint; compact Type, Format and Quality selectors sit beneath it with a red media-kind icon and no manual format-check action. Keep 12-DIP spacing between groups and the existing fixed footer. The controls wrap at narrow widths, retain localized accessible names, and preserve each draft across navigation and language/theme changes. After a 450-ms typing pause, inspect pending links one at a time when the provider is idle. Show a localized waiting/checking line and keep the selectors hidden until verified metadata arrives. Then offer real resolutions and source-bounded audio bitrates only. Changing output choices reuses metadata; replacing a URL, leaving the workspace or closing cancels stale work. Playlist quality remains automatic per item. Adding links captures these output choices independently, without inheriting another editor video identity. Files use the saved folder and global transfer preferences. Remove scheduling and manual reorder arrows; retire legacy saved timers without changing job order, completed files or per-job options. Preserve English/Arabic layout, native keyboard access and explicit start/pause/resume behavior.

### Native browser setup

The setup dialog reuses Page, Ink, Muted, Accent and the existing native controls. Its title is 23 DIP with 30 line height; the primary action uses the rounded PrimaryButton treatment with a local 44 DIP minimum height and 12 × 6 padding. The first view pairs the browser selector with concise action copy and current app-registration status. Successful registration uses Success, while separate text reports whether the browser handshake has been tested. Registration alone never presents a verified browser connection.

A published listing selects the store action; otherwise installed and portable apps both select local setup. The explicit local action prepares verified matching companion files if needed, connects the app, copies the real extension path and opens the selected browser's setup page. Missing or malformed embedded recovery content instead presents an active illustrated-guide action and hides file-loading instructions. Optional manual controls stay collapsed initially. Firefox's temporary-add-on limitation is stated alongside the loading instructions. Paths and browser addresses retain LTR containers within Arabic layout; messages and verification status use polite live announcements.

### Browser chooser

The isolated dialog contains a burgundy wordmark header with readiness copy and a drawn Close control, a thumbnail/title/source identity block, segmented Video/Audio controls, compact format and quality radio-card groups, a selection summary and a full-width Download action with a drawn glyph. Media-type segments use an inset selected fill. Formats remain visible choices. Each format and quality card has a 14px circular radio mark; selection uses a 4px Accent ring. Compact rules hide the visual HD/4K/8K badges. Effective card and action dimensions are recorded in Layout and the frontmatter.

Available formats and heights come from inspection. The summary repeats the selected format and height, or automatic source quality for audio, and states that file size is unavailable before downloading. Radio groups expose role, checked state and a roving Tab stop. Home and End move to the first and last choices; vertical arrows wrap, and horizontal arrows follow interface direction for Arabic. Video/Audio unavailable states disable the corresponding segment.

Browser focus uses a two-pixel Accent outline offset by three pixels. Escape and outside pointer dismiss the chooser, Tab stays among available controls, and closing restores anchor focus. Dismissal restores the overlay label and clears its pending/finding presentation; disposed picker callbacks ignore late inspection or send results. Loading, retry, unavailable, successful handoff and legacy-app handoff states use visible text. CSS variables carry light, dark and forced-color themes; selection, scrollbars and numeric alignment use the same palette and typography.

A trusted Download confirmation carries the selected media kind, container and height through the start-download capability. The native app saves that choice with its existing saved folder in the durable queue before accepting it, starts when idle and queues confirmed work while busy. Queue-wide pause, cancel, engine halt and close clear automatic continuation intent; old persisted work stays stopped after restart. Hosts without confirmed-start support show the explicit older-app review/start fallback.

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

Keep the manager shell, neutral workspace and restrained Vydrop red actions. The inline editor groups the URL, Paste action and brief guidance in one shallow surface, followed by flat output rows with aligned captions. Rows stack at narrow widths; URL entry stays left-to-right in either language. One folder picker includes Change, with a separate Open folder action. A divider separates the quiet queue action from the primary Download action. Header and form fill the available center width with aligned 24-DIP side padding; no fixed content-width cap or tab Close control is used. Media identity remains in the selected right inspector at wide widths and inline when compact. All controls continue to use real available media, saved options and existing queue commands.


## Native icon and color refinement

Use the selected Lucide 0.577.0 system icons as native WPF geometry, with the shared LineIcon rounded 1.7-DIP stroke. The approved Vydrop wordmark and brand arrow remain separate and unchanged. Navigation, form, History, menus and the theme switch share these geometries; keep the existing resource keys to preserve older view bindings. The pinned upstream commit and ISC / Feather MIT licenses are in THIRD-PARTY-NOTICES.txt. No icon package or network lookup runs in the app.

Reserve cherry for actions, active downloads and selection. Paused or interrupted work uses Warning amber; completion uses Success green; unavailable files and failures use Error. Row and inspector progress inherit the same state brush as their status label and symbol. History's unavailable icon also uses Error alongside its existing struck title and explicit missing-file label. MediaAudio and MediaVideo use the same neutral icon color; their distinct shapes and labels identify the media type. Each state retains visible copy and a meaningful icon.

The native workspace uses off-white layers with pale navigation in light mode and charcoal layers with graphite navigation in dark mode. Neutral utility icons recede so Vydrop red actions, focus outlines and status feedback retain emphasis. English and Arabic use the same roles. Text and semantic colors meet 4.5:1 on normal, field, hover and selected surfaces; control boundaries meet 3:1. Windows high contrast overrides every new semantic role with system foreground colors.


### Restrained native palette

The user rejected the pink/purple surface treatment. Native navigation uses pale neutral surfaces with dark text in light mode and graphite with light text in dark mode, alongside off-white light workspaces and charcoal dark workspaces. Selection surfaces and utility icons are neutral; the red logo and primary actions carry the brand. Remove lavender audio accents and rose navigation icons. Preserve status semantics, typography, layout, keyboard focus and all Windows high-contrast overrides. The browser companion retains its existing palette in this desktop-only scope.


## Contact placement

Contact me is a section in Settings below Download preferences. Its GitHub, LinkedIn and localized Email buttons are visible directly, wrap within the workspace, and use the existing SocialCommand destinations. Browser integration is reached from Settings; no contact footer or popup remains. All links retain normal button focus, accessible names and English/Arabic layout.


## Library and Queue separation

Library is the completed-media collection; Queue is the unfinished-work workspace. Both share the existing virtualized rows and inspector, while headings, descriptions, search hints, filters and actions state their distinct roles. Library and Media badges count completed files; Queue counts unfinished jobs. Search text is retained separately in each scope. Missing completed files retain their strike-through title and unavailable status in Library.

A job that finishes leaves Queue and appears in Library with the same persisted identity. Changing scope, filters, or job state cannot leave an invisible row selected in the inspector. The visible Start download action returns Queue to All and clears its search so new transfers are visible; confirmed browser work also opens Queue. Preserve the existing durable queue schema and explicit resume behavior.

Queue rows expose Pause/Resume, Cancel while active, folder, source refresh when applicable, move controls and Remove on one line. Status shares the title line; the progress bar ends at the thumbnail's lower edge. Accessible names and tooltips explain every icon. Move controls skip completed records and disable unavailable boundaries; active jobs cannot be removed.

Media rows and the queue toolbar keep a left-to-right spatial layout in both interface languages. Artwork stays on the left, transport icons keep their orientation, and row action order and progress direction stay stable. Media titles use the font and reading direction of their own text. Arabic labels and accessible hover actions remain localized. Shared native tooltips use the current Surface/Ink palette, an 8-DIP corner, 12×8-DIP content padding, a 340-DIP maximum width, and a restrained downward shadow. Arabic hints use Noto Sans Arabic and right-to-left text; filenames and URLs retain their literal reading order.

Add to queue opens a center workspace for URL drafts and ordinary queue actions. Accepted links return to the actual Queue list. Browser integration, subtitle options and playlist selection also use inline workspace sections, retaining the toolbar and language/theme controls. Compact details replaces the center list until Back to queue is chosen. No in-app workflow creates a modal window; the native folder picker keeps its normal operating-system behavior. The queue's primary label changes to Resume downloads for paused/interrupted work; an explicit click resumes those durable jobs and processes pending work without changing saved options or the queue schema.

Settings starts with one Browser integration action, followed by App updates, Download support, Download speed and Contact me. Browser integration has no top-level navigation button.
