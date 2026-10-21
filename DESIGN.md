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
  browser-border-dark: "#624759"
  browser-selected-ink-dark: "#FF9EB4"
typography:
  headline: {"fontFamily": "Inter", "fontSize": "27px", "fontWeight": 600, "lineHeight": "36px"}
  title: {"fontFamily": "Inter", "fontSize": "23px", "fontWeight": 600, "lineHeight": "32px"}
  dialog-title: {"fontFamily": "Inter", "fontSize": "22px", "fontWeight": 600}
  body: {"fontFamily": "Inter", "fontSize": "14px", "fontWeight": 400, "lineHeight": "22px"}
  item-title: {"fontFamily": "Inter", "fontSize": "14px", "fontWeight": 600}
  detail-source: {"fontFamily": "Inter", "fontSize": "13px"}
  label: {"fontFamily": "Inter", "fontSize": "12px"}
  caption: {"fontFamily": "Inter", "fontSize": "11px"}
  duration: {"fontFamily": "Inter", "fontSize": "10px"}
  arabic-body: {"fontFamily": "Noto Sans Arabic", "fontSize": "14px", "lineHeight": "22px"}
  browser-body: {"fontFamily": "system-ui, sans-serif", "fontSize": "13px", "fontWeight": 400, "lineHeight": 1.5}
  browser-title: {"fontFamily": "system-ui, sans-serif", "fontSize": "17px", "fontWeight": 650, "lineHeight": 1.4}
rounded:
  row: "4px"
  navigation: "5px"
  control: "8px"
  dialog: "10px"
spacing:
  micro: "3px"
  xs: "4px"
  sm: "8px"
  row: "10px"
  control: "12px"
  md: "16px"
  action: "18px"
  lg: "20px"
  section: "22px"
  workspace: "24px"
  dialog: "28px"
components:
  button-primary: {"backgroundColor": "{colors.action}", "textColor": "{colors.on-accent}", "padding": "10px 18px", "height": "44px"}
  button-secondary: {"backgroundColor": "{colors.field-light}", "textColor": "{colors.ink-light}", "padding": "8px 16px", "height": "44px", "rounded": "{rounded.control}"}
  button-quiet: {"backgroundColor": "transparent", "textColor": "{colors.accent-light}", "padding": "8px 16px", "height": "44px", "rounded": "{rounded.control}"}
  search-field: {"backgroundColor": "{colors.field-light}", "textColor": "{colors.ink-light}", "padding": "8px 12px", "height": "44px", "rounded": "{rounded.control}"}
  navigation-item: {"backgroundColor": "transparent", "textColor": "{colors.navigation-ink}", "padding": "10px 12px", "height": "46px", "rounded": "{rounded.navigation}"}
  navigation-item-selected: {"backgroundColor": "{colors.navigation-selected-light}", "textColor": "{colors.navigation-selected-ink}", "padding": "10px 12px", "height": "46px", "rounded": "{rounded.navigation}"}
  library-filter: {"backgroundColor": "transparent", "textColor": "{colors.muted-light}", "padding": "7px 10px", "height": "38px"}
  library-row-selected: {"backgroundColor": "{colors.selected-light}", "textColor": "{colors.ink-light}", "padding": "10px", "rounded": "{rounded.row}"}
  browser-choice-selected: {"backgroundColor": "{colors.selected-light}", "textColor": "{colors.accent-light}", "padding": "8px 12px", "height": "38px", "rounded": "{rounded.navigation}"}
  browser-launch: {"backgroundColor": "{colors.action}", "textColor": "{colors.on-accent}", "padding": "8px 12px", "height": "44px", "rounded": "{rounded.navigation}"}
---

# Design System: Vydrop

## Overview

**Creative North Star: "The Personal Media Library"**

Vydrop frames download management as a personal media library: confident burgundy navigation, quiet paper or plum work surfaces, and cherry actions. Media identity leads through thumbnails and readable titles; controls and measured transfer state stay close to the selected item. The approved Vydrop V/download-arrow and wordmark anchor the identity.

The native Windows interface uses tonal separation, fine borders, restrained corners and a cut-corner primary action. The browser companion repeats the palette and action silhouette in a compact output chooser. Clarity, accessible states and honest runtime metadata define the visual character. The former reference-like treatment was replaced by this approved original world.

**Key Characteristics:**

- Burgundy navigation with cherry primary actions.
- Paper-white light surfaces and layered plum dark surfaces.
- Inter and Noto Sans Arabic in the native app; browser-native system typography in the companion.
- Thumbnail-led rows, explicit status and a selected-media inspector.
- Logical RTL layout, visible focus and system high-contrast colors.

This record is extracted from ThemeResources.cs, ManagerStyles.xaml, Controls.xaml, MainWindow.xaml, DownloadManagerView.xaml and its code-behind, and browser/common/quality-picker.js. Native measurements below are device-independent pixels; portable frontmatter expresses them as px. Existing build captures corroborate layout and theme behavior. Concept content is sample data. Native evidence and mechanical-gate limitations live in .impeccable/review/native-validation.json; no web fidelity-score claim is made here.

