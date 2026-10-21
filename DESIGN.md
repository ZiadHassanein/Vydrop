---
name: Vydrop
description: A media library with burgundy navigation, paper and plum surfaces, and cherry actions.
colors:
  page-light: "#FCFAFC"
  page-dark: "#1D1820"
  surface-light: "#F5EDF2"
  surface-dark: "#261E2A"
  field-light: "#FFFFFF"
  field-dark: "#2B242F"
  ink-light: "#2A2029"
  ink-dark: "#FAF3F7"
  muted-light: "#715F6D"
  muted-dark: "#CCBAC7"
  accent-light: "#A91F3B"
  accent-dark: "#F17B96"
  action: "#C72D49"
  on-accent: "#FFFFFF"
  download-action-fill: "#C13046"
  download-action-ink: "#FFFFFF"
  download-action-progress: "#651321"
  border-light: "#8E7D87"
  border-dark: "#94828D"
  hover-light: "#F3E9ED"
  hover-dark: "#353039"
  selected-light: "#F8E8EE"
  selected-dark: "#442638"
  error-light: "#A12232"
  error-dark: "#FFB0B7"
  divider-light: "#E5D9E1"
  divider-dark: "#443448"
  success-light: "#24643E"
  success-dark: "#85D1A2"
  navigation-light: "#3B1B2C"
  navigation-dark: "#351A2A"
  navigation-ink: "#FFF4FA"
  navigation-muted: "#DDC2D2"
  navigation-selected-light: "#753149"
  navigation-selected-dark: "#642A43"
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
  browser-title: {"fontFamily":"system-ui, sans-serif","fontSize":"18px","fontWeight":650,"lineHeight":1.4}
  browser-caption: {"fontFamily":"system-ui, sans-serif","fontSize":"12px"}
  browser-title-narrow: {"fontFamily":"system-ui, sans-serif","fontSize":"17px","fontWeight":650,"lineHeight":1.4}
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
  browser-segment-selected: {"backgroundColor":"{colors.selected-light}","textColor":"{colors.accent-light}","padding":"7px","height":"39px","rounded":"{rounded.segment-option}"}
  browser-format-selected: {"backgroundColor":"{colors.selected-light}","textColor":"{colors.accent-light}","padding":"12px","height":"49px","rounded":"{rounded.browser-control}"}
  browser-choice-selected: {"backgroundColor":"{colors.selected-light}","textColor":"{colors.accent-light}","padding":"12px","height":"53px","rounded":"{rounded.browser-control}"}
  browser-launch: {"backgroundColor":"{colors.action}","textColor":"{colors.on-accent}","padding":"9px 12px","height":"48px","rounded":"{rounded.browser-control}"}
---

# Design System: Vydrop

## Overview

**Creative North Star: "The Personal Media Library"**

Vydrop frames download management as a personal media library: confident burgundy navigation, quiet paper or plum work surfaces, and cherry actions. Media identity leads through thumbnails and readable titles; controls and measured transfer state stay close to the selected item. The approved Vydrop V/download-arrow and wordmark anchor the identity.

The native Windows interface uses tonal separation, fine borders, restrained corners and chamfered navigation and actions. The browser companion repeats the palette and action silhouette in a compact chooser with segmented media types and visible format and quality radio cards. Clarity, accessible states and honest runtime metadata define the visual character.

**Key Characteristics:**

- Burgundy navigation with cherry primary actions.
- Paper-white light surfaces and layered plum dark surfaces.
- Inter and Noto Sans Arabic in the native app; browser-native system typography in the companion.
- Large media thumbnails, counted filters, compact drawn status controls and a selected-media inspector.
- Logical RTL layout, visible focus and system high-contrast colors.

This record describes the current built source: ThemeResources.cs, ManagerStyles.xaml, Controls.xaml, MainWindow.xaml, DownloadManagerView.xaml and its code-behind, QueueItemView.Media.cs, MainViewModel.Library.cs and browser/common/quality-picker.js. The approved light, dark and browser v2 comps remain visual references; the source supplies the measurements below. Native measurements are WPF device-independent pixels (DIP), expressed as px in the portable frontmatter. Browser measurements are CSS pixels. Component heights describe minimum heights where source uses MinHeight or min-height. Existing captures corroborate the composition and themes; their local media is test data. No numeric web comp-diff, font ranking or bitmap-plate gate pass is claimed for WPF. Native gate limitations are recorded in .impeccable/review/native-validation.json.

## Colors

Cherry, burgundy and warm plum define the palette; green is reserved for success. Frontmatter owns exact primitive values. Paired -light and -dark entries map to native semantic brushes. Sidecar tonal strips are synthesized OKLCH previews for documentation, not additional shipping palette tokens.