## Colors

Cherry, burgundy and warm plum define the palette; green is reserved for success. Frontmatter owns exact primitive values. Paired -light and -dark entries map directly to native semantic brushes. Sidecar tonal strips are synthesized OKLCH previews for documentation, not additional shipping palette tokens.

### Primary

- **Cherry Action** (action, on-accent): solid primary actions with white content in both themes.
- **Cherry Accent** (accent-light, accent-dark): quiet actions, progress, focus and selected-row outlines; dark mode brightens small accent details.
- **Burgundy Navigation** (navigation-*): persistent navigation, warm white ink, muted rose copy and lighter burgundy selected fill.
- **Editor Download Action** (download-action-fill, download-action-ink, download-action-progress): retained dedicated download-control brushes; keep them distinct from general manager Action and Accent.

### Neutral

- **Paper / Plum Page** (page-*): main workspace.
- **Blush / Plum Surface** (surface-*): details and supporting containers. The browser dark panel uses Surface; its light panel uses Page.
- **Field**, **Ink** and **Muted**: input faces, primary copy and supporting metadata.
- **Border**, **Divider**, **Hover** and **Selected**: distinct boundary, separator, pointer and selection roles.
- **Success** and **Error**: semantic feedback accompanied by explicit status text. Missing files also use a struck title.
- **Browser Border** (browser-border-*): softer companion strokes. browser-selected-ink-dark brightens selected tabs, choices and error text in its dark theme.

**The Semantic Brush Rule.** Use native dynamic resources for theme-aware color roles. In Windows high contrast, system colors replace the authored palette; never pin a status or selection to a brand color.

High contrast maps Ink, Muted, Error, Success and navigation text to WindowText; Accent, Action, Border, DownloadActionFill and selected navigation to Highlight; OnAccent, DownloadActionInk, DownloadActionProgress and selected-navigation ink to HighlightText; other surfaces to Window. Selected/focused row border thickness increases from one to three. Browser forced colors use Canvas, CanvasText, ButtonText, Highlight and HighlightText.

## Typography

The native app bundles **Inter** and **Noto Sans Arabic** through LatinFont and ArabicFont. ContentFont selects by interface language, and Arabic media titles also select the Arabic face inside English UI. The title bar remains native; no decorative display face is introduced.

Frontmatter captures the observed hierarchy: semibold library headline, selected-media or empty-state title, dialog heading, regular body, source, label and caption. Default body line height is fixed; headline/title override it. Row titles are single-line ellipsized with a tooltip; inspector titles wrap to two lines. There is no single mathematical scale ratio in source.

The browser uses the system UI sans-serif stack. Its title is capped at two lines; labels and launch action use weight 650, selected tabs and quality choices use 700, and supporting text is 12px.

**The Language Shape Rule.** Mirror the logical interface for Arabic while keeping technical strings such as source domains, file paths, versions and applicable output details readable in their explicit LTR containers.

## Layout

The manager uses navigation, a flexible library and a selected-media inspector. The native window opens at 1280 × 820, minimum 760 × 500. Library and inspector scroll independently; list containers are recycled and horizontal scrolling is suppressed.

- Navigation is 190 wide, reducing to 176 below manager width 960.
- Inspector width is 330 from 1080 through 1360, then 370 above 1360.
- Below 1080, details move into a 340-wide overlay drawer with 20 outer margin. Details opens it; Escape or Close dismisses it. Background sections are disabled, keyboard navigation cycles within, and focus returns to the list.
- Library margins settle at 24 leading, 4 top, 18 trailing, 0 bottom; below 960 the leading margin is 14. Inspector content has 16 side insets and 20 bottom space. Section separators commonly use 22 vertical space.
- Row artwork is 92 × 60 in a 102-wide leading column. Inspector artwork is 178 high. Missing images retain an outlined media-symbol fallback.
- The Add download editor has 28 outer margin, maximum width 1120 and radius 10. Its form is capped at 620. A 390-wide context column appears only with media context, settings closed and editor width at least 990; otherwise content stacks.

The browser chooser is at most 348 wide and leaves 12px viewport clearance. Width and maximum height are bounded by viewport size minus 24px. Body padding is 18px; header padding is 14px vertically and 16px horizontally. Quality choices use two columns with 8px gaps. The anchor gap is 8px. Embedded popups reserve 570px body height while open. These are fluid constraints, not a mobile breakpoint.

## Elevation & Depth

The native manager uses tonal layering and dividers rather than drop shadows. Selection adds a blush/plum fill and accent outline. The inspector is a continuous surface; the compact drawer uses a half-black scrim and the editor a dark plum scrim. Keep the library flat.

The floating browser chooser has one structural shadow (0 16px 48px #170d2040). Its border and burgundy header remain visible without the shadow.

**The Surface Separation Rule.** Use palette layers and fine boundaries for native hierarchy; reserve the companion shadow for the floating browser chooser.

Native hover layers fade in over 130ms and out over 100ms with cubic ease-out. View reveals use 180ms, opacity .82 to 1 and a 5px vertical settle; theme change uses a 260ms circular reveal from the theme control. Windows client-area animation settings and high contrast disable these effects. The manager cut-corner action has no added hover animation.

## Shapes

Native fields and secondary buttons use the control radius; navigation and artwork use the navigation radius, and rows use the row radius. Dialogs and the browser panel use the dialog radius. Boundaries are normally one unit thick, with explicit thicker focus/high-contrast states.

The manager signature action clips the leading upper corner using WPF geometry M12,0 H240 V48 H0 V12 Z, stretched to the control and logically mirrored for RTL. The browser launch echoes it with polygon(12px 0,100% 0,100% 100%,0 100%,0 12px); focus and forced colors remove the clip to preserve a complete outline.

Use existing vector brand assets and the outlined geometry icon system. Runtime thumbnails are media data. Concept rasters are design references, never shipping controls or text.

## Components

### Buttons

Manager primary actions use Cherry Action, white semibold content, minimum height 44 and padding 18 horizontally by 10 vertically. Focus adds a two-unit Ink stroke; press lowers opacity to .85, disabled state to .45. The inherited rounded PrimaryButton remains in existing editor controls: minimum height 52, subtle white hover layer and two-unit Ink focus.

Secondary buttons use Field, Ink and Border, control radius, 16 × 8 padding and minimum height 44. Hover uses Hover, press uses Selected, and focus adds a two-unit Accent border. Quiet actions inherit the geometry with transparent face/stroke and Accent text. Small row/toolbar actions retain their local smaller minimum sizes.

### Inputs / Fields

Search uses Field and Ink, one-unit Border, control radius and height 44, with local 12 × 8 padding. Its prompt hides once text exists. Text entry uses Accent caret, Selected text selection and an Accent border on focus. Disabled fields use .55 opacity. Native combo boxes share the surface/radius and add a separate two-unit Accent focus border; their popup is a scrolling selection surface.

### Navigation

Navigation items use outlined vector icons, minimum height 46, padding 12 × 10 and margin 8 × 2. Selected items use NavigationSelected with semibold NavigationSelectedInk. Hover adds a NavigationMuted border; focus uses a two-unit NavigationInk border. Arabic reverses logical placement; the wordmark retains its LTR orientation.

### Filter tabs and quality choices

Library filters are quiet text tabs with a two-unit underline. Selection uses Ink, semibold weight and Action underline; hover uses Accent text. Focus adds an Accent boundary while retaining the thicker bottom edge.

Browser Video/Audio tabs repeat the two-pixel underline. Quality choices are bordered controls with navigation-radius corners. Selection uses Selected fill, Accent text/border and bold weight; dark mode uses its brighter selected ink. Formats/heights come from inspection, with retry or app handoff when unavailable.

### Library rows and selected-media details

Flat rows carry thumbnail, title, source, output, explicit status and contextual actions. Selection uses Selected fill and Accent border; focus stays visible and high contrast thickens the border. Hover changes the boundary to Divider. Active row progress is three units high; inspector progress is five. Preparing uses indeterminate progress. Missing files strike their title and show a text status.

The inspector repeats media identity at larger scale, then real output, state, transfer summary, primary action, save folder and options. Speed and remaining time appear for active items. Unavailable values stay explicit. The More menu uses native menu items and restores trigger focus after closing.

### Browser chooser

The isolated dialog contains a burgundy header, media identity, format select, quality choices and full-width launch action, with its own light/dark/forced-color CSS. Buttons/selects are at least 38px high; launch is at least 44px. Focus uses a two-pixel Accent outline offset by three pixels. Escape and outside pointer dismiss it, Tab stays among available controls, and closing restores anchor focus. Loading, retry, unavailable, success and legacy-app handoff states are visible text.

## Do's and Don'ts

### Do:

- **Do** use the semantic theme roles and Windows system colors for high contrast.
- **Do** preserve the approved vector Vydrop identity and use runtime media thumbnails with an honest fallback.
- **Do** keep selected, focused, unavailable and transfer states identifiable through text or shape as well as color.
- **Do** use the bundled native language fonts and mirror logical layout for Arabic.
- **Do** retain the cut-corner action, flat library rows and distinct selected-media surface.

### Don't:

- **Don't** introduce competitor branding or turn the approved concept images into shipping raster UI.
- **Don't** invent thumbnails, file sizes, transfer rates, remaining times or resume guarantees.
- **Don't** use green as a general brand accent; reserve it for successful states with a label or symbol.
- **Don't** add dashboard analytics, decorative graphs or an overcrowded legacy toolbar to the operating surface.
- **Don't** replace native high-contrast or reduced-motion behavior with fixed authored styling.