### Primary

- **Cherry Action** (action, on-accent): solid primary actions with white content in both themes.
- **Cherry Accent** (accent-light, accent-dark): quiet actions, progress, focus and selected-row outlines; dark mode brightens small accent details.
- **Burgundy Navigation** (navigation-*): persistent navigation, warm white ink, muted rose copy and lighter burgundy selected fill.
- **Editor Download Action** (download-action-fill, download-action-ink, download-action-progress): retained dedicated download-control brushes, separate from manager Action and Accent.

### Secondary

- **Source Marks** (source-youtube, source-vimeo, source-soundcloud, source-linkedin, source-facebook): small drawn identifiers beside actual source domains in rows and details. The general web mark uses Accent. These source colors do not become action colors.

### Neutral

- **Paper / Plum Page** (page-*): main workspace.
- **Blush / Plum Surface** (surface-*): details, supporting containers and count badges. The browser dark panel uses Surface; its light panel uses Page.
- **Field**, **Ink** and **Muted**: input and radio-card faces, primary copy and supporting metadata.
- **Border**, **Divider**, **Hover** and **Selected**: distinct boundary, separator, pointer and selection roles.
- **Success** and **Error**: semantic feedback accompanied by explicit status text and a drawn state symbol. Missing files also use a struck title.
- **Browser Border** (browser-border-*): companion strokes, including the brighter dark-mode boundary. The chooser's CSS variables map its surfaces, text, borders, divider, accent and selected state to these palette roles.

**The Semantic Brush Rule.** Use native dynamic resources for theme-aware color roles. In Windows high contrast, system colors replace the authored palette; never pin a status or selection to a brand color.

High contrast maps Ink, Muted, Error, Success and navigation text to WindowText; Accent, Action, Border, DownloadActionFill and selected navigation to Highlight; OnAccent, DownloadActionInk, DownloadActionProgress and selected-navigation ink to HighlightText; other surfaces to Window. Source marks override their brand fills with Accent and their symbol stroke/fill with OnAccent. Selected/focused row border thickness increases from one to three. Browser forced colors use Canvas, CanvasText, ButtonText, Highlight and HighlightText. The launch button alone sets forced-color-adjust:none within the forced-colors query to suppress Chromium text backplates while explicitly retaining system Highlight and HighlightText.

## Typography

The native app bundles **Inter** and **Noto Sans Arabic** through LatinFont and ArabicFont. ContentFont selects by interface language, and Arabic media titles also select the Arabic face inside English UI. The title bar remains native; the approved vector wordmark supplies the brand lettering.

The library headline is semibold (34 DIP, 44 line height). Row titles are semibold (16 DIP), source domains regular (13 DIP), output metadata (12 DIP), and compact row status (11 DIP). Inspector and empty-state titles use the title role (23 DIP, 32 line height). Body text is regular (14 DIP, 22 line height); dialog titles use the dialog-title role. Filter count copy uses caption size; navigation counts use label size. Duration overlays use the duration role. Row titles are single-line ellipsized with a tooltip; inspector titles wrap to two lines. There is no single mathematical scale ratio in source.

The browser uses the system UI sans-serif stack and tabular numerals. Body copy is 14px with 1.5 line height. The media title is 18px, weight 650, line height 1.4 and capped at two lines; at viewports up to 420px it becomes 17px. Media-type segments and launch action use weight 650; format and quality cards use 600. Supporting text is 12px and quality badges are 10px.

**The Language Shape Rule.** Mirror the logical interface for Arabic while keeping technical strings such as source domains, file paths, versions and applicable output details readable in their explicit LTR containers.

## Layout

The manager uses navigation, a flexible library and a selected-media inspector. The native window opens at 1280 × 820 DIP, minimum 760 × 500. Library and inspector scroll independently; list containers are recycled and horizontal scrolling is suppressed.

- Navigation is 224 DIP wide, reducing to 176 below manager width 960. Its brand row is 92 high.
- Inspector width is 320 from manager width 1080 through 1360, then 350 above 1360.
- Below 1080, details move into a 340-wide overlay drawer with 20 outer margin. Details or a row double-click opens it; Escape or Close dismisses it. Background sections are disabled, keyboard navigation cycles within, and focus returns to the list.
- Library margins settle at 24 leading, 4 top, 18 trailing, 0 bottom; below 960 the leading margin is 14. The heading/action group has 18 bottom space, search 8, and filters 10. Inspector content has 20 side insets and 20 bottom space. Section separators use 22 vertical space.
- Row artwork is 132 × 78 in a 146-wide leading column, with rounded artwork corners and a bottom-trailing duration overlay. Row padding is 12 horizontally and 14 vertically. Inspector artwork is 184 high. Missing images retain a drawn media-symbol fallback.
- Row content separates identity, output metadata and a narrow trailing status/action group. This trailing group has minimum width 78 and maximum width 126; status text can ellipsize with a tooltip.
- The Add download editor has 28 outer margin, maximum width 1120 and dialog radius. Its form is capped at 620. A 390-wide context column appears only with media context, settings closed and editor width at least 990; otherwise content stacks.

The browser chooser is at most 416 CSS pixels wide and leaves 12px viewport clearance. Width and maximum height are bounded by viewport size minus 24px, with scrolling within the panel. The anchor gap is 8px. The header is at least 64px high with 13 × 20px padding; body padding is 22px. The wordmark is 128 × 34px. Identity uses a 116px-wide image at 16:10 (72.5px high) beside text with a 16px gap. Missing artwork removes the image column. Format and quality cards use two columns with 9px gaps. At viewports up to 420px, body padding is 18px, header padding 12 × 18px, identity artwork becomes 100 × 62.5px with a 12px gap, and card padding becomes 11px. Embedded popups reserve 650px body height while open and restore the previous height when dismissed.

## Elevation & Depth

The native manager uses tonal layering and dividers rather than drop shadows. Selection adds a blush/plum fill and accent outline. The inspector is a continuous surface; the compact drawer uses a half-black scrim and the editor a dark plum scrim. Keep the library flat.

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

Navigation items use outlined 18 × 18 vector icons, minimum height 48 DIP, padding 14 × 10 and margin 10 × 3. Selected items use NavigationSelected and semibold NavigationSelectedInk within the chamfered face. Hover adds a NavigationMuted stroke; focus uses a two-unit NavigationInk stroke. Queue, Video and Audio display live 23-high count badges. Queue's badge counts unfinished items. Arabic reverses logical placement; the wordmark retains its LTR orientation.

### Counted filter tabs

Library filters are text tabs with a two-unit underline and 23-high count badges. Selection uses Ink, semibold weight and Action underline; hover uses Accent text. Focus adds an Accent boundary while retaining the thicker bottom edge. All, Downloading, Paused and Completed show queue-derived counts. Paused includes interrupted work; these counts come from the full queue rather than the search-filtered result set. A drawn compact control beside the tabs opens queue management.

### Library rows and selected-media details

Flat rows carry large thumbnail, title, a drawn source mark and domain, output metadata, explicit status and contextual controls. Source marks are 18 × 16 DIP. Selection uses Selected fill and Accent border; focus stays visible and high contrast thickens the border. Hover changes the boundary to Divider. Missing files strike their title and show a text status with an alert symbol.

The trailing group combines a drawn 12 × 12 status symbol with 11 DIP status text. Active non-preparing, paused and interrupted rows append the real current percentage. A drawn 18 × 18 action sits in a minimum 34 × 34 target; completed rows open their save folder, active rows pause and other rows expose their available primary action. The 28-minimum More target uses three drawn dots and a native context menu. Closing that menu restores trigger focus.

Row and inspector progress are both 5 DIP high with a slanted fill edge and their real Percent binding. Preparing uses the IsIndeterminate state. The inspector repeats media identity at larger scale, then actual output, state and active percentage. Transfer amount, speed and remaining time are compact inline text; active values appear only when available and otherwise use explicit unavailable values. A single-line ellipsized LTR save path has a full-path tooltip and a separate drawn folder action. The inspector ends with subtitle options and saved-locally feedback when applicable.

### Browser chooser

The isolated dialog contains a burgundy wordmark header with readiness copy and a drawn Close control, a thumbnail/title/source identity block, segmented Video/Audio controls, format and quality radio-card groups, a selection summary and a full-width launch action. Media-type segments include drawn video/audio symbols and use an inset selected fill. They are not underlined tabs. Formats are visible cards rather than a select menu. Each format and quality card has a circular radio mark; quality may show an inspected HD/4K/8K label. Format cards are at least 49px high and quality cards 53px. The 48px launch action includes a drawn download glyph.

Available formats and heights come from inspection. The summary repeats the selected format and height, or automatic source quality for audio, and states that file size is unavailable before downloading. Radio groups expose role, checked state and a roving Tab stop. Home and End move to the first and last choices; vertical arrows wrap, and horizontal arrows follow interface direction for Arabic. Video/Audio unavailable states disable the corresponding segment.

Browser focus uses a two-pixel Accent outline offset by three pixels. Escape and outside pointer dismiss the chooser, Tab stays among available controls, and closing restores anchor focus. Loading, retry, unavailable, successful handoff and legacy-app handoff states are visible text. CSS variables carry light, dark and forced-color themes; selection, scrollbars and numeric alignment use the same palette and typography.

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
